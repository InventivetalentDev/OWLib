// File auto generated by STUHashTool
using static STULib.Types.Generic.Common;

namespace STULib.Types.Dump {
    [STU(0xF3A63FB4)]
    public class STU_F3A63FB4 : STUInstance {
        [STUField(0x8A5FECAC)]
        public STUGUID m_8A5FECAC;

        [STUField(0xA127B088)]
        public STUGUID m_A127B088;  // STU_9CADF2EC

        [STUField(0x54379800, EmbeddedInstance = true)]
        public STULib.Types.STUConfigVar[] m_54379800;

        [STUField(0xBAA74493, "m_condition", EmbeddedInstance = true)]
        public STULib.Types.STUConfigVar Condition;

        [STUField(0xBB16810A, "m_priority", EmbeddedInstance = true)]
        public STULib.Types.STUConfigVar Priority;
    }
}
