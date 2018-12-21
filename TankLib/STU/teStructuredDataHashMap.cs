﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace TankLib.STU {
    public class teStructuredDataHashMap<T> : Dictionary<ulong, T>, ISerializable_STU where T : STUInstance {
        public uint   Unknown1;
        public uint[] Unknown2;

        public void Deserialize(teStructuredData assetFile, STUField_Info field) {
            var data    = assetFile.Data;
            var dynData = assetFile.DynData;

            var offset = data.ReadInt32();
            if (offset == -1) return;
            dynData.Seek(offset);
            var unk2Size = dynData.ReadInt32();
            if (unk2Size == 0) return;
            Unknown1 = dynData.ReadUInt32();
            var unk2Offset = dynData.ReadInt64();
            var dataOffset = dynData.ReadInt64();

            dynData.Seek(unk2Offset);
            Unknown2 = new uint[unk2Size];
            for (var i = 0; i != unk2Size; ++i) Unknown2[i] = dynData.ReadUInt32();

            dynData.Seek(dataOffset);
            var mapSize = Unknown2.Last();
            for (var i = 0; i != mapSize; ++i) {
                var key = dynData.ReadUInt64();
                // Last 4 bytes are padding for in-place deserialization.
                var value = (int) dynData.ReadUInt64();
                if (value == -1) {
                    Add(key, null);
                } else {
                    if (value < assetFile.Instances.Length) {
                        var stuType                        = assetFile.Instances[value];
                        if (stuType != null) stuType.Usage = TypeUsage.HashMap;

                        if (stuType is T casted) Add(key, casted);
                        else
                            throw new InvalidCastException($"Attempted to cast '{stuType?.GetType().Name}' to '{typeof(T).Name}'");
                    } else {
                        throw new ArgumentOutOfRangeException($"DataRoot index is out of range. Id: {value}, Type: STUHashMap<{typeof(T).Name}>, Data offset: {dynData.Position() - 8}");
                    }
                }
            }
        }

        public void Deserialize_Array(teStructuredData data, STUField_Info field) {
            // Not possible
            throw new NotImplementedException();
        }
    }
}
