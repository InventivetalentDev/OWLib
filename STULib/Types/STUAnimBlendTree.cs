// File auto generated by STUHashTool

using STULib.Types.Dump;
using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x67866D38, "STUAnimBlendTree")]
    public class STUAnimBlendTree : STUInstance {
        [STUField(0x85CC326B)]
        public STU_DF9B7DE2 m_85CC326B;

        [STUField(0x0B15B894, "m_animNodes", EmbeddedInstance = true)]
        public STUAnimNode_Base[] AnimNodes;

        [STUField(0xF9CA7995)]
        public uint[] m_F9CA7995;

        [STUField(0xBF7A74B0, EmbeddedInstance = true)]
        public STU_8C127DE2[] m_BF7A74B0;

        [STUField(0xD6497916, EmbeddedInstance = true)]
        public STU_CB30C7C3 m_D6497916;

        [STUField(0xA4712A0A)]
        public uint m_A4712A0A;

        [STUField(0x191CEC72)]
        public uint m_191CEC72;
    }
}


