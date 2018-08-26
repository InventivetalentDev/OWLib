using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.JSON;
using DataTool.SaveLogic;
using DataTool.SaveLogic.Unlock;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using Combo = DataTool.FindLogic.Combo;
using static DataTool.Helper.IO;
using STUHero = TankLib.STU.Types.STUHero;
using STUVoiceSetComponent = TankLib.STU.Types.STUVoiceSetComponent;
using static DataTool.Helper.Logger;

namespace DataTool.ToolLogic.Dump {
    [Tool("dump-hero-voice", Description = "Dumps all the strings", TrackTypes = new ushort[] { 0x75 }, CustomFlags = typeof(DumpFlags))]
    public class DumpHeroVoice : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public class BaseCondition {
            [JsonConverter(typeof(StringEnumConverter))]
            public Enum_1AA009C2 CondType;
        }
        public class MapCond : BaseCondition {
            public string Map;
        }

        public class HeroCond : BaseCondition {
            public string Hero;
        }
        
        public class TeamCond : BaseCondition {
            [JsonConverter(typeof(StringEnumConverter))]
            public TeamIndex Team;
        }

        public class VirtualCond : BaseCondition {
            public string Virtual01C;
            public ulong Key;
        }

        public class GenderCond : BaseCondition {
            [JsonConverter(typeof(StringEnumConverter))]
            public Enum_0C014B4A Gender;
        }

        public class BaseCelebCond : BaseCondition {
            public string Celebration;
        }
        
        public class CelebCond : BaseCelebCond {
            public string Virtual0C1;
        }
        
        public class CelebCond2 : BaseCelebCond {
            public string Virtual0C3;
            public ulong Key;
        }

        public class Conversation {
            public string GUID;
            public int Position;
        }

        public class ConditionsContainer {
            public int Required;
            public List<BaseCondition> Requirements;
        }

        public class SoundInfo {
            public string HeroName;
            public string SoundFile;
            public string StimulusSet;
            public string Subtitle;
            public Conversation Conversation;
            public List<string> Skins;
            public ConditionsContainer Conditions;
            
            [JsonIgnore]
            public ulong GUID;

            public bool ShouldSerializeConversation() => Conversation != null;
            public bool ShouldSerializeSubtitle() => Subtitle != null;
            public bool ShouldSerializeSkins() => Skins.Any();
            public bool ShouldSerializeConditions() => Conditions != null;
        }
        
        private static readonly Dictionary<uint, string> MapNames = new Dictionary<uint, string>();
        private static Dictionary<string, SoundInfo> SoundList = new Dictionary<string, SoundInfo>();

        public void Parse(ICLIFlags toolFlags) {            
            foreach (ulong mapGuid in TrackedFiles[0x9F]) {
                STUMapHeader mapHeader = GetInstance<STUMapHeader>(mapGuid);
                if (mapHeader == null) continue;
                
                MapNames[teResourceGUID.Index(mapGuid)] = GetString(mapHeader.m_1C706502) ?? GetString(mapHeader.m_displayName);
            }
            
            foreach (ulong heroGuid in TrackedFiles[0x75]) {
                STUHero hero = GetInstance<STUHero>(heroGuid);
                if (hero == null) continue;
                
                string heroNameActual = (GetString(hero.m_0EDCE350) ?? $"Unknown{teResourceGUID.Index(heroGuid)}").TrimEnd(' ');
                
                var progression = new ProgressionUnlocks(hero);
                if (progression.LootBoxesUnlocks == null) continue; // no npcs thanks
                
                STUVoiceSetComponent baseComponent = default;
                Combo.ComboInfo baseInfo = default;
 
                Log("\tProcessing data for {0}", heroNameActual);

                if (ProcessSounds(heroNameActual, hero.m_gameplayEntity, null, ref baseComponent, ref baseInfo)) {
                    if (hero.m_heroProgression == 0) continue;
                    
                    foreach (Unlock itemInfo in progression.OtherUnlocks)
                        ProcessUnlock(heroNameActual, itemInfo, hero, baseComponent, baseInfo);

                    foreach (var defaultUnlocks in progression.LevelUnlocks)
                        foreach (Unlock unlock in defaultUnlocks.Unlocks)
                            ProcessUnlock(heroNameActual, unlock, hero, baseComponent, baseInfo);

                    foreach (var eventUnlocks in progression.LootBoxesUnlocks) {
                        if (eventUnlocks?.Unlocks == null) continue;

                        foreach (Unlock unlock in eventUnlocks.Unlocks)
                            ProcessUnlock(heroNameActual, unlock, hero, baseComponent, baseInfo);
                    }
                }
            }

            ParseJSON(
                SoundList,
                toolFlags as DumpFlags
            );
        }
        
