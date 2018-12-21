﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TankLib;
using TankLib.Math;
using TankLib.STU;

namespace TankLibHelper {
    public class InstanceBuilder : ClassBuilder {
        private static readonly List<string> ImportTankMathTypes = new List<string> {
                                                                                        "teColorRGB",
                                                                                        "teColorRGBA",
                                                                                        "teEntityID",
                                                                                        "teMtx43A",
                                                                                        "teQuat",
                                                                                        "teUUID",
                                                                                        "teVec4",
                                                                                        "teVec3A",
                                                                                        "teVec3",
                                                                                        "teVec2",
                                                                                        "DBID"
                                                                                    };

        private readonly STUInstanceJSON _instance;
        private readonly string          _parentName;

        public InstanceBuilder(BuilderConfig config, StructuredDataInfo info, STUInstanceJSON instance) : base(config, info) {
            _instance = instance;

            Name = Info.GetInstanceName(_instance.Hash);
            if (instance.Parent != 0) _parentName = Info.GetInstanceName(_instance.Parent);
        }

        public override string BuildCSharp() {
            var builder       = new StringBuilder();
            var importBuilder = new StringBuilder(); // ahh rewrite

            var importedTankMath = false;
            var importedEnums    = false;

            {
                //WriteDefaultHeader(builder, "Instance", "TankLibHelper.InstanceBuilder");

                if (Info.KnownInstances.ContainsKey(_instance.Hash))
                    builder.AppendLine($"    [{nameof(STUAttribute)}(0x{_instance.Hash:X8}, \"{Name}\")]");
                else
                    builder.AppendLine($"    [{nameof(STUAttribute)}(0x{_instance.Hash:X8})]");

                if (_instance.Parent == 0)
                    builder.AppendLine($"    public class {Name} : {nameof(STUInstance)} {{");
                else
                    builder.AppendLine($"    public class {Name} : {_parentName} {{");
            }

            var i = 0;
            foreach (var field in _instance.Fields) {
                if (i != 0) builder.AppendLine();
                BuildFieldCSharp(field, builder);

                if (ImportTankMathTypes.Contains(field.Type) && !importedTankMath) {
                    importedTankMath = true;
                    importBuilder.AppendLine("using TankLib.Math;");
                }

                if ((field.SerializationType == 8 || field.SerializationType == 9) && !importedEnums) {
                    importedEnums = true;
                    importBuilder.AppendLine("using TankLib.STU.Types.Enums;");
                }

                i++;
            }

            {
                builder.AppendLine("    }"); // close class
                builder.AppendLine("}");     // close namespace
            }

            return GetDefaultHeader("Instance", "TankLibHelper.InstanceBuilder", importBuilder.ToString()) + builder;
        }

        private void BuildFieldCSharp(STUFieldJSON field, StringBuilder builder) {
            string attribute;
            {
                attribute = $"[{nameof(STUFieldAttribute)}(0x{field.Hash:X8}";

                if (Info.KnownFields.ContainsKey(field.Hash)) attribute += $", \"{Info.GetFieldName(field.Hash)}\"";

                if (field.SerializationType == 2 || field.SerializationType == 3) attribute += $", ReaderType = typeof({nameof(EmbeddedInstanceFieldReader)})";

                if (field.SerializationType == 4 || field.SerializationType == 5) attribute += $", ReaderType = typeof({nameof(InlineInstanceFieldReader)})";

                attribute += ")]";
            }

            string definition;

            {
                var type = GetFieldTypeCSharp(field) + GetFieldPostTypeCSharp(field);
                definition = $"{type} {Info.GetFieldName(field.Hash)}";
            }

            builder.AppendLine($"        {attribute}");
            builder.AppendLine($"        public {definition};");

            // todo: what is going on with stuunlock
        }

        private string GetFieldPostTypeCSharp(STUFieldJSON field) {
            if (field.SerializationType == 1 || field.SerializationType == 3 || field.SerializationType == 5 || field.SerializationType == 9 || field.SerializationType == 11 || field.SerializationType == 13)
                return "[]";
            return null;
        }


        private string GetFieldTypeCSharp(STUFieldJSON field) {
            if ((field.SerializationType == 2 || field.SerializationType == 3 || field.SerializationType == 4 || field.SerializationType == 5) && field.Type.StartsWith("STU_")) {
                var hash = uint.Parse(field.Type.Split('_')[1], NumberStyles.HexNumber);

                return Info.GetInstanceName(hash);
            }

            if (field.SerializationType == 7) {
                var hash = uint.Parse(field.Type.Split('_')[1], NumberStyles.HexNumber);
                return $"{nameof(teStructuredDataHashMap<STUInstance>)}<{Info.GetInstanceName(hash)}>";
            }

            if (field.SerializationType == 8 || field.SerializationType == 9) return Info.GetEnumName(uint.Parse(field.Type, NumberStyles.HexNumber));

            if (field.SerializationType == 10 || field.SerializationType == 11) return nameof(teStructuredDataAssetRef<ulong>) + "<ulong>";

            if (field.SerializationType == 12 || field.SerializationType == 13) {
                var hash = uint.Parse(field.Type.Split('_')[1], NumberStyles.HexNumber);
                if (!Info.Instances.ContainsKey(hash)) return nameof(teStructuredDataAssetRef<ulong>) + "<ulong>";
                return nameof(teStructuredDataAssetRef<ulong>) + $"<{Info.GetInstanceName(hash)}>";
            }

            switch (field.Type) {
                // primitives with factories
                case "u64":
                    return "ulong";
                case "u32":
                    return "uint";
                case "u16":
                    return "ushort";
                case "u8":
                    return "byte";

                case "s64":
                    return "long";
                case "s32":
                    return "int";
                case "s16":
                    return "short";
                case "s8":
                    return "sbyte";

                case "f64":
                    return "double";
                case "f32":
                    return "float";

                case "teString":
                    return nameof(teString);

                // structs
                case "teVec2":
                    return nameof(teVec2);
                case "teVec3":
                    return nameof(teVec3);
                case "teVec3A":
                    return nameof(teVec3A);
                case "teVec4":
                    return nameof(teVec4);
                case "teQuat":
                    return nameof(teQuat);
                case "teColorRGB":
                    return nameof(teColorRGB);
                case "teColorRGBA":
                    return nameof(teColorRGBA);
                case "teMtx43A":
                    return nameof(teMtx43A); // todo: supposed to be 4x4?
                case "teEntityID":
                    return nameof(teEntityID);
                case "teUUID":
                    return nameof(teUUID);
                case "teStructuredDataDateAndTime":
                    return nameof(teStructuredDataDateAndTime);

                // ISerializable_STU
                case "DBID":
                    return nameof(DBID);
            }

            throw new NotImplementedException();
        }
    }
}
