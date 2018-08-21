using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.DataModels;
using DataTool.Flag;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using Newtonsoft.Json;
using TankLib;
using TankLib.STU.Types;
using Combo = DataTool.FindLogic.Combo;
using static DataTool.Helper.IO;
using STUHero = TankLib.STU.Types.STUHero;
using STUVoiceSetComponent = TankLib.STU.Types.STUVoiceSetComponent;

namespace DataTool.ToolLogic.Dump {
    [Tool("dump-hero-voice", Description = "Dumps all the strings", TrackTypes = new ushort[] { 0x75 }, CustomFlags = typeof(DumpFlags))]
    public class DumpHeroVoice : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public class SoundInfo {
            public string HeroName;
            public string SoundFile;
            public string StimulusSet;
            public string ConversationSet;
            public int? ConversationPos;
            public string Subtitle;

            [JsonIgnore]
            public ulong GUID;

            public SoundInfo(string heroName, ulong guid, ulong groupGuid, string subtitle, ulong conversationGuid, int? conversationPosition) {
                GUID = guid;
                HeroName = heroName;
                SoundFile = $"{teResourceGUID.LongKey(guid):X12}";
                StimulusSet = $"{teResourceGUID.LongKey(groupGuid):X12}";
                ConversationSet = conversationGuid == 0 ? null : $"{teResourceGUID.LongKey(conversationGuid):X12}";
                ConversationPos = conversationPosition;
                Subtitle = subtitle;
            }
        }

        public void Parse(ICLIFlags toolFlags) {
            List<SoundInfo> soundList = new List<SoundInfo>();
            
            foreach (ulong heroGuid in TrackedFiles[0x75]) {
                STUHero hero = GetInstance<STUHero>(heroGuid);
                if (hero == null) continue;
                
                string heroNameActual = (GetString(hero.m_0EDCE350) ?? $"Unknown{teResourceGUID.Index(heroGuid)}").TrimEnd(' ');
                
                STUVoiceSetComponent voiceSetComponent = GetInstance<STUVoiceSetComponent>(hero.m_gameplayEntity);

                if (voiceSetComponent?.m_voiceDefinition == null) continue;
                
                Combo.ComboInfo info = new Combo.ComboInfo();
                Combo.Find(info, voiceSetComponent.m_voiceDefinition);
                
                VoiceSet voiceSet = new VoiceSet(GetInstance<STUVoiceSet>(voiceSetComponent.m_voiceDefinition));
                Combo.VoiceSetInfo voiceSetInfo = info.VoiceSets[voiceSetComponent.m_voiceDefinition];
              
                foreach (var voicelineInstanceInfo in voiceSetInfo.VoiceLineInstances) {
                    foreach (var voiceLineInstance in voicelineInstanceInfo.Value) {
                        var subtitleInfo = GetInstance<STU_7A68A730>(voiceLineInstance.Subtitle);
                        var subtitle = subtitleInfo?.m_798027DE.m_text;
                        ulong conversationGuid = 0;
                        int? conversationPosition = null;

                        if (voiceSet.VoiceLines.ContainsKey(voiceLineInstance.GUIDx09B)) {
                           var vl = voiceSet.VoiceLines[voiceLineInstance.GUIDx09B];

                            if (vl.VoiceConversation != 0) {
                                var convo = GetInstance<STUVoiceConversation>(vl.VoiceConversation);
                                conversationPosition = convo.m_voiceConversationLine.ToList().FindIndex(c => c.m_lineGUID == voiceLineInstance.GUIDx09B);
                                conversationGuid = vl.VoiceConversation;
                            }
                        }

                        foreach (var sound in voiceLineInstance.SoundFiles) {
                            soundList.Add(new SoundInfo(heroNameActual, sound, voiceLineInstance.VoiceStimulus, subtitle, conversationGuid, conversationPosition));
                        }
                    }
                }
            }
            
            ParseJSON(
                soundList.OrderBy(s => s.GUID).ToList(),
                toolFlags as DumpFlags
            );
        }
    }
}