        private static void ProcessUnlock(string heroNameActual, Unlock unlock, STUHero hero, STUVoiceSetComponent baseComponent, Combo.ComboInfo baseInfo) {
            if (!(unlock.STU is STUUnlock_SkinTheme unlockSkinTheme)) return;
            if (unlockSkinTheme.m_0B1BA7C1 != 0)
                return;

            ProcessSkin(heroNameActual, unlockSkinTheme.m_skinTheme, hero, unlock.Name, baseComponent, baseInfo);
        }
        
        private static void ProcessSkin(string heroNameActual, ulong skinResource, STUHero hero, string name, STUVoiceSetComponent baseComponent, Combo.ComboInfo baseInfo) {
            STUSkinTheme skin = GetInstance<STUSkinTheme>(skinResource);
            if (skin == null)
                return;

            STUVoiceSetComponent component = default;
            Combo.ComboInfo info = default;

            ProcessSounds(heroNameActual, hero.m_gameplayEntity, name, ref component, ref info, baseComponent, baseInfo, SkinTheme.GetReplacements(skin));
        }

        private static bool ProcessSounds(string heroNameActual, ulong entityMain, string skin, ref STUVoiceSetComponent voiceSetComponent, ref Combo.ComboInfo info, STUVoiceSetComponent baseComponent = null, Combo.ComboInfo baseCombo = null, Dictionary<ulong, ulong> replacements = null) {
            voiceSetComponent = GetInstance<STUVoiceSetComponent>(Combo.GetReplacement(entityMain, replacements));

            if (voiceSetComponent?.m_voiceDefinition == null)
                return false;

            info = new Combo.ComboInfo();
            Combo.Find(info, voiceSetComponent.m_voiceDefinition, replacements);

            if (baseComponent != null && baseCombo != null)
                if (!Combo.RemoveDuplicateVoiceSetEntries(baseCombo, ref info, baseComponent.m_voiceDefinition, Combo.GetReplacement(voiceSetComponent.m_voiceDefinition, replacements)))
                    return false;
            
            VoiceSet voiceSet = new VoiceSet(GetInstance<STUVoiceSet>(Combo.GetReplacement(voiceSetComponent.m_voiceDefinition, replacements)));
            Combo.VoiceSetInfo voiceSetInfo = info.VoiceSets[Combo.GetReplacement(voiceSetComponent.m_voiceDefinition, replacements)];
            
            foreach (var voicelineInstanceInfo in voiceSetInfo.VoiceLineInstances) {
                foreach (var voiceLineInstance in voicelineInstanceInfo.Value) {
                    SoundInfo newSound = new SoundInfo {
                        HeroName = heroNameActual,
                        Subtitle = GetInstance<STU_7A68A730>(voiceLineInstance.Subtitle)?.m_798027DE.m_text,
                        StimulusSet = teResourceGUID.AsString(voiceLineInstance.VoiceStimulus),
                        Skins = new List<string>()
                    };

                    if (skin != null)
                        newSound.Skins.Add(skin);

                    if (voiceSet.VoiceLines.ContainsKey(voiceLineInstance.GUIDx06F)) {
                        var vl = voiceSet.VoiceLines[voiceLineInstance.GUIDx06F];

                        if (vl.STU.m_voiceLineRuntime.m_4FF98D41 != null) {
                            var condition = vl.STU.m_voiceLineRuntime.m_4FF98D41;
                            newSound.Conditions = ParseConditions(condition, ref newSound);
                        }

                        if (vl.VoiceConversation != 0) {
                            var convo = GetInstance<STUVoiceConversation>(vl.VoiceConversation);
                            
                            newSound.Conversation = new Conversation {
                                GUID = teResourceGUID.AsString(vl.VoiceConversation),
                                Position = convo.m_voiceConversationLine.ToList().FindIndex(c => c.m_lineGUID == voiceLineInstance.GUIDx06F)
                            };
                        }
                    }

                    foreach (var sound in voiceLineInstance.SoundFiles) {
                        newSound.GUID = sound;
                        newSound.SoundFile = teResourceGUID.AsString(sound);

                        if (SoundList.ContainsKey(newSound.SoundFile)) {
                            if (skin != null) {
                                SoundList[newSound.SoundFile].Skins.Add(skin);
                                continue;
                            }
                        }
                        
                        SoundList[newSound.SoundFile] = newSound;
                    }
                }
            }

            return true;
        }
        
