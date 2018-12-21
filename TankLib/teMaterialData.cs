﻿using System.IO;
using System.Runtime.InteropServices;
using TankLib.Helpers;

namespace TankLib {
    /// <summary>Tank Material Data, file type 0B3</summary>
    public class teMaterialData {
        /// <summary>Header data</summary>
        public MatDataHeader Header;

        /// <summary>Constant buffer definitions</summary>
        public teMaterialDataStaticInput[] StaticInputs;

        /// <summary>Texture definitions</summary>
        public Texture[] Textures;

        /// <summary>Unknown definitions</summary>
        public Unknown[] Unknowns;

        /// <summary>Load material data from a stream</summary>
        public teMaterialData(Stream stream) {
            using (var reader = new BinaryReader(stream)) {
                Header = reader.Read<MatDataHeader>();

                if (Header.TextureOffset > 0) {
                    reader.BaseStream.Position = Header.TextureOffset;

                    Textures = reader.ReadArray<Texture>(Header.TextureCount);
                }

                if (Header.Offset4 > 0) {
                    reader.BaseStream.Position = Header.Offset4;

                    Unknowns = reader.ReadArray<Unknown>(Header.Offset4Count);
                }

                if (Header.StaticInputsOffset > 0 && Header.StaticInputCount != -1) {
                    reader.BaseStream.Position = Header.StaticInputsOffset;
                    StaticInputs               = new teMaterialDataStaticInput[Header.StaticInputCount];
                    for (var i = 0; i < Header.StaticInputCount; i++) StaticInputs[i] = new teMaterialDataStaticInput(reader);
                }
            }
        }

        public Texture GetTexture(uint hash) {
            if (Textures == null) return default;
            foreach (var texture in Textures)
                if (texture.NameHash == hash)
                    return texture;
            return default;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MatDataHeader {
            /// <summary>Offset to static input definitions</summary>
            public long StaticInputsOffset; // 0

            public long Offset2; // 8

            /// <summary>Offset to texture definitions</summary>
            public long TextureOffset; // 16

            public long Offset4; // 24

            /// <summary>Number of static inputs </summary>
            public short StaticInputCount; // 28

            public short Unk;

            /// <summary>Texture definition count</summary>
            public byte TextureCount;

            public byte   Offset4Count;
            public ushort Unknown4;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Unknown {
            public ulong A;
            public ulong B;
        }

        /// <summary>MaterialData Texture</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Texture {
            /// <summary>Texture GUID</summary>
            /// <remarks>File type 004</remarks>
            public teResourceGUID TextureGUID;

            /// <summary>CRC32 of input name</summary>
            /// <remarks>Matches up on teShaderInstance</remarks>
            public uint NameHash;

            /// <summary>Unknown flags</summary>
            public byte Flags;
        }
    }

    public class teMaterialDataStaticInput {
        public byte[] Data;

        public HeaderData Header;

        public unsafe teMaterialDataStaticInput(BinaryReader reader) {
            using (var rms = new RememberMeStream(reader, sizeof(HeaderData))) {
                Header                     = reader.Read<HeaderData>();
                reader.BaseStream.Position = rms.Position + Header.Offset;
                Data                       = reader.ReadBytes(Header.Size);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct HeaderData {
            public uint  Hash;
            public short Offset;
            public short Size;
        }
    }
}
