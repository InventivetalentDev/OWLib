// Instance generated by TankLibHelper.InstanceBuilder

// ReSharper disable All
namespace TankLib.STU.Types {
    [STUAttribute(0x3AA5CB04, "STUMirroredIdleAnimComponent")]
    public class STUMirroredIdleAnimComponent : STUMirroredEntityComponent {
        [STUFieldAttribute(0xC9D669B6, "m_idleAnimation")]
        public teStructuredDataAssetRef<STUAnimation> m_idleAnimation;

        [STUFieldAttribute(0xD91EF907, "m_collisionModel")]
        public teStructuredDataAssetRef<ulong> m_collisionModel;

        [STUFieldAttribute(0x7D921E31, ReaderType = typeof(EmbeddedInstanceFieldReader))]
        public STUAnimGameData_Skeleton m_7D921E31;

        [STUFieldAttribute(0x25D54A00, ReaderType = typeof(EmbeddedInstanceFieldReader))]
        public STUAnimGameData_Animation m_25D54A00;

        [STUFieldAttribute(0x422B4A8E)]
        public int m_422B4A8E;

        [STUFieldAttribute(0xEDF6D105)]
        public float m_EDF6D105;

        [STUFieldAttribute(0x8D7C6FCE)]
        public uint m_8D7C6FCE;

        [STUFieldAttribute(0x5E009A60)]
        public byte m_5E009A60;
    }
}