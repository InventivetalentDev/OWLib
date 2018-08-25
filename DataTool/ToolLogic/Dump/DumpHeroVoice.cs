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

        public class ConditionalCrap {
            public Enum_1AA009C2 m_type;
            public string GuessedType;
            public string Data;

            public ConditionalCrap(Enum_1AA009C2 type, string otherType, string data) {
                m_type = type;
                GuessedType = otherType;
                Data = data;
            }
        }

        public class SoundInfo {
            public string HeroName;
            public string SoundFile;
            public string StimulusSet;
            public string ConvoSet;
            public int? ConvoPos;
            public string Subtitle;
            public string[] Skins;
            public List<ConditionalCrap> OtherStuff;

            [JsonIgnore]
            public ulong GUID;

            public bool ShouldSerializeConvoSet() => !string.IsNullOrEmpty(ConvoSet);
            public bool ShouldSerializeConvoPos() => ConvoPos != null;
            public bool ShouldSerializeSubtitle() => Subtitle != null;
            public bool ShouldSerializeSkins() => Skins != null;
            public bool ShouldSerializeOtherStuff() => OtherStuff.Any();

            public SoundInfo(string heroName, ulong guid, ulong groupGuid, string subtitle, ulong convoGuid, int? convoPosition, string skin, List<ConditionalCrap> otherStuff) {
                GUID = guid;
                HeroName = heroName;
                SoundFile = $"{teResourceGUID.LongKey(guid):X12}";
                StimulusSet = $"{teResourceGUID.LongKey(groupGuid):X12}";
                ConvoSet = convoGuid == 0 ? null : $"{teResourceGUID.LongKey(convoGuid):X12}";
                ConvoPos = convoPosition;
                Subtitle = subtitle;
                Skins = skin != null ? new []{skin} : null;
                OtherStuff = otherStuff;
            }
        }
        
        private static readonly Dictionary<uint, string> MapNames = new Dictionary<uint, string>();

        public void Parse(ICLIFlags toolFlags) {
            List<SoundInfo> soundList = new List<SoundInfo>();
            
            foreach (ulong mapGuid in TrackedFiles[0x9F]) {
                STUMapHeader mapHeader = GetInstance<STUMapHeader>(mapGuid);
                if (mapHeader == null) continue;
                
                MapNames[teResourceGUID.Index(mapGuid)] = GetString(mapHeader.m_1C706502) ?? GetString(mapHeader.m_displayName);
            }
            
            foreach (ulong heroGuid in TrackedFiles[0x75]) {
                STUHero hero = GetInstance<STUHero>(heroGuid);
                if (hero == null) continue;
                
                var progression = new ProgressionUnlocks(hero);
                if (progression.LootBoxesUnlocks == null) continue; // no npcs thanks
                
                string heroNameActual = (GetString(hero.m_0EDCE350) ?? $"Unknown{teResourceGUID.Index(heroGuid)}").TrimEnd(' ');
                STUVoiceSetComponent baseComponent = default;
                Combo.ComboInfo baseInfo = default;
                
                Log("\tProcessing data for {0}", heroNameActual);

                if (ProcessSounds(heroNameActual, hero.m_gameplayEntity, null, ref baseComponent, ref baseInfo, ref soundList)) {
                    if (hero.m_heroProgression == 0) continue;
                    
                    foreach (Unlock itemInfo in progression.OtherUnlocks)
                        ProcessUnlock(heroNameActual, itemInfo, hero, baseComponent, baseInfo, ref soundList);

                    foreach (var defaultUnlocks in progression.LevelUnlocks)
                        foreach (Unlock unlock in defaultUnlocks.Unlocks)
                            ProcessUnlock(heroNameActual, unlock, hero, baseComponent, baseInfo, ref soundList);

                    foreach (var eventUnlocks in progression.LootBoxesUnlocks) {
                        if (eventUnlocks?.Unlocks == null) continue;

                        foreach (Unlock unlock in eventUnlocks.Unlocks)
                            ProcessUnlock(heroNameActual, unlock, hero, baseComponent, baseInfo, ref soundList);
                    }
                }
            }

            ParseJSON(
                soundList.OrderBy(s => s.GUID).ToList(),
                toolFlags as DumpFlags
            );
        }
        
        public static void ProcessUnlock(string heroNameActual, Unlock unlock, STUHero hero, STUVoiceSetComponent baseComponent, Combo.ComboInfo baseInfo, ref List<SoundInfo> soundList) {
            if (!(unlock.STU is STUUnlock_SkinTheme unlockSkinTheme)) return;
            if (unlockSkinTheme.m_0B1BA7C1 != 0)
                return;

            ProcessSkin(heroNameActual, unlockSkinTheme.m_skinTheme, hero, unlock.Name, baseComponent, baseInfo, ref soundList);
        }
        
        public static void ProcessSkin(string heroNameActual, ulong skinResource, STUHero hero, string name, STUVoiceSetComponent baseComponent, Combo.ComboInfo baseInfo, ref List<SoundInfo> soundList) {
            STUSkinTheme skin = GetInstance<STUSkinTheme>(skinResource);
            if (skin == null)
                return;

            STUVoiceSetComponent component = default;
            Combo.ComboInfo info = default;

            ProcessSounds(heroNameActual, hero.m_gameplayEntity, name, ref component, ref info, ref soundList, baseComponent, baseInfo, SkinTheme.GetReplacements(skin));
        }        

        public static bool ProcessSounds(string heroNameActual, ulong entityMain, string skin, ref STUVoiceSetComponent voiceSetComponent, ref Combo.ComboInfo info, ref List<SoundInfo> soundList, STUVoiceSetComponent baseComponent = null, Combo.ComboInfo baseCombo = null, Dictionary<ulong, ulong> replacements = null) {
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
                    var subtitleInfo = GetInstance<STU_7A68A730>(voiceLineInstance.Subtitle);
                    var subtitle = subtitleInfo?.m_798027DE.m_text;
                    ulong conversationGuid = 0;
                    int? conversationPosition = null;

                    var otherStuff = new List<ConditionalCrap>();
                    
                    //string file = $"{teResourceGUID.LongKey(voiceLineInstance.SoundFiles[0]):X12}";

                    if (voiceSet.VoiceLines.ContainsKey(voiceLineInstance.GUIDx06F)) {
                        var vl = voiceSet.VoiceLines[voiceLineInstance.GUIDx06F];
                        
                        if (vl.STU.m_voiceLineRuntime.m_4FF98D41 != null) {
                            var cond = vl.STU.m_voiceLineRuntime.m_4FF98D41;
                            
                            if (cond is STU_32A19631 cond2) {
                                var subCond = cond2.m_4FF98D41;

                                switch (subCond) {
                                    // Map Specific?? 
                                    case STU_E9DB72FF mapCond:
                                        var mapName = MapNames[teResourceGUID.Index(mapCond.m_map)];
                                        otherStuff.Add(new ConditionalCrap(mapCond.m_type, "MapSpecific", mapName));
                                        break;
                                    // interaction of some sort
                                    case STU_D815520F heroCond:
                                        var hero = GetInstance<STUHero>(heroCond.m_8C8C5285);
                                        var name = (GetString(hero?.m_0EDCE350) ?? $"Unknown{teResourceGUID.Index(heroCond.m_8C8C5285)}").TrimEnd(' ');
                                        otherStuff.Add(new ConditionalCrap(heroCond.m_type, "Interaction", name));
                                        break;
                                    // Dunno
                                    case STU_D0364821 noclue:
                                        // 0000000385450.0B2 - Reaper - "Enemies below us" - 6
                                        break;
                                    case STU_7C69EA0F lessclue:
                                        otherStuff.Add(new ConditionalCrap(lessclue.m_type, "Less Clue", null));
                                        break;
                                    case STU_C37857A5 celebCond:
                                        otherStuff.Add(new ConditionalCrap(celebCond.m_type, "Celebration", celebCond.GetCelebrationType(celebCond.m_celebrationType)));
                                        break;
                                    case STU_BDD783B9 stillnoclue:
                                        otherStuff.Add(new ConditionalCrap(stillnoclue.m_type, "Still No Clue", null));
                                        break;
                                    default:
                                        //var file = $"{teResourceGUID.LongKey(vl.VoiceSounds[0]):X12}";
                                        //Debugger.Break();
                                        break;
                                }
                            } else {
                                //Debugger.Break();
                            }
                        }

                        if (vl.VoiceConversation != 0) {
                            var convo = GetInstance<STUVoiceConversation>(vl.VoiceConversation);
                            conversationPosition = convo.m_voiceConversationLine.ToList().FindIndex(c => c.m_lineGUID == voiceLineInstance.GUIDx06F);
                            conversationGuid = vl.VoiceConversation;
                        }
                    }

                    foreach (var sound in voiceLineInstance.SoundFiles)
                        soundList.Add(new SoundInfo(heroNameActual, sound, voiceLineInstance.VoiceStimulus, subtitle, conversationGuid, conversationPosition, skin, otherStuff));
                }
            }

            return true;
        }
    }
}