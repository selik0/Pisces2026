using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GameProto
{
    /// <summary>
    /// 一个 protobuf 消息字段的定义，用于代码生成。
    /// </summary>
    public sealed class ProtoFieldDef
    {
        public ProtoFieldDef(int fieldNumber, string name, Type clrType, bool packed = false, bool zigZag = false)
        {
            if (fieldNumber <= 0 || fieldNumber > ProtoWireFormat.FieldNumberMax)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldNumber));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (clrType == null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }
            FieldNumber = fieldNumber;
            Name = name;
            ClrType = clrType;
            Packed = packed;
            ZigZag = zigZag;
        }

        /// <summary>字段编号（1 到 2^29-1）。</summary>
        public int FieldNumber { get; }

        /// <summary>C# 成员名（必须是合法标识符）。</summary>
        public string Name { get; }

        /// <summary>成员 CLR 类型（int/string/byte[]/List&lt;T&gt;/Dictionary&lt;K,V&gt;/消息类型）。</summary>
        public Type ClrType { get; }

        /// <summary>重复数值字段是否使用 packed 编码。</summary>
        public bool Packed { get; }

        /// <summary>int/long 字段是否使用 zigzag（sint32/sint64）。</summary>
        public bool ZigZag { get; }
    }

    /// <summary>
    /// protobuf 消息代码生成器：把字段定义生成"直写"版 struct 源码。
    /// 生成的 struct 自带手写 WriteTo/ComputeSize/MergeFrom（按字段号 switch 直接赋值），
    /// 不依赖反射、不依赖表达式树编译，IL2CPP/AOT 完全安全。
    /// 生成时默认按内存对齐重排字段声明顺序（大对齐在前，减少 padding）。
    /// 供 Excel 导出器（GameEngineEditor）生成配置类使用，也可对已有 struct 重新生成。
    /// </summary>
    public static class ProtoCodeGen
    {
        /// <summary>
        /// 从已有类型反射其 [ProtoField] 成员，得到字段定义列表（按字段号升序）。
        /// </summary>
        public static IReadOnlyList<ProtoFieldDef> GetFieldDefinitions(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            var list = new List<ProtoFieldDef>();
            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                MemberInfo[] members = current.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (MemberInfo member in members)
                {
                    if (member.MemberType != MemberTypes.Field && member.MemberType != MemberTypes.Property)
                    {
                        continue;
                    }
                    ProtoFieldAttribute attribute =
                        Attribute.GetCustomAttribute(member, typeof(ProtoFieldAttribute), true) as ProtoFieldAttribute;
                    if (attribute == null)
                    {
                        continue;
                    }
                    Type memberType = member is FieldInfo
                        ? ((FieldInfo)member).FieldType
                        : ((PropertyInfo)member).PropertyType;
                    list.Add(new ProtoFieldDef(attribute.FieldNumber, member.Name, memberType,
                        attribute.Packed, attribute.ZigZag));
                }
            }
            list.Sort((x, y) => x.FieldNumber.CompareTo(y.FieldNumber));
            return list;
        }

        /// <summary>
        /// 按内存对齐重排字段（稳定排序：对齐大小降序，同对齐按字段号升序）。
        /// 大对齐字段（long/double/引用）在前，小对齐字段（byte/bool）在后，减少 padding。
        /// 仅影响 struct 内存布局，不影响 protobuf 字段编号与序列化格式。
        /// </summary>
        public static void SortForAlignment(IList<ProtoFieldDef> fields)
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }
            List<ProtoFieldDef> sorted = fields
                .OrderByDescending(f => GetAlignment(f.ClrType))
                .ThenBy(f => f.FieldNumber)
                .ToList();
            fields.Clear();
            foreach (ProtoFieldDef field in sorted)
            {
                fields.Add(field);
            }
        }

        /// <summary>从已有类型生成直写版 struct 源码。</summary>
        public static string GenerateStruct(Type type, string typeName = null)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            return GenerateStruct(typeName ?? type.Name, GetFieldDefinitions(type));
        }

        /// <summary>从字段定义生成直写版 struct 源码（默认做内存对齐排序）。</summary>
        public static string GenerateStruct(string typeName, IReadOnlyList<ProtoFieldDef> fields,
            bool sortByAlignment = true)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }
            var declared = new List<ProtoFieldDef>(fields);
            if (sortByAlignment)
            {
                SortForAlignment(declared);   // 字段声明顺序：按内存对齐重排
            }
            var byNumber = new List<ProtoFieldDef>(declared);
            byNumber.Sort((x, y) => x.FieldNumber.CompareTo(y.FieldNumber)); // 三方法遍历：按字段号
            var declaredInfos = new List<FieldInfoEx>();
            foreach (ProtoFieldDef def in declared)
            {
                declaredInfos.Add(FieldInfoEx.Create(def));
            }
            var numberInfos = new List<FieldInfoEx>();
            foreach (ProtoFieldDef def in byNumber)
            {
                numberInfos.Add(FieldInfoEx.Create(def));
            }

            var sb = new StringBuilder();
            sb.AppendLine("// 由 ProtoCodeGen 自动生成，请勿手动修改");
            sb.AppendLine("public struct " + typeName + " : IProtoMessage");
            sb.AppendLine("{");

            // 字段声明（按对齐排序后的顺序，减少 padding）
            foreach (FieldInfoEx info in declaredInfos)
            {
                var attrParts = new List<string>();
                if (info.Def.Packed)
                {
                    attrParts.Add("Packed = true");
                }
                if (info.Def.ZigZag)
                {
                    attrParts.Add("ZigZag = true");
                }
                string attrInner = attrParts.Count > 0 ? ", " + string.Join(", ", attrParts) : string.Empty;
                sb.AppendLine("    [ProtoField(" + info.Def.FieldNumber + attrInner + ")]");
                sb.AppendLine("    public " + GetTypeName(info.Def.ClrType) + " " + info.Def.Name + ";");
                sb.AppendLine();
            }

            EmitWriteTo(sb, numberInfos);
            sb.AppendLine();
            EmitComputeSize(sb, numberInfos);
            sb.AppendLine();
            EmitMergeFrom(sb, numberInfos);

            sb.AppendLine("}");
            return sb.ToString();
        }

        // ────────────────────────────────────────────────
        //  字段分类
        // ────────────────────────────────────────────────

        private enum GenKind
        {
            Scalar,
            String,
            Bytes,
            Message,
            Repeated,
            Map,
        }

        private sealed class FieldInfoEx
        {
            public ProtoFieldDef Def;
            public GenKind Kind;
            public GenKind ElementKind;
            public Type ElementType;
            public Type MapKeyType;
            public Type MapValueType;
            public string WireType;        // 标量/元素的期望 wire type（C# 枚举名）
            public string ElementWireType; // repeated 元素的期望 wire type

            public static FieldInfoEx Create(ProtoFieldDef def)
            {
                Type type = def.ClrType;
                var info = new FieldInfoEx { Def = def };
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    Type[] args = type.GetGenericArguments();
                    info.Kind = GenKind.Map;
                    info.MapKeyType = args[0];
                    info.MapValueType = args[1];
                    ClassifyValueType(args[1], out info.ElementKind, out info.ElementWireType);
                    info.ElementType = args[1];
                    info.WireType = "LengthDelimited";
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    info.Kind = GenKind.Repeated;
                    info.ElementType = type.GetGenericArguments()[0];
                    ClassifyValueType(info.ElementType, out info.ElementKind, out info.ElementWireType);
                    info.WireType = info.ElementWireType;
                }
                else
                {
                    ClassifyValueType(type, out info.Kind, out info.WireType);
                    info.ElementKind = info.Kind;
                    info.ElementType = type;
                }
                return info;
            }

            private static void ClassifyValueType(Type type, out GenKind kind, out string wireType)
            {
                if (type == typeof(int) || type == typeof(long) || type == typeof(uint) ||
                    type == typeof(ulong) || type == typeof(bool) || type.IsEnum)
                {
                    kind = GenKind.Scalar;
                    wireType = "Varint";
                }
                else if (type == typeof(float))
                {
                    kind = GenKind.Scalar;
                    wireType = "Fixed32";
                }
                else if (type == typeof(double))
                {
                    kind = GenKind.Scalar;
                    wireType = "Fixed64";
                }
                else if (type == typeof(string))
                {
                    kind = GenKind.String;
                    wireType = "LengthDelimited";
                }
                else if (type == typeof(byte[]))
                {
                    kind = GenKind.Bytes;
                    wireType = "LengthDelimited";
                }
                else if (typeof(IProtoMessage).IsAssignableFrom(type))
                {
                    kind = GenKind.Message;
                    wireType = "LengthDelimited";
                }
                else
                {
                    throw new ProtoProtocolException("不支持的 protobuf 字段类型：" + type);
                }
            }
        }

        // ────────────────────────────────────────────────
        //  WriteTo
        // ────────────────────────────────────────────────

        private static void EmitWriteTo(StringBuilder sb, IList<FieldInfoEx> infos)
        {
            sb.AppendLine("    public void WriteTo(ProtoWriter writer)");
            sb.AppendLine("    {");
            foreach (FieldInfoEx info in infos)
            {
                EmitWriteField(sb, "        ", info, info.Def.Name);
            }
            sb.AppendLine("    }");
        }

        private static void EmitWriteField(StringBuilder sb, string indent, FieldInfoEx info, string valueExpr)
        {
            switch (info.Kind)
            {
                case GenKind.Scalar:
                    sb.AppendLine(indent + "if (" + NotDefaultExpr(info, valueExpr) + ")");
                    sb.AppendLine(indent + "{");
                    sb.AppendLine(indent + "    writer.WriteTag(" + info.Def.FieldNumber + ", ProtoWireType." + info.WireType + ");");
                    sb.AppendLine(indent + "    " + WriteExpr("writer", valueExpr, info.ElementType, info.Def.ZigZag) + ";");
                    sb.AppendLine(indent + "}");
                    break;
                case GenKind.String:
                    sb.AppendLine(indent + "if (!string.IsNullOrEmpty(" + valueExpr + "))");
                    sb.AppendLine(indent + "{");
                    sb.AppendLine(indent + "    writer.WriteTag(" + info.Def.FieldNumber + ", ProtoWireType.LengthDelimited);");
                    sb.AppendLine(indent + "    writer.WriteString(" + valueExpr + ");");
                    sb.AppendLine(indent + "}");
                    break;
                case GenKind.Bytes:
                    sb.AppendLine(indent + "if (" + valueExpr + " != null && " + valueExpr + ".Length > 0)");
                    sb.AppendLine(indent + "{");
                    sb.AppendLine(indent + "    writer.WriteTag(" + info.Def.FieldNumber + ", ProtoWireType.LengthDelimited);");
                    sb.AppendLine(indent + "    writer.WriteBytes(" + valueExpr + ");");
                    sb.AppendLine(indent + "}");
                    break;
                case GenKind.Message:
                    sb.AppendLine(indent + "if (" + NotDefaultExpr(info, valueExpr) + ")");
                    sb.AppendLine(indent + "{");
                    sb.AppendLine(indent + "    writer.WriteTag(" + info.Def.FieldNumber + ", ProtoWireType.LengthDelimited);");
                    sb.AppendLine(indent + "    writer.WriteMessage(" + valueExpr + ");");
                    sb.AppendLine(indent + "}");
                    break;
                case GenKind.Repeated:
                    EmitWriteRepeated(sb, indent, info, valueExpr);
                    break;
                case GenKind.Map:
                    EmitWriteMap(sb, indent, info, valueExpr);
                    break;
                default:
                    throw new ProtoProtocolException("无效的字段类型");
            }
        }

        private static void EmitWriteRepeated(StringBuilder sb, string indent, FieldInfoEx info, string valueExpr)
        {
            string listExpr = valueExpr;
            sb.AppendLine(indent + "if (" + listExpr + " != null && " + listExpr + ".Count > 0)");
            sb.AppendLine(indent + "{");
            if (info.Def.Packed && info.ElementKind == GenKind.Scalar)
            {
                // packed：先累加总大小，再写 [tag][length][数据]
                sb.AppendLine(indent + "    int __size = 0;");
                sb.AppendLine(indent + "    for (int __i = 0; __i < " + listExpr + ".Count; __i++)");
                sb.AppendLine(indent + "    {");
                sb.AppendLine(indent + "        __size += " + SizeExpr(listExpr + "[__i]", info.ElementType, info.Def.ZigZag) + ";");
                sb.AppendLine(indent + "    }");
                sb.AppendLine(indent + "    writer.WriteTag(" + info.Def.FieldNumber + ", ProtoWireType.LengthDelimited);");
                sb.AppendLine(indent + "    writer.WriteLength(__size);");
                sb.AppendLine(indent + "    for (int __i = 0; __i < " + listExpr + ".Count; __i++)");
                sb.AppendLine(indent + "    {");
                sb.AppendLine(indent + "        " + WriteRawExpr("writer", listExpr + "[__i]", info.ElementType, info.Def.ZigZag) + ";");
                sb.AppendLine(indent + "    }");
            }
            else
            {
                sb.AppendLine(indent + "    for (int __i = 0; __i < " + listExpr + ".Count; __i++)");
                sb.AppendLine(indent + "    {");
                sb.AppendLine(indent + "        writer.WriteTag(" + info.Def.FieldNumber + ", ProtoWireType." + info.ElementWireType + ");");
                sb.AppendLine(indent + "        " + WriteExpr("writer", listExpr + "[__i]", info.ElementType, info.Def.ZigZag) + ";");
                sb.AppendLine(indent + "    }");
            }
            sb.AppendLine(indent + "}");
        }

        private static void EmitWriteMap(StringBuilder sb, string indent, FieldInfoEx info, string valueExpr)
        {
            string kvType = "KeyValuePair<" + GetTypeName(info.MapKeyType) + ", " + GetTypeName(info.MapValueType) + ">";
            sb.AppendLine(indent + "if (" + valueExpr + " != null && " + valueExpr + ".Count > 0)");
            sb.AppendLine(indent + "{");
            sb.AppendLine(indent + "    foreach (" + kvType + " __kv in " + valueExpr + ")");
            sb.AppendLine(indent + "    {");
            sb.AppendLine(indent + "        int __entrySize = " + MapKeySizeExpr("__kv.Key", info.MapKeyType) + " + " +
                               MapValueSizeExpr("__kv.Value", info) + ";");
            sb.AppendLine(indent + "        writer.WriteTag(" + info.Def.FieldNumber + ", ProtoWireType.LengthDelimited);");
            sb.AppendLine(indent + "        writer.WriteLength(__entrySize);");
            sb.AppendLine(indent + "        " + WriteMapKey("writer", "__kv.Key", info.MapKeyType) + ";");
            sb.AppendLine(indent + "        " + WriteMapValue("writer", "__kv.Value", info) + ";");
            sb.AppendLine(indent + "    }");
            sb.AppendLine(indent + "}");
        }

        // ────────────────────────────────────────────────
        //  ComputeSize
        // ────────────────────────────────────────────────

        private static void EmitComputeSize(StringBuilder sb, IList<FieldInfoEx> infos)
        {
            sb.AppendLine("    public int ComputeSize()");
            sb.AppendLine("    {");
            sb.AppendLine("        int __size = 0;");
            foreach (FieldInfoEx info in infos)
            {
                EmitSizeField(sb, "        ", info, info.Def.Name);
            }
            sb.AppendLine("        return __size;");
            sb.AppendLine("    }");
        }

        private static void EmitSizeField(StringBuilder sb, string indent, FieldInfoEx info, string valueExpr)
        {
            switch (info.Kind)
            {
                case GenKind.Scalar:
                case GenKind.String:
                case GenKind.Bytes:
                case GenKind.Message:
                    sb.AppendLine(indent + "if (" + NotDefaultExpr(info, valueExpr) + ")");
                    sb.AppendLine(indent + "{");
                    sb.AppendLine(indent + "    __size += " + FieldSizeExpr(info, valueExpr) + ";");
                    sb.AppendLine(indent + "}");
                    break;
                case GenKind.Repeated:
                    EmitSizeRepeated(sb, indent, info, valueExpr);
                    break;
                case GenKind.Map:
                    EmitSizeMap(sb, indent, info, valueExpr);
                    break;
                default:
                    throw new ProtoProtocolException("无效的字段类型");
            }
        }

        private static void EmitSizeRepeated(StringBuilder sb, string indent, FieldInfoEx info, string valueExpr)
        {
            sb.AppendLine(indent + "if (" + valueExpr + " != null && " + valueExpr + ".Count > 0)");
            sb.AppendLine(indent + "{");
            if (info.Def.Packed && info.ElementKind == GenKind.Scalar)
            {
                sb.AppendLine(indent + "    int __packed = 0;");
                sb.AppendLine(indent + "    for (int __i = 0; __i < " + valueExpr + ".Count; __i++)");
                sb.AppendLine(indent + "    {");
                sb.AppendLine(indent + "        __packed += " + SizeExpr(valueExpr + "[__i]", info.ElementType, info.Def.ZigZag) + ";");
                sb.AppendLine(indent + "    }");
                sb.AppendLine(indent + "    __size += " + TagSizeExpr(info.Def.FieldNumber, "LengthDelimited") + " + " +
                               "ProtoWireFormat.GetVarintSize((ulong)__packed) + __packed;");
            }
            else
            {
                sb.AppendLine(indent + "    for (int __i = 0; __i < " + valueExpr + ".Count; __i++)");
                sb.AppendLine(indent + "    {");
                sb.AppendLine(indent + "        __size += " + TagSizeExpr(info.Def.FieldNumber, info.ElementWireType) + " + " +
                               ElementSizeExpr(valueExpr + "[__i]", info) + ";");
                sb.AppendLine(indent + "    }");
            }
            sb.AppendLine(indent + "}");
        }

        /// <summary>重复字段元素（不含 tag）的序列化大小表达式。</summary>
        private static string ElementSizeExpr(string valueExpr, FieldInfoEx info)
        {
            switch (info.ElementKind)
            {
                case GenKind.Scalar:
                    return SizeExpr(valueExpr, info.ElementType, info.Def.ZigZag);
                case GenKind.String:
                    return "ProtoWireFormat.GetVarintSize((ulong)System.Text.Encoding.UTF8.GetByteCount(" + valueExpr + ")) + " +
                           "System.Text.Encoding.UTF8.GetByteCount(" + valueExpr + ")";
                case GenKind.Bytes:
                    return "ProtoWireFormat.GetVarintSize((ulong)" + valueExpr + ".Length) + " + valueExpr + ".Length";
                case GenKind.Message:
                    return "ProtoWireFormat.GetVarintSize((ulong)" + valueExpr + ".ComputeSize()) + " + valueExpr + ".ComputeSize()";
                default:
                    throw new ProtoProtocolException("无效的重复元素类型");
            }
        }

        private static void EmitSizeMap(StringBuilder sb, string indent, FieldInfoEx info, string valueExpr)
        {
            string kvType = "KeyValuePair<" + GetTypeName(info.MapKeyType) + ", " + GetTypeName(info.MapValueType) + ">";
            sb.AppendLine(indent + "if (" + valueExpr + " != null && " + valueExpr + ".Count > 0)");
            sb.AppendLine(indent + "{");
            sb.AppendLine(indent + "    foreach (" + kvType + " __kv in " + valueExpr + ")");
            sb.AppendLine(indent + "    {");
            sb.AppendLine(indent + "        int __entrySize = " + MapKeySizeExpr("__kv.Key", info.MapKeyType) + " + " +
                               MapValueSizeExpr("__kv.Value", info) + ";");
            sb.AppendLine(indent + "        __size += " + TagSizeExpr(info.Def.FieldNumber, "LengthDelimited") + " + " +
                           "ProtoWireFormat.GetVarintSize((ulong)__entrySize) + __entrySize;");
            sb.AppendLine(indent + "    }");
            sb.AppendLine(indent + "}");
        }

        // ────────────────────────────────────────────────
        //  MergeFrom
        // ────────────────────────────────────────────────

        private static void EmitMergeFrom(StringBuilder sb, IList<FieldInfoEx> infos)
        {
            sb.AppendLine("    public void MergeFrom(ProtoReader reader)");
            sb.AppendLine("    {");
            sb.AppendLine("        while (true)");
            sb.AppendLine("        {");
            sb.AppendLine("            uint __tag = reader.ReadTag();");
            sb.AppendLine("            if (__tag == 0)");
            sb.AppendLine("            {");
            sb.AppendLine("                return;");
            sb.AppendLine("            }");
            sb.AppendLine("            int __field = ProtoWireFormat.GetFieldNumber(__tag);");
            sb.AppendLine("            ProtoWireType __wt = ProtoWireFormat.GetWireType(__tag);");
            sb.AppendLine("            switch (__field)");
            sb.AppendLine("            {");
            foreach (FieldInfoEx info in infos)
            {
                EmitMergeCase(sb, "                ", info);
            }
            sb.AppendLine("                default:");
            sb.AppendLine("                    reader.SkipField(__tag);");
            sb.AppendLine("                    break;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }

        private static void EmitMergeCase(StringBuilder sb, string indent, FieldInfoEx info)
        {
            sb.AppendLine(indent + "case " + info.Def.FieldNumber + ":");
            string bodyIndent = indent + "    ";
            switch (info.Kind)
            {
                case GenKind.Scalar:
                case GenKind.String:
                case GenKind.Bytes:
                    EmitMergeSimple(sb, bodyIndent, info);
                    break;
                case GenKind.Message:
                    EmitMergeMessage(sb, bodyIndent, info);
                    break;
                case GenKind.Repeated:
                    EmitMergeRepeated(sb, bodyIndent, info);
                    break;
                case GenKind.Map:
                    EmitMergeMap(sb, bodyIndent, info);
                    break;
                default:
                    throw new ProtoProtocolException("无效的字段类型");
            }
            sb.AppendLine(indent + "    break;");
            sb.AppendLine();
        }

        private static void EmitMergeSimple(StringBuilder sb, string indent, FieldInfoEx info)
        {
            sb.AppendLine(indent + "if (__wt == ProtoWireType." + info.WireType + ")");
            sb.AppendLine(indent + "{");
            sb.AppendLine(indent + "    " + info.Def.Name + " = " + ReadExpr("reader", info.ElementType, info.Def.ZigZag) + ";");
            sb.AppendLine(indent + "}");
            sb.AppendLine(indent + "else");
            sb.AppendLine(indent + "{");
            sb.AppendLine(indent + "    throw new ProtoProtocolException(\"字段 " + info.Def.FieldNumber + " wire type 不匹配\");");
            sb.AppendLine(indent + "}");
        }

        private static void EmitMergeMessage(StringBuilder sb, string indent, FieldInfoEx info)
        {
            sb.AppendLine(indent + "if (__wt == ProtoWireType.LengthDelimited)");
            sb.AppendLine(indent + "{");
            if (info.ElementType.IsValueType)
            {
                // struct 消息字段：装箱合并后回写
                sb.AppendLine(indent + "    object __box = " + info.Def.Name + ";");
                sb.AppendLine(indent + "    reader.ReadMessageInto((IProtoMessage)__box);");
                sb.AppendLine(indent + "    " + info.Def.Name + " = (" + GetTypeName(info.ElementType) + ")__box;");
            }
            else
            {
                sb.AppendLine(indent + "    if (" + info.Def.Name + " == null)");
                sb.AppendLine(indent + "    {");
                sb.AppendLine(indent + "        " + info.Def.Name + " = new " + GetTypeName(info.ElementType) + "();");
                sb.AppendLine(indent + "    }");
                sb.AppendLine(indent + "    reader.ReadMessageInto(" + info.Def.Name + ");");
            }
            sb.AppendLine(indent + "}");
            sb.AppendLine(indent + "else");
            sb.AppendLine(indent + "{");
            sb.AppendLine(indent + "    throw new ProtoProtocolException(\"字段 " + info.Def.FieldNumber + " wire type 不匹配\");");
            sb.AppendLine(indent + "}");
        }

        private static void EmitMergeRepeated(StringBuilder sb, string indent, FieldInfoEx info)
        {
            string listType = "List<" + GetTypeName(info.ElementType) + ">";
            string read = ReadExpr("reader", info.ElementType, info.Def.ZigZag);
            if (info.ElementKind == GenKind.Scalar && (info.Def.Packed || info.ElementWireType != "LengthDelimited"))
            {
                sb.AppendLine(indent + "if (__wt == ProtoWireType.LengthDelimited)");
                sb.AppendLine(indent + "{");
                sb.AppendLine(indent + "    " + listType + " __list = " + info.Def.Name + ";");
                sb.AppendLine(indent + "    if (__list == null)");
                sb.AppendLine(indent + "    {");
                sb.AppendLine(indent + "        __list = new " + listType + "();");
                sb.AppendLine(indent + "        " + info.Def.Name + " = __list;");
                sb.AppendLine(indent + "    }");
                sb.AppendLine(indent + "    reader.ReadLengthDelimited(__inner =>");
                sb.AppendLine(indent + "    {");
                sb.AppendLine(indent + "        while (!__inner.IsAtEnd)");
                sb.AppendLine(indent + "        {");
                sb.AppendLine(indent + "            __list.Add(" + ReadExpr("__inner", info.ElementType, info.Def.ZigZag) + ");");
                sb.AppendLine(indent + "        }");
                sb.AppendLine(indent + "    });");
                sb.AppendLine(indent + "}");
                sb.AppendLine(indent + "else if (__wt == ProtoWireType." + info.ElementWireType + ")");
                sb.AppendLine(indent + "{");
                sb.AppendLine(indent + "    if (" + info.Def.Name + " == null)");
                sb.AppendLine(indent + "    {");
                sb.AppendLine(indent + "        " + info.Def.Name + " = new " + listType + "();");
                sb.AppendLine(indent + "    }");
                sb.AppendLine(indent + "    " + info.Def.Name + ".Add(" + read + ");");
                sb.AppendLine(indent + "}");
                sb.AppendLine(indent + "else");
                sb.AppendLine(indent + "{");
                sb.AppendLine(indent + "    throw new ProtoProtocolException(\"字段 " + info.Def.FieldNumber + " wire type 不匹配\");");
                sb.AppendLine(indent + "}");
            }
            else
            {
                sb.AppendLine(indent + "if (__wt == ProtoWireType.LengthDelimited)");
                sb.AppendLine(indent + "{");
                sb.AppendLine(indent + "    if (" + info.Def.Name + " == null)");
                sb.AppendLine(indent + "    {");
                sb.AppendLine(indent + "        " + info.Def.Name + " = new " + listType + "();");
                sb.AppendLine(indent + "    }");
                sb.AppendLine(indent + "    " + info.Def.Name + ".Add(" + read + ");");
                sb.AppendLine(indent + "}");
                sb.AppendLine(indent + "else");
                sb.AppendLine(indent + "{");
                sb.AppendLine(indent + "    throw new ProtoProtocolException(\"字段 " + info.Def.FieldNumber + " wire type 不匹配\");");
                sb.AppendLine(indent + "}");
            }
        }

        private static void EmitMergeMap(StringBuilder sb, string indent, FieldInfoEx info)
        {
            string dictType = "Dictionary<" + GetTypeName(info.MapKeyType) + ", " + GetTypeName(info.MapValueType) + ">";
            sb.AppendLine(indent + "if (__wt == ProtoWireType.LengthDelimited)");
            sb.AppendLine(indent + "{");
            sb.AppendLine(indent + "    " + dictType + " __dict = " + info.Def.Name + ";");
            sb.AppendLine(indent + "    if (__dict == null)");
            sb.AppendLine(indent + "    {");
            sb.AppendLine(indent + "        __dict = new " + dictType + "();");
            sb.AppendLine(indent + "        " + info.Def.Name + " = __dict;");
            sb.AppendLine(indent + "    }");
            sb.AppendLine(indent + "    reader.ReadLengthDelimited(__inner =>");
            sb.AppendLine(indent + "    {");
            sb.AppendLine(indent + "        " + GetTypeName(info.MapKeyType) + " __key = default(" + GetTypeName(info.MapKeyType) + ");");
            sb.AppendLine(indent + "        " + GetTypeName(info.MapValueType) + " __value = default(" + GetTypeName(info.MapValueType) + ");");
            sb.AppendLine(indent + "        bool __hasKey = false;");
            sb.AppendLine(indent + "        bool __hasValue = false;");
            sb.AppendLine(indent + "        while (!__inner.IsAtEnd)");
            sb.AppendLine(indent + "        {");
            sb.AppendLine(indent + "            uint __t = __inner.ReadTag();");
            sb.AppendLine(indent + "            int __f2 = ProtoWireFormat.GetFieldNumber(__t);");
            sb.AppendLine(indent + "            if (__f2 == 1)");
            sb.AppendLine(indent + "            {");
            sb.AppendLine(indent + "                __key = " + ReadExpr("__inner", info.MapKeyType, false) + ";");
            sb.AppendLine(indent + "                __hasKey = true;");
            sb.AppendLine(indent + "            }");
            sb.AppendLine(indent + "            else if (__f2 == 2)");
            sb.AppendLine(indent + "            {");
            sb.AppendLine(indent + "                __value = " + ReadMapValueExpr("__inner", info) + ";");
            sb.AppendLine(indent + "                __hasValue = true;");
            sb.AppendLine(indent + "            }");
            sb.AppendLine(indent + "            else");
            sb.AppendLine(indent + "            {");
            sb.AppendLine(indent + "                __inner.SkipField(__t);");
            sb.AppendLine(indent + "            }");
            sb.AppendLine(indent + "        }");
            sb.AppendLine(indent + "        if (__hasKey && __hasValue)");
            sb.AppendLine(indent + "        {");
            sb.AppendLine(indent + "            __dict[__key] = __value;");
            sb.AppendLine(indent + "        }");
            sb.AppendLine(indent + "    });");
            sb.AppendLine(indent + "}");
            sb.AppendLine(indent + "else");
            sb.AppendLine(indent + "{");
            sb.AppendLine(indent + "    throw new ProtoProtocolException(\"字段 " + info.Def.FieldNumber + " wire type 不匹配\");");
            sb.AppendLine(indent + "}");
        }

        // ────────────────────────────────────────────────
        //  表达式辅助
        // ────────────────────────────────────────────────

        private static string NotDefaultExpr(FieldInfoEx info, string valueExpr)
        {
            switch (info.Kind)
            {
                case GenKind.Scalar:
                    return ScalarNotDefaultExpr(info.ElementType, valueExpr);
                case GenKind.String:
                    return "!string.IsNullOrEmpty(" + valueExpr + ")";
                case GenKind.Bytes:
                    return valueExpr + " != null && " + valueExpr + ".Length > 0";
                case GenKind.Repeated:
                case GenKind.Map:
                    return valueExpr + " != null && " + valueExpr + ".Count > 0";
                case GenKind.Message:
                    if (info.ElementType.IsValueType)
                    {
                        // 嵌套 struct：任一成员非默认即非默认（递归）
                        IReadOnlyList<ProtoFieldDef> subs = GetFieldDefinitions(info.ElementType);
                        var parts = new List<string>();
                        foreach (ProtoFieldDef sub in subs)
                        {
                            FieldInfoEx subInfo = FieldInfoEx.Create(sub);
                            parts.Add("(" + NotDefaultExpr(subInfo, valueExpr + "." + sub.Name) + ")");
                        }
                        if (parts.Count == 0)
                        {
                            return "false";
                        }
                        return string.Join(" || ", parts);
                    }
                    return valueExpr + " != null";
                default:
                    throw new ProtoProtocolException("无效的字段类型");
            }
        }

        private static string ScalarNotDefaultExpr(Type type, string valueExpr)
        {
            if (type == typeof(int) || type == typeof(long) || type == typeof(uint) || type == typeof(ulong))
            {
                return valueExpr + " != 0";
            }
            if (type == typeof(bool))
            {
                return valueExpr;
            }
            if (type == typeof(float))
            {
                return valueExpr + " != 0f";
            }
            if (type == typeof(double))
            {
                return valueExpr + " != 0d";
            }
            if (type.IsEnum)
            {
                return "(long)" + valueExpr + " != 0";
            }
            throw new ProtoProtocolException("不支持的标量类型：" + type);
        }

        private static string FieldSizeExpr(FieldInfoEx info, string valueExpr)
        {
            switch (info.Kind)
            {
                case GenKind.Scalar:
                    return TagSizeExpr(info.Def.FieldNumber, info.WireType) + " + " +
                           SizeExpr(valueExpr, info.ElementType, info.Def.ZigZag);
                case GenKind.String:
                    return TagSizeExpr(info.Def.FieldNumber, "LengthDelimited") + " + " +
                           "ProtoWireFormat.GetVarintSize((ulong)System.Text.Encoding.UTF8.GetByteCount(" + valueExpr + ")) + " +
                           "System.Text.Encoding.UTF8.GetByteCount(" + valueExpr + ")";
                case GenKind.Bytes:
                    return TagSizeExpr(info.Def.FieldNumber, "LengthDelimited") + " + " +
                           "ProtoWireFormat.GetVarintSize((ulong)" + valueExpr + ".Length) + " + valueExpr + ".Length";
                case GenKind.Message:
                    return TagSizeExpr(info.Def.FieldNumber, "LengthDelimited") + " + " +
                           "ProtoWireFormat.GetVarintSize((ulong)" + valueExpr + ".ComputeSize()) + " + valueExpr + ".ComputeSize()";
                default:
                    throw new ProtoProtocolException("无效的字段类型");
            }
        }

        private static string SizeExpr(string valueExpr, Type type, bool zigzag)
        {
            if (type == typeof(int))
            {
                return zigzag
                    ? "ProtoWireFormat.GetVarintSize(ProtoWireFormat.EncodeZigZag32(" + valueExpr + "))"
                    : "ProtoWireFormat.GetVarintSize((ulong)(long)" + valueExpr + ")";
            }
            if (type == typeof(long))
            {
                return zigzag
                    ? "ProtoWireFormat.GetVarintSize(ProtoWireFormat.EncodeZigZag64(" + valueExpr + "))"
                    : "ProtoWireFormat.GetVarintSize((ulong)" + valueExpr + ")";
            }
            if (type == typeof(uint))
            {
                return "ProtoWireFormat.GetVarintSize(" + valueExpr + ")";
            }
            if (type == typeof(ulong))
            {
                return "ProtoWireFormat.GetVarintSize(" + valueExpr + ")";
            }
            if (type == typeof(bool))
            {
                return "1";
            }
            if (type == typeof(float))
            {
                return "4";
            }
            if (type == typeof(double))
            {
                return "8";
            }
            if (type.IsEnum)
            {
                return "ProtoWireFormat.GetVarintSize((ulong)(long)" + valueExpr + ")";
            }
            throw new ProtoProtocolException("不支持的标量类型：" + type);
        }

        private static string TagSizeExpr(int fieldNumber, string wireType)
        {
            return "ProtoWireFormat.GetVarintSize(ProtoWireFormat.MakeTag(" + fieldNumber + ", ProtoWireType." + wireType + "))";
        }

        private static string ReadExpr(string readerVar, Type type, bool zigzag)
        {
            if (type == typeof(int))
            {
                return readerVar + ".Read" + (zigzag ? "SInt32" : "Int32") + "()";
            }
            if (type == typeof(long))
            {
                return readerVar + ".Read" + (zigzag ? "SInt64" : "Int64") + "()";
            }
            if (type == typeof(uint))
            {
                return readerVar + ".ReadUInt32()";
            }
            if (type == typeof(ulong))
            {
                return readerVar + ".ReadUInt64()";
            }
            if (type == typeof(bool))
            {
                return readerVar + ".ReadBool()";
            }
            if (type == typeof(float))
            {
                return readerVar + ".ReadFloat()";
            }
            if (type == typeof(double))
            {
                return readerVar + ".ReadDouble()";
            }
            if (type.IsEnum)
            {
                return "(" + GetTypeName(type) + ")" + readerVar + ".ReadInt64()";
            }
            if (type == typeof(string))
            {
                return readerVar + ".ReadString()";
            }
            if (type == typeof(byte[]))
            {
                return readerVar + ".ReadBytes()";
            }
            if (typeof(IProtoMessage).IsAssignableFrom(type))
            {
                return readerVar + ".ReadMessage<" + GetTypeName(type) + ">()";
            }
            throw new ProtoProtocolException("不支持的字段类型：" + type);
        }

        private static string WriteExpr(string writerVar, string valueExpr, Type type, bool zigzag)
        {
            if (type == typeof(int))
            {
                return writerVar + ".Write" + (zigzag ? "SInt32" : "Int32") + "(" + valueExpr + ")";
            }
            if (type == typeof(long))
            {
                return writerVar + ".Write" + (zigzag ? "SInt64" : "Int64") + "(" + valueExpr + ")";
            }
            if (type == typeof(uint))
            {
                return writerVar + ".WriteUInt32(" + valueExpr + ")";
            }
            if (type == typeof(ulong))
            {
                return writerVar + ".WriteUInt64(" + valueExpr + ")";
            }
            if (type == typeof(bool))
            {
                return writerVar + ".WriteBool(" + valueExpr + ")";
            }
            if (type == typeof(float))
            {
                return writerVar + ".WriteFloat(" + valueExpr + ")";
            }
            if (type == typeof(double))
            {
                return writerVar + ".WriteDouble(" + valueExpr + ")";
            }
            if (type.IsEnum)
            {
                return writerVar + ".WriteInt64((long)" + valueExpr + ")";
            }
            if (type == typeof(string))
            {
                return writerVar + ".WriteString(" + valueExpr + ")";
            }
            if (type == typeof(byte[]))
            {
                return writerVar + ".WriteBytes(" + valueExpr + ")";
            }
            if (typeof(IProtoMessage).IsAssignableFrom(type))
            {
                return writerVar + ".WriteMessage(" + valueExpr + ")";
            }
            throw new ProtoProtocolException("不支持的字段类型：" + type);
        }

        private static string WriteRawExpr(string writerVar, string valueExpr, Type type, bool zigzag)
        {
            if (type == typeof(int))
            {
                return writerVar + ".WriteRawVarint((ulong)(long)" + valueExpr + ")";
            }
            if (type == typeof(long))
            {
                return writerVar + ".WriteRawVarint((ulong)" + valueExpr + ")";
            }
            if (type == typeof(uint))
            {
                return writerVar + ".WriteRawVarint(" + valueExpr + ")";
            }
            if (type == typeof(ulong))
            {
                return writerVar + ".WriteRawVarint(" + valueExpr + ")";
            }
            if (type == typeof(bool))
            {
                return writerVar + ".WriteRawByte(" + valueExpr + " ? (byte)1 : (byte)0)";
            }
            if (type == typeof(float))
            {
                return writerVar + ".WriteFloat(" + valueExpr + ")";
            }
            if (type == typeof(double))
            {
                return writerVar + ".WriteDouble(" + valueExpr + ")";
            }
            if (type.IsEnum)
            {
                return writerVar + ".WriteRawVarint((ulong)(long)" + valueExpr + ")";
            }
            if (zigzag)
            {
                return writerVar + ".WriteRawVarint(ProtoWireFormat.EncodeZigZag32(" + valueExpr + "))";
            }
            throw new ProtoProtocolException("不支持的标量类型：" + type);
        }

        private static string ReadMapValueExpr(string readerVar, FieldInfoEx info)
        {
            switch (info.ElementKind)
            {
                case GenKind.Scalar:
                    return ReadExpr(readerVar, info.MapValueType, false);
                case GenKind.String:
                case GenKind.Bytes:
                    return ReadExpr(readerVar, info.MapValueType, false);
                case GenKind.Message:
                    return readerVar + ".ReadMessage<" + GetTypeName(info.MapValueType) + ">()";
                default:
                    throw new ProtoProtocolException("无效的 map 值类型");
            }
        }

        private static string MapKeySizeExpr(string keyExpr, Type keyType)
        {
            if (keyType == typeof(string))
            {
                return TagSizeExpr(1, "LengthDelimited") + " + " +
                       "ProtoWireFormat.GetVarintSize((ulong)System.Text.Encoding.UTF8.GetByteCount(" + keyExpr + ")) + " +
                       "System.Text.Encoding.UTF8.GetByteCount(" + keyExpr + ")";
            }
            return TagSizeExpr(1, "Varint") + " + " + SizeExpr(keyExpr, keyType, false);
        }

        private static string MapValueSizeExpr(string valueExpr, FieldInfoEx info)
        {
            switch (info.ElementKind)
            {
                case GenKind.Scalar:
                    return TagSizeExpr(2, ScalarWireTypeName(info.MapValueType)) + " + " +
                           SizeExpr(valueExpr, info.MapValueType, false);
                case GenKind.String:
                    return TagSizeExpr(2, "LengthDelimited") + " + " +
                           "ProtoWireFormat.GetVarintSize((ulong)System.Text.Encoding.UTF8.GetByteCount(" + valueExpr + ")) + " +
                           "System.Text.Encoding.UTF8.GetByteCount(" + valueExpr + ")";
                case GenKind.Bytes:
                    return TagSizeExpr(2, "LengthDelimited") + " + " +
                           "ProtoWireFormat.GetVarintSize((ulong)" + valueExpr + ".Length) + " + valueExpr + ".Length";
                case GenKind.Message:
                    return TagSizeExpr(2, "LengthDelimited") + " + " +
                           "ProtoWireFormat.GetVarintSize((ulong)" + valueExpr + ".ComputeSize()) + " + valueExpr + ".ComputeSize()";
                default:
                    throw new ProtoProtocolException("无效的 map 值类型");
            }
        }

        private static string WriteMapKey(string writerVar, string keyExpr, Type keyType)
        {
            if (keyType == typeof(string))
            {
                return writerVar + ".WriteTag(1, ProtoWireType.LengthDelimited); " + writerVar + ".WriteString(" + keyExpr + ")";
            }
            return writerVar + ".WriteTag(1, ProtoWireType.Varint); " + WriteExpr(writerVar, keyExpr, keyType, false);
        }

        private static string WriteMapValue(string writerVar, string valueExpr, FieldInfoEx info)
        {
            switch (info.ElementKind)
            {
                case GenKind.Scalar:
                    return writerVar + ".WriteTag(2, ProtoWireType." + ScalarWireTypeName(info.MapValueType) + "); " +
                           WriteExpr(writerVar, valueExpr, info.MapValueType, false);
                case GenKind.String:
                    return writerVar + ".WriteTag(2, ProtoWireType.LengthDelimited); " + writerVar + ".WriteString(" + valueExpr + ")";
                case GenKind.Bytes:
                    return writerVar + ".WriteTag(2, ProtoWireType.LengthDelimited); " + writerVar + ".WriteBytes(" + valueExpr + ")";
                case GenKind.Message:
                    return writerVar + ".WriteTag(2, ProtoWireType.LengthDelimited); " + writerVar + ".WriteMessage(" + valueExpr + ")";
                default:
                    throw new ProtoProtocolException("无效的 map 值类型");
            }
        }

        /// <summary>标量类型的 wire type 名（Varint/Fixed32/Fixed64）。</summary>
        private static string ScalarWireTypeName(Type type)
        {
            if (type == typeof(float))
            {
                return "Fixed32";
            }
            if (type == typeof(double))
            {
                return "Fixed64";
            }
            return "Varint";
        }

        /// <summary>类型的自然对齐大小（用于内存对齐排序）。</summary>
        private static int GetAlignment(Type type)
        {
            if (type == null)
            {
                return 1;
            }
            if (type.IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }
            if (!type.IsValueType)
            {
                return IntPtr.Size; // string/byte[]/List/Dictionary/class 消息：8（x64）
            }
            if (type == typeof(bool) || type == typeof(byte) || type == typeof(sbyte))
            {
                return 1;
            }
            if (type == typeof(short) || type == typeof(ushort) || type == typeof(char))
            {
                return 2;
            }
            if (type == typeof(int) || type == typeof(uint) || type == typeof(float))
            {
                return 4;
            }
            if (type == typeof(long) || type == typeof(ulong) || type == typeof(double))
            {
                return 8;
            }
            if (type.IsValueType)
            {
                // 嵌套 struct：取其内部最大对齐
                int max = 1;
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                                            BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    max = Math.Max(max, GetAlignment(field.FieldType));
                }
                return max;
            }
            return IntPtr.Size;
        }

        /// <summary>生成 C# 类型名。</summary>
        private static string GetTypeName(Type type)
        {
            if (type == typeof(int))
            {
                return "int";
            }
            if (type == typeof(long))
            {
                return "long";
            }
            if (type == typeof(uint))
            {
                return "uint";
            }
            if (type == typeof(ulong))
            {
                return "ulong";
            }
            if (type == typeof(short))
            {
                return "short";
            }
            if (type == typeof(ushort))
            {
                return "ushort";
            }
            if (type == typeof(byte))
            {
                return "byte";
            }
            if (type == typeof(sbyte))
            {
                return "sbyte";
            }
            if (type == typeof(bool))
            {
                return "bool";
            }
            if (type == typeof(float))
            {
                return "float";
            }
            if (type == typeof(double))
            {
                return "double";
            }
            if (type == typeof(char))
            {
                return "char";
            }
            if (type == typeof(string))
            {
                return "string";
            }
            if (type == typeof(object))
            {
                return "object";
            }
            if (type == typeof(byte[]))
            {
                return "byte[]";
            }
            if (type.IsArray)
            {
                return GetTypeName(type.GetElementType()) + "[]";
            }
            if (type.IsGenericType)
            {
                string name = type.Name;
                int tick = name.IndexOf('`');
                if (tick >= 0)
                {
                    name = name.Substring(0, tick);
                }
                var argNames = new List<string>();
                foreach (Type arg in type.GetGenericArguments())
                {
                    argNames.Add(GetTypeName(arg));
                }
                return name + "<" + string.Join(", ", argNames) + ">";
            }
            return type.Name;
        }
    }
}
