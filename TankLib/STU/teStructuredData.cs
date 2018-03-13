﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using TankLib.Helpers.Hash;
using TankLib.STU.DataTypes;

namespace TankLib.STU {
    /// <summary>
    /// Structured Data parser
    /// </summary>
    public class teStructuredData {
        // ReSharper disable once InconsistentNaming
        /// <summary>"v1" STU magic number</summary>
        public const int STRUCTURED_DATA_IMMUTABLE_MAGIC = 0x53545544;
        public static readonly teStructuredDataMgr Manager = new teStructuredDataMgr();
        
        /// <summary>CRC64 of the header</summary>
        public ulong HeaderChecksum;
        
        /// <summary>Loaded instances</summary>
        public STUInstance[] Instances;
        
        /// <summary>Type of STU structure this asset uses</summary>
        public teStructuredDataFormat Format;
        
        #region V2
        public STUBag<STUInstance_Info> InstanceInfo;
        public STUBag<STUInlineArray_Info> InlinedTypesInfo;
        public STUBag<STUBag<STUField_Info>> FieldInfoBags;
        #endregion

        #region V1
        private Dictionary<long, STUInstance> _instanceOffsets;
        #endregion
        
        #region Streams
        public BinaryReader DynData;
        public BinaryReader Data;
        #endregion

        /// <summary>Data start position</summary>
        private long _startPos;
   
        /// <summary>Load STU asset from a stream</summary>
        /// <param name="stream">The stream to load from</param>
        /// <param name="keepOpen">Leave the stream open after reading</param>
        public teStructuredData(Stream stream, bool keepOpen=false) {
            _startPos = stream.Position;
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, keepOpen)) {
                uint magic = reader.ReadUInt32();

                if (magic == STRUCTURED_DATA_IMMUTABLE_MAGIC) {
                    Format = teStructuredDataFormat.V1;
                    DeserializeV1(reader);
                } else {
                    // "v2" stu with no magic :(
                    Format = teStructuredDataFormat.V2;
                    if (stream.Length < 36) throw new EndOfStreamException("Invalid STU asset. Truncated header.");
                    DeserializeV2(reader);
                }
            }
            FinishDeserialize();
        }

        /// <summary>Read a "Version1" STU asset</summary>
        private void DeserializeV1(BinaryReader reader) {
            _instanceOffsets = new Dictionary<long, STUInstance>();
            
            reader.BaseStream.Position = _startPos;

            STUHeaderV1 header = reader.Read<STUHeaderV1>();
            
            STUInstanceRecordV1[] instanceTable = new STUInstanceRecordV1[header.InstanceCount];
            Instances = new STUInstance[header.InstanceCount];
            for (int i = 0; i < header.InstanceCount; i++) {
                STUInstanceRecordV1 record = reader.Read<STUInstanceRecordV1>();
                instanceTable[i] = record;
                
                long position = reader.BaseStream.Position;
                reader.BaseStream.Position = record.Offset + _startPos;
                uint instanceHash = reader.ReadUInt32();
                uint nextOffset = reader.ReadUInt32();
                reader.BaseStream.Position = position;
                
                STUInstance instance = Manager.CreateInstance(instanceHash);
                _instanceOffsets[record.Offset] = instance;
                Instances[i] = instance;
            }

            Data = reader;  // hmm

            for (int i = 0; i < header.InstanceCount; i++) {
                STUInstanceRecordV1 record = instanceTable[i];

                if (Instances[i] == null) continue;
                reader.BaseStream.Position = record.Offset + _startPos;
                uint instanceHash = reader.ReadUInt32();
                uint nextOffset = reader.ReadUInt32();
                Instances[i].Deserialize(this);
            }
        }

        /// <summary>Read a "Version2" STU asset</summary>
        private void DeserializeV2(BinaryReader reader) {
            reader.BaseStream.Position = _startPos;
            HeaderChecksum = CRC.CRC64(reader.ReadBytes(36));
            reader.BaseStream.Position = _startPos;
            Helpers.SerializableHelper.Deserialize(reader, out InstanceInfo);
            Helpers.SerializableHelper.Deserialize(reader, out InlinedTypesInfo);
            Helpers.SerializableHelper.Deserialize(reader, out FieldInfoBags);
            
            int dynDataSize = reader.ReadInt32();
            int dynDataOff = reader.ReadInt32();
            int dataBufferOffset = reader.ReadInt32();
            if (dynDataSize > 0) {
                reader.BaseStream.Position = dynDataOff + _startPos;
                DynData = new BinaryReader(new MemoryStream(reader.ReadBytes(dynDataSize)));
            }
            if (dataBufferOffset < reader.BaseStream.Length) {
                long dataSize = reader.BaseStream.Length - dataBufferOffset;
                if (dataSize > int.MaxValue) throw new Exception("oops");

                reader.BaseStream.Position = dataBufferOffset + _startPos;
                Data = new BinaryReader(new MemoryStream(reader.ReadBytes((int)dataSize)));
            }

            if (InstanceInfo.Count > 0) {
                Instances = new STUInstance[InstanceInfo.Count];
                for (int i = 0; i < InstanceInfo.Count; i++) {
                    Instances[i] = Manager.CreateInstance(InstanceInfo[i].Hash);
                }
            }
            
            for (var i = 0; i != InstanceInfo.Count; ++i) {
                STUInstance_Info info = InstanceInfo[i];
                STUInstance instance = Instances[i];

                long startPosition = Data.Position();
                instance.Deserialize(this);
                long endPosition = Data.Position();

                Data.BaseStream.Position = startPosition + info.Size;
                //if (endPosition - startPosition != info.Size)
                //    throw new Exceptions.InvalidTypeSize($"read len != type size. Type: '{instance.GetName()}', Data offset: {startPosition}");
            }
        }

        /// <summary>Gets the STUInstance at an offset</summary>
        public STUInstance GetInstanceAtOffset(long offset) {
            if (Format != teStructuredDataFormat.V1) throw new InvalidOperationException();
            if (!_instanceOffsets.ContainsKey(offset)) return null;
            return _instanceOffsets[offset];
        }

        /// <summary>Cleanup after deserializing asset</summary>
        private void FinishDeserialize() {
            Data?.Dispose();
            DynData?.Dispose();
            Data = null;
            DynData = null;
        }

        /// <summary>The the primary instance of this asset</summary>
        public T GetMainInstance<T>() where T : STUInstance {
            if (Instances.Length == 0) return null;
            return Instances[0] as T;
        }
    }

    public enum teStructuredDataFormat {
        V1,
        V2
    }
}