        private static ConditionsContainer ParseConditions(STU_C1A2DB26 condition, ref SoundInfo newSound) {
            var @return = new ConditionsContainer {
                Required = 1,
                Requirements = new List<BaseCondition>()
            };

            if (!(condition is STU_32A19631 cond2)) return null;
            var subCond = cond2.m_4FF98D41;                

            switch (subCond) {
                case STU_E9DB72FF mapCond:
                    @return.Requirements.Add(new MapCond{ CondType = mapCond.m_type, Map = MapNames[teResourceGUID.Index(mapCond.m_map)]});
                    break;
                case STU_D815520F heroCond:
                    var hero = GetInstance<STUHero>(heroCond.m_8C8C5285);
                    var name = (GetString(hero?.m_0EDCE350) ?? $"Unknown{teResourceGUID.Index(heroCond.m_8C8C5285)}").TrimEnd(' ');
                    @return.Requirements.Add(new HeroCond{ CondType = heroCond.m_type,  Hero = name});
                    break;
                case STU_C37857A5 celebCond:
                    @return.Requirements.Add(new CelebCond {
                        CondType = celebCond.m_type,
                        Celebration = celebCond.GetCelebrationType(celebCond.m_celebrationType),
                        Virtual0C1 = teResourceGUID.AsString(celebCond.m_celebrationType)
                    });
                    break;
                case STU_C7CA73B1 celebCond2:
                    @return.Requirements.Add(new CelebCond2 {
                        CondType = celebCond2.m_type,
                        Celebration = celebCond2.GetCelebrationType(celebCond2.m_celebration),
                        Virtual0C3 = teResourceGUID.AsString(celebCond2.m_celebration)
                    });
                    break;
                // Cond depends on Virtual 01Cs
                case STU_D0364821 virtualCond:
                    @return.Requirements.Add(new VirtualCond {
                        CondType = virtualCond.m_type,
                        Virtual01C = teResourceGUID.AsString(virtualCond.m_identifier),
                        Key = virtualCond.m_identifier.GUID
                    });
                    break;
                case STU_BDD783B9 teamCond:
                    @return.Requirements.Add(new TeamCond{ CondType = teamCond.m_type, Team = teamCond.m_team });
                    break;
                case STU_7C69EA0F thiccCond:
                    @return.Required = (int) thiccCond.m_amount; // Override the default requirement

                    // Thicc Cond is basically a wrapper of multiple subconditions that follow the same format as a normal condition.
                    if (thiccCond.m_4FF98D41 != null) {
                        foreach (STU_32A19631 cond in thiccCond.m_4FF98D41) {
                            var conds = ParseConditions(cond, ref newSound);
                            if (conds != null)
                                @return.Requirements.AddRange(conds.Requirements);
                        }
                    }
                    break;
                case STU_A95E4B99 genderCond:
                    @return.Requirements.Add(new GenderCond {
                        CondType = genderCond.m_type,
                        Gender = genderCond.m_7D88A63A
                    });
                    break;
                case STU_C9F4617F genderCond2:
                    @return.Requirements.Add(new GenderCond {
                        CondType = genderCond2.m_type,
                        Gender = genderCond2.m_7D88A63A
                    });
                    break;
                default:
                    @return = null; // No condition, don't return anything
                    break;
            }

            return @return;
        }
    }
}