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
            public Enum_1AA009C2 m_type;
            public string GuessedType;
        }
        public class MapCond : BaseCondition {
            public string GuessedType = "MapCond";
            public string Map;
        }

        public class HeroCond : BaseCondition {
            public string GuessedType = "HeroCond";
            public string Hero;
        }
        
        public class TeamCond : BaseCondition {
            public string GuessedType = "TeamCond";
            
            [JsonConverter(typeof(StringEnumConverter))]
            public TeamIndex Team;
        }

        public class VirtualCond : BaseCondition {
            public string GuessedType = "VirtualCond";
            public string Virtual01C;
            public ulong Key;
        }

        public class GenderCond : BaseCondition {
            public string GuessedType = "GenderCond";

            [JsonConverter(typeof(StringEnumConverter))]
            public Enum_0C014B4A Gender;
        }

        public class BaseCelebCond : BaseCondition {
            public string Celebration;
        }
        
        public class CelebCond : BaseCelebCond {
            public string GuessedType = "CelebCond";
            public string Virtual0C1;
        }
        
        public class CelebCond2 : BaseCelebCond {
            public string GuessedType = "CelebCond2";
            public string Virtual0C3;
            public ulong Key;
        }

        public class CondDetails {
            public uint m_amount;
            public int m_07D0F7AA;
            public ulong m_A20DCD80;
            
            [JsonConverter(typeof(StringEnumConverter))]
            public Enum_AB6CE3D1 m_967A138B;
        }

        public class Conversation {
            public string GUID;
            public int Position;
        }

        public class SoundInfo {
            public string HeroName;
            public string SoundFile;
            public string StimulusSet;
            public string Subtitle;
            public Conversation Conversation;
            public string[] Skins;
            public CondDetails CondDetails;
            public List<BaseCondition> Conditions;

            [JsonIgnore]
            public ulong GUID;

            public bool ShouldSerializeConversation() => Conversation != null;
            public bool ShouldSerializeSubtitle() => Subtitle != null;
            public bool ShouldSerializeSkins() => Skins != null;
            public bool ShouldSerializeCondDetails() => CondDetails != null;
            public bool ShouldSerializeConditions() => Conditions.Any();
        }
        
        private static readonly Dictionary<uint, string> MapNames = new Dictionary<uint, string>();
        private static List<SoundInfo> SoundList = new List<SoundInfo>();

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
                SoundList.OrderBy(s => s.GUID).ToList(),
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
                        Conditions = new List<BaseCondition>(),
                        StimulusSet = teResourceGUID.AsString(voiceLineInstance.VoiceStimulus),
                        Skins = skin != null ? new []{skin} : null
                    };

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
                        SoundList.Add(newSound);
                    }
                }
            }

            return true;
        }
        
        private static List<BaseCondition> ParseConditions(STU_C1A2DB26 condition, ref SoundInfo newSound) {
            var @return = new List<BaseCondition>();
            
            if (condition is STU_32A19631 cond2) {
                var subCond = cond2.m_4FF98D41;

                switch (subCond) {
                    case STU_E9DB72FF mapCond:
                        @return.Add(new MapCond{ m_type = mapCond.m_type, Map = MapNames[teResourceGUID.Index(mapCond.m_map)]});
                        break;
                    case STU_D815520F heroCond:
                        var hero = GetInstance<STUHero>(heroCond.m_8C8C5285);
                        var name = (GetString(hero?.m_0EDCE350) ?? $"Unknown{teResourceGUID.Index(heroCond.m_8C8C5285)}").TrimEnd(' ');
                        @return.Add(new HeroCond{ m_type = heroCond.m_type,  Hero = name});
                        break;
                    case STU_C37857A5 celebCond:
                        @return.Add(new CelebCond {
                            m_type = celebCond.m_type,
                            Celebration = celebCond.GetCelebrationType(celebCond.m_celebrationType),
                            Virtual0C1 = teResourceGUID.AsString(celebCond.m_celebrationType)
                        });
                        break;
                    case STU_C7CA73B1 celebCond2:
                        @return.Add(new CelebCond2 {
                            m_type = celebCond2.m_type,
                            Celebration = celebCond2.GetCelebrationType(celebCond2.m_celebration),
                            Virtual0C3 = teResourceGUID.AsString(celebCond2.m_celebration)
                        });
                        break;
                    // Cond depends on Virtual 01Cs
                    case STU_D0364821 virtualCond:
                        @return.Add(new VirtualCond {
                            m_type = virtualCond.m_type,
                            Virtual01C = teResourceGUID.AsString(virtualCond.m_identifier),
                            Key = virtualCond.m_identifier.GUID
                        });
                        break;
                    case STU_BDD783B9 teamCond:
                        @return.Add(new TeamCond{ m_type = teamCond.m_type, Team = teamCond.m_team });
                        break;
                    case STU_7C69EA0F thiccCond:
                        newSound.CondDetails = new CondDetails {
                            m_amount = thiccCond.m_amount,
                            m_07D0F7AA = thiccCond.m_07D0F7AA,
                            m_967A138B = thiccCond.m_967A138B,
                            m_A20DCD80 = thiccCond.m_A20DCD80
                        };

                        // Thicc Cond is basically a wrapper of multiple subconditions that follow the same format as a normal condition.
                        if (thiccCond.m_4FF98D41 != null) {
                            foreach (STU_32A19631 cond in thiccCond.m_4FF98D41) {
                                @return.AddRange(ParseConditions(cond, ref newSound));
                            }
                        }
                        break;
                    case STU_A95E4B99 genderCond:
                        @return.Add(new GenderCond { m_type = genderCond.m_type, Gender = genderCond.m_7D88A63A });
                        break;
                    default:
                        //Debugger.Break();
                        break;
                }
            } else {
                //Debugger.Break();
            }

            return @return;
        }
    }
}