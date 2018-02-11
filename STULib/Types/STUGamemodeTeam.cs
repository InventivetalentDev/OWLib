// File auto generated by STUHashTool

using STULib.Types.Generic;

namespace STULib.Types {
    [STU(0xD797394C, "STUGamemodeTeam")]
    public class STUGamemodeTeam : Common.STUInstance {
        [STUField(0xA2781AA4)]
        public Common.STUGUID StateScriptA;  // STU_6BE90C5C

        [STUField(0x6F71E9AA)]
        public STUGamemodeVarValuePair[] m_6F71E9AA;

        [STUField(0x76E8C82A)]
        public Common.STUGUID StateScriptB;  // STU_6BE90C5C

        [STUField(0xEA2B516F)]
        public STUGamemodeBodyVars[] m_EA2B516F;

        [STUField(0x59C86C8D, EmbeddedInstance = true)]
        public STUCosmeticCollection AllowedHeroes;

        [STUField(0x33B0B2B6)]
        public Enums.STUEnumTeamType TeamType;  // ffa team = 4, normal = 1/2

        [STUField(0x7FA93ED4)]
        public int m_7FA93ED4;

        [STUField(0x8B3CD15B)]
        public int MaxPlayers;

        [STUField(0x170AA4B8)]
        public int MinPlayers;
    }
}


