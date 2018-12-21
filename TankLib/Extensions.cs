﻿using System.IO;
using System.Text;
using TankLib.Helpers;

namespace TankLib {
    public static class Extensions {
        #region BinaryReader

        /// <summary>
        ///     Read struct from a BinaryReader
        /// </summary>
        /// <param name="reader">Source reader</param>
        /// <typeparam name="T">Struct to read</typeparam>
        /// <returns>Read struct</returns>
        public static T Read<T>(this BinaryReader reader) where T : struct {
            var result = reader.ReadBytes(FastStruct<T>.Size);
            return FastStruct<T>.ArrayToStructure(result);
        }

        /// <summary>
        ///     Read array of structs from a BinaryReader
        /// </summary>
        /// <param name="reader">Source reader</param>
        /// <typeparam name="T">Struct to read</typeparam>
        /// <returns>Array of read structs</returns>
        public static T[] ReadArray<T>(this BinaryReader reader) where T : struct {
            var numBytes = (int) reader.ReadInt64();
            if (numBytes == 0) return new T[0];

            var result = reader.ReadBytes(numBytes);

            reader.BaseStream.Position += (0 - numBytes) & 0x07;
            return FastStruct<T>.ReadArray(result);
        }

        /// <summary>
        ///     Read array of structs from a reader
        /// </summary>
        /// <param name="reader">Target reader</param>
        /// <param name="count">Count</param>
        /// <typeparam name="T">Struct to read</typeparam>
        /// <returns>Stuct array</returns>
        public static T[] ReadArray<T>(this BinaryReader reader, int count) where T : struct {
            if (count == 0) return new T[0];

            var numBytes = FastStruct<T>.Size * count;

            var result = reader.ReadBytes(numBytes);

            return FastStruct<T>.ReadArray(result);
        }

        /// <summary>
        ///     Write a struct to a BinaryWriter
        /// </summary>
        /// <param name="writer">Target writer</param>
        /// <param name="struct">Struct instance to write</param>
        /// <typeparam name="T">Struct type</typeparam>
        public static void Write<T>(this BinaryWriter writer, T @struct) where T : struct { writer.Write(FastStruct<T>.StructureToArray(@struct)); }

        /// <summary>
        ///     Write an array of structs to a BinaryWriter
        /// </summary>
        /// <param name="writer">Target write</param>
        /// <param name="struct">Struct array to write</param>
        /// <typeparam name="T">Struct type</typeparam>
        public static void WriteStructArray<T>(this BinaryWriter writer, T[] @struct) where T : struct { writer.Write(FastStruct<T>.WriteArray(@struct)); }

        /// <summary>
        ///     Get BinaryReader position
        /// </summary>
        /// <param name="reader">Target reader</param>
        /// <returns>Reader position</returns>
        public static long Position(this BinaryReader reader) { return reader.BaseStream.Position; }

        // needs testing.
        /// <summary>
        ///     Read a string of specified size
        /// </summary>
        /// <param name="reader">Source reader</param>
        /// <param name="size">String length</param>
        /// <returns></returns>
        public static string ReadString(this BinaryReader reader, int size) {
            var bytes = reader.ReadBytes(size);
            return Encoding.UTF8.GetString(bytes);
        }


        public static long Seek(this BinaryReader reader, long offset, SeekOrigin origin = SeekOrigin.Begin) { return reader.BaseStream.Seek(offset, origin); }

        /// <summary>
        ///     Get BinaryReader size
        /// </summary>
        /// <param name="reader">Target reader</param>
        /// <returns>Size</returns>
        public static long Size(this BinaryReader reader) { return reader.BaseStream.Length; }

        public static void BoundaryAlign(this BinaryReader reader, int boundary) {
            var value           = reader.BaseStream.Position;
            var nearestMultiple = boundary * ((value - 1) / boundary + 1);
            reader.BaseStream.Position = nearestMultiple;
        }

        #endregion

        #region Random Utils

        /// <summary>
        ///     Reverse a string using XOR
        /// </summary>
        /// <param name="s">Source string</param>
        /// <returns>Reversed string</returns>
        public static string ReverseXor(this string s) {
            var charArray = s.ToCharArray();
            var len       = s.Length - 1;

            for (var i = 0; i < len; i++, len--) {
                charArray[i]   ^= charArray[len];
                charArray[len] ^= charArray[i];
                charArray[i]   ^= charArray[len];
            }

            return new string(charArray);
        }

        public static unsafe ulong SwapBytes(this ulong value, int b1, int b2) {
            var data = (byte*) &value;
            var a    = *(data + b1);
            var b    = *(data + b2);
            *(data + b1) = b;
            *(data + b2) = a;
            return value;
        }

        #endregion

        #region Textures

        public static TextureTypes.DDSPixelFormat ToPixelFormat(this TextureTypes.TextureType type) {
            var ret = new TextureTypes.DDSPixelFormat {
                                                          Size      = 32,
                                                          Flags     = 4,
                                                          FourCC    = (uint) type,
                                                          BitCount  = 32,
                                                          RedMask   = 0x0000FF00,
                                                          GreenMask = 0x00FF0000,
                                                          BlueMask  = 0xFF000000,
                                                          AlphaMask = 0x000000FF
                                                      };
            if (type == TextureTypes.TextureType.ATI2) ret.Flags |= 0x80000000;
            return ret;
        }

        public static uint ByteSize(this TextureTypes.TextureType T) { return T == TextureTypes.TextureType.DXT5 ? 16u : 8u; }

        #endregion
    }
}
