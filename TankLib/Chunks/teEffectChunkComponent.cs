﻿using System.IO;
using System.Runtime.InteropServices;

namespace TankLib.Chunks {
    /// <inheritdoc />
    /// <summary>ECMP: Prefix chunk type that controls timing for the next chunk</summary>
    public class teEffectChunkComponent : IChunk {
        public float Duration;

        public ComponentHeader Header;

        public float  StartTime;
        public string ID => "ECMP";

        public void Parse(Stream stream) {
            using (var reader = new BinaryReader(stream)) {
                Header = reader.Read<ComponentHeader>();

                if (Header.StartTimeOffset != 0) {
                    reader.BaseStream.Position = Header.StartTimeOffset;
                    StartTime                  = reader.ReadSingle();
                }

                if (Header.DurationOffset != 0) {
                    reader.BaseStream.Position = Header.DurationOffset;
                    Duration                   = reader.ReadSingle();
                }
            }
        }

        /// <summary>ECMP header</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ComponentHeader { // this is messy and only just works
            public long           StartTimeOffset;
            public long           DurationOffset;
            public teResourceGUID Hardpoint;
            public float          FloatOneA;
            public float          FloatOneB;
            public long           Unk;
            public short          Index;

            // public short ShortFlag;
            // public short MinusOneShort;

            // public float StartTime; // no

            // public long Pad1;
            // public long Pad2;
            // public int Pad3;

            // public PMCEType Flag; // hmm
            // public short Unk3;
            // public short Unk4;
            // public short Unk5;
            // public short Unk6;
        }
    }
}
