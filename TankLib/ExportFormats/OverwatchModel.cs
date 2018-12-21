﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpDX;
using TankLib.Chunks;
using TankLib.Math;

namespace TankLib.ExportFormats {
    /// <summary>
    ///     OWMDL format
    /// </summary>
    public class OverwatchModel : IExportFormat {
        private readonly teChunkedData _data;
        public           ulong         GUID;

        public string ModelLookFileName;
        public string Name;
        public sbyte  TargetLod;

        public OverwatchModel(teChunkedData chunkedData, ulong guid, sbyte targetLod = 1) {
            _data     = chunkedData;
            GUID      = guid;
            TargetLod = targetLod;
        }

        public string Extension => "owmdl";

        public void Write(Stream stream) {
            var renderMesh = _data.GetChunk<teModelChunk_RenderMesh>();
            var model      = _data.GetChunk<teModelChunk_Model>();
            var skeleton   = _data.GetChunk<teModelChunk_Skeleton>();
            var cloth      = _data.GetChunk<teModelChunk_Cloth>();
            var stu        = _data.GetChunk<teModelChunk_STU>();

            using (var writer = new BinaryWriter(stream)) {
                writer.Write((ushort) 1);
                writer.Write((ushort) 6);
                if (ModelLookFileName == null)
                    writer.Write((byte) 0);
                else
                    writer.Write(ModelLookFileName);
                if (Name == null)
                    writer.Write((byte) 0);
                else
                    writer.Write(Name);

                short[]                                       hierarchy    = null;
                Dictionary<int, teModelChunk_Cloth.ClothNode> clothNodeMap = null;
                if (skeleton != null) {
                    if (cloth != null)
                        hierarchy = cloth.CreateFakeHierarchy(skeleton, out clothNodeMap);
                    else
                        hierarchy = skeleton.Hierarchy;
                    writer.Write(skeleton.Header.BonesAbs);
                } else {
                    writer.Write((ushort) 0);
                }

                if (renderMesh == null) {
                    writer.Write(0u);
                    writer.Write(0);
                    return;
                }

                var submeshes = renderMesh.Submeshes.Where(x => x.Descriptor.LOD == TargetLod || x.Descriptor.LOD == -1)
                                          .ToArray();

                writer.Write((uint) submeshes.Length);

                if (stu?.StructuredData.m_hardPoints != null)
                    writer.Write(stu.StructuredData.m_hardPoints.Length);
                else
                    writer.Write(0); // hardpoints

                if (skeleton != null)
                    for (var i = 0; i < skeleton.Header.BonesAbs; ++i) {
                        writer.Write(GetBoneName(skeleton.IDs[i]));
                        var parent               = hierarchy[i];
                        if (parent == -1) parent = (short) i;
                        writer.Write(parent);

                        skeleton.GetWorldSpace(i, out var scale, out var rotation, out var translation);
                        writer.Write(translation);
                        writer.Write(scale);
                        writer.Write(rotation);
                    }

                var submeshIdx = 0;
                foreach (var submesh in submeshes) {
                    writer.Write($"Submesh_{submeshIdx}.{model.Materials[submesh.Descriptor.Material]:X16}");
                    writer.Write(model.Materials[submesh.Descriptor.Material]);
                    writer.Write(submesh.UVCount);

                    writer.Write(submesh.Vertices.Length);
                    writer.Write(submesh.Indices.Length / 3);

                    for (var j = 0; j < submesh.Vertices.Length; j++) {
                        writer.Write(submesh.Vertices[j]);
                        writer.Write(-submesh.Normals[j]
                                             .X);
                        writer.Write(-submesh.Normals[j]
                                             .Y);
                        writer.Write(-submesh.Normals[j]
                                             .Z);

                        foreach (var uv in submesh.UV[j]) writer.Write(uv);

                        if (skeleton != null && submesh.BoneIndices[j] != null) {
                            writer.Write((byte) 4);
                            writer.Write(skeleton.Lookup[submesh.BoneIndices[j][0]]);
                            writer.Write(skeleton.Lookup[submesh.BoneIndices[j][1]]);
                            writer.Write(skeleton.Lookup[submesh.BoneIndices[j][2]]);
                            writer.Write(skeleton.Lookup[submesh.BoneIndices[j][3]]);
                            writer.Write(submesh.BoneWeights[j][0]);
                            writer.Write(submesh.BoneWeights[j][1]);
                            writer.Write(submesh.BoneWeights[j][2]);
                            writer.Write(submesh.BoneWeights[j][3]);
                        } else {
                            writer.Write((byte) 0);
                        }

                        writer.Write(submesh.Color1.ElementAtOrDefault(j));
                        writer.Write(submesh.Color2.ElementAtOrDefault(j));
                    }

                    for (var j = 0; j < submesh.Descriptor.IndicesToDraw; j += 3) {
                        writer.Write((byte) 3);
                        writer.Write((int) submesh.Indices[j]);
                        writer.Write((int) submesh.Indices[j + 1]);
                        writer.Write((int) submesh.Indices[j + 2]);
                    }

                    submeshIdx++;
                }


                if (stu?.StructuredData.m_hardPoints != null) {
                    foreach (var hardPoint in stu.StructuredData.m_hardPoints) {
                        writer.Write(IdToString("hardpoint", teResourceGUID.Index(hardPoint.m_EDF0511C)));

                        var parentMat = Matrix.Identity;
                        if (hardPoint.m_FF592924 != 0 && skeleton != null) {
                            var boneIdx = skeleton.IDs.TakeWhile(id => id != teResourceGUID.Index(hardPoint.m_FF592924))
                                                  .Count();

                            parentMat = skeleton.GetWorldSpace(boneIdx);
                        }

                        var hardPointMat = Matrix.RotationQuaternion(hardPoint.m_rotation) * Matrix.Translation(hardPoint.m_position);

                        hardPointMat = hardPointMat * parentMat;
                        hardPointMat.Decompose(out _, out var rot, out var pos);

                        writer.Write(pos);
                        writer.Write(rot);
                    }

                    foreach (var modelHardpoint in stu.StructuredData.m_hardPoints) writer.Write(IdToString("bone", teResourceGUID.Index(modelHardpoint.m_FF592924)));
                }

                // ext 1.3: old cloth
                writer.Write(0);

                // ext 1.4: embedded refpose
                if (skeleton != null)
                    for (var i = 0; i < skeleton.Header.BonesAbs; ++i) {
                        writer.Write(IdToString("bone", skeleton.IDs[i]));
                        var parent = hierarchy[i];
                        writer.Write(parent);

                        GetRefPoseTransform(i, hierarchy, skeleton, clothNodeMap, out var scale, out var quat, out var translation);

                        var rot = quat.ToEulerAngles();
                        writer.Write(translation);
                        writer.Write(scale);
                        writer.Write(rot);
                    }

                writer.Write(teResourceGUID.Index(GUID));
            }
        }

