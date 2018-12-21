﻿using System.IO;
using System.Runtime.InteropServices;
using TankLib.Math;

namespace TankLib.Chunks {
    /// <inheritdoc />
    /// <summary>MHRP: Defines hardpoints for a model</summary>
    public class teModelChunk_Hardpoint : IChunk {
        /// <summary>Hardpoint definitions</summary>
        public Hardpoint[] Hardpoints;

        /// <summary>Header data</summary>
        public HardpointHeader Header;

        /// <summary>An unknown byte array after the hardpoint definitions</summary>
        public byte[] Unknown;

        public string ID => "MHRP";

        public void Parse(Stream input) {
            using (var reader = new BinaryReader(input)) {
                Header = reader.Read<HardpointHeader>();

                if (Header.HardpointOffset > 0) {
                    input.Position = Header.HardpointOffset;
                    Hardpoints     = reader.ReadArray<Hardpoint>(Header.HardpointCount);
                }

                if (Header.UnknownOffset > 0) {
                    input.Position = Header.UnknownOffset;
                    Unknown        = reader.ReadArray<byte>(Header.UnknownCount);
                }
            }
        }

        /// <summary>MHRP header</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct HardpointHeader {
            /// <summary>Number of hardpoints</summary>
            public int HardpointCount;

            /// <summary>Number of unknown values</summary>
            public int UnknownCount;

            /// <summary>Offset to hardpoint array</summary>
            public long HardpointOffset;

            /// <summary>Offset to unknown array</summary>
            public long UnknownOffset;
        }

        /// <summary>A single model hardpoint</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Hardpoint {
            /// <summary>4x4 matrix</summary>
            public teMtx44 Matrix;

            /// <summary>03C Hardpoint GUID</summary>
            public teResourceGUID GUID;

            /// <summary>012 Bone GUID</summary>
            public teResourceGUID ParentBone;

            public ulong Unknown1;
            public ulong Unknown2;
        }
    }
}