        public static void GetRefPoseTransform(int                                           i,
                                               short[]                                       hierarchy,
                                               teModelChunk_Skeleton                         skeleton,
                                               Dictionary<int, teModelChunk_Cloth.ClothNode> clothNodeMap,
                                               out teVec3                                    scale,
                                               out teQuat                                    quat,
                                               out teVec3                                    translation) {
            if (clothNodeMap != null && clothNodeMap.ContainsKey(i)) {
                var thisMat   = skeleton.GetWorldSpace(i);
                var parentMat = skeleton.GetWorldSpace(hierarchy[i]);

                var newParentMat = thisMat * Matrix.Invert(parentMat);

                newParentMat.Decompose(out var scl2, out var rot2, out var pos2);

                quat        = new teQuat(rot2.X, rot2.Y, rot2.Z, rot2.W);
                scale       = new teVec3(scl2.X, scl2.Y, scl2.Z);
                translation = new teVec3(pos2.X, pos2.Y, pos2.Z);
            } else {
                var bone = skeleton.Matrices34Inverted[i];

                scale       = new teVec3(bone[1, 0], bone[1, 1], bone[1, 2]);
                quat        = new teQuat(bone[0, 0], bone[0, 1], bone[0, 2], bone[0, 3]);
                translation = new teVec3(bone[2, 0], bone[2, 1], bone[2, 2]);
            }
        }

        public static string GetBoneName(uint id) { return IdToString("bone", id); }

        public static string IdToString(string prefix, uint id) { // hmm
            return $"{prefix}_{id:X4}";
        }
    }
}
