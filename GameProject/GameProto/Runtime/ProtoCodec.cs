using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GameProto
{
    /// <summary>
    /// 特性驱动的 protobuf 自动编解码入口：根据类型上 <see cref="ProtoFieldAttribute"/> 标注的
    /// 公开字段/属性，以反射方式完成序列化、大小计算与解析。消息类型描述按类型缓存，
    /// 首次使用后不再反射。高频消息可覆写 <see cref="ProtoMessage.WriteTo"/>、
    /// <see cref="ProtoMessage.ComputeSize"/>、<see cref="ProtoMessage.MergeFrom(ProtoReader)"/>
    /// 手工实现以获得最佳性能。
    /// </summary>
    public static partial class ProtoCodec
    {
        private static readonly Dictionary<Type, TypeDescriptor> DescriptorCache = new Dictionary<Type, TypeDescriptor>();
        private static readonly object CacheLock = new object();

        /// <summary>
        /// 将消息序列化为字节数组。支持 class 与 struct 消息。
        /// </summary>
        public static byte[] Serialize<T>(T message) where T : IProtoMessage
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            ProtoWriter writer = new ProtoWriter();
            message.WriteTo(writer);
            return writer.ToArray();
        }

        /// <summary>
        /// 从字节数组解析出消息新实例。支持 class 与 struct 消息。
        /// struct 通过约束调用（constrained）按引用把解析结果写回局部变量，
        /// 不会因装箱导致修改丢失。
        /// </summary>
        public static T Deserialize<T>(byte[] data) where T : IProtoMessage, new()
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            T message = new T();
            ProtoReader reader = new ProtoReader(data);
            message.MergeFrom(reader);
            return message;
        }

        /// <summary>
        /// struct 消息的序列化入口，由 struct 的 WriteTo 实现委托调用。
        /// </summary>
        public static void WriteStructFields<T>(T message, ProtoWriter writer) where T : struct, IProtoMessage
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            WriteMessageFields(message, writer);
        }

        /// <summary>
        /// struct 消息的大小计算入口，由 struct 的 ComputeSize 实现委托调用。
        /// </summary>
        public static int ComputeStructSize<T>(T message) where T : struct, IProtoMessage
        {
            return ComputeMessageSize(message);
        }

        private static TypeDescriptor GetDescriptor(Type type)
        {
            lock (CacheLock)
            {
                TypeDescriptor descriptor;
                if (!DescriptorCache.TryGetValue(type, out descriptor))
                {
                    descriptor = new TypeDescriptor(type);
                    DescriptorCache.Add(type, descriptor);
                }
                return descriptor;
            }
        }

        internal static void WriteMessageFields(object message, ProtoWriter writer)
        {
            TypeDescriptor descriptor = GetDescriptor(message.GetType());
            foreach (FieldDescriptor field in descriptor.Fields)
            {
                object value = field.GetValue(message);
                if (IsDefaultValue(field, value))
                {
                    continue;
                }
                WriteField(writer, field, value);
            }
        }

        internal static int ComputeMessageSize(object message)
        {
            TypeDescriptor descriptor = GetDescriptor(message.GetType());
            int size = 0;
            foreach (FieldDescriptor field in descriptor.Fields)
            {
                object value = field.GetValue(message);
                if (IsDefaultValue(field, value))
                {
                    continue;
                }
                size += ComputeFieldSize(field, value);
            }
            return size;
        }

        internal static void MergeMessageFields(object message, ProtoReader reader)
        {
            TypeDescriptor descriptor = GetDescriptor(message.GetType());
            while (true)
            {
                uint tag = reader.ReadTag();
                if (tag == 0)
                {
                    return;
                }
                int fieldNumber = ProtoWireFormat.GetFieldNumber(tag);
                ProtoWireType wireType = ProtoWireFormat.GetWireType(tag);
                FieldDescriptor field;
                if (!descriptor.ByNumber.TryGetValue(fieldNumber, out field))
                {
                    reader.SkipField(tag);
                    continue;
                }
                if (field.Kind == FieldKind.Repeated && field.ElementKind == FieldKind.Scalar &&
                    wireType == ProtoWireType.LengthDelimited)
                {
                    // packed 编码：即使写入端未声明 Packed，解析端也按规范兼容两种形式
                    ReadPackedInto(message, field, reader);
                    continue;
                }
                ReadSingleFieldInto(message, field, reader, wireType);
            }
        }

        private static void WriteField(ProtoWriter writer, FieldDescriptor field, object value)
        {
            switch (field.Kind)
            {
                case FieldKind.Scalar:
                    writer.WriteTag(field.FieldNumber, field.WireType);
                    WriteScalarValue(writer, value, field.ElementType, field.ZigZag);
                    break;
                case FieldKind.String:
                    writer.WriteTag(field.FieldNumber, ProtoWireType.LengthDelimited);
                    writer.WriteString((string)value);
                    break;
                case FieldKind.Bytes:
                    writer.WriteTag(field.FieldNumber, ProtoWireType.LengthDelimited);
                    writer.WriteBytes((byte[])value);
                    break;
                case FieldKind.Message:
                    writer.WriteTag(field.FieldNumber, ProtoWireType.LengthDelimited);
                    writer.WriteMessage((IProtoMessage)value);
                    break;
                case FieldKind.Repeated:
                    WriteRepeated(writer, field, (IList)value);
                    break;
                case FieldKind.Map:
                    WriteMap(writer, field, (IDictionary)value);
                    break;
                default:
                    throw new ProtoProtocolException("无效的字段类型");
            }
        }

        private static int ComputeFieldSize(FieldDescriptor field, object value)
        {
            switch (field.Kind)
            {
                case FieldKind.Scalar:
                    return TagSize(field.FieldNumber, field.WireType) +
                           ComputeScalarValueSize(value, field.ElementType, field.ZigZag);
                case FieldKind.String:
                    return TagSize(field.FieldNumber, ProtoWireType.LengthDelimited) +
                           ComputeStringSize((string)value);
                case FieldKind.Bytes:
                {
                    byte[] bytes = (byte[])value;
                    return TagSize(field.FieldNumber, ProtoWireType.LengthDelimited) +
                           ProtoWireFormat.GetVarintSize((ulong)bytes.Length) + bytes.Length;
                }
                case FieldKind.Message:
                {
                    int messageSize = ((IProtoMessage)value).ComputeSize();
                    return TagSize(field.FieldNumber, ProtoWireType.LengthDelimited) +
                           ProtoWireFormat.GetVarintSize((ulong)messageSize) + messageSize;
                }
                case FieldKind.Repeated:
                    return ComputeRepeatedSize(field, (IList)value);
                case FieldKind.Map:
                    return ComputeMapSize(field, (IDictionary)value);
                default:
                    throw new ProtoProtocolException("无效的字段类型");
            }
        }

        private static void WriteRepeated(ProtoWriter writer, FieldDescriptor field, IList list)
        {
            int count = list.Count;
            if (count == 0)
            {
                return;
            }
            if (field.Packed)
            {
                int size = 0;
                for (int i = 0; i < count; i++)
                {
                    size += ComputeScalarValueSize(list[i], field.ElementType, field.ZigZag);
                }
                writer.WriteTag(field.FieldNumber, ProtoWireType.LengthDelimited);
                writer.WriteLength(size);
                for (int i = 0; i < count; i++)
                {
                    WriteScalarValue(writer, list[i], field.ElementType, field.ZigZag);
                }
                return;
            }
            for (int i = 0; i < count; i++)
            {
                object item = list[i];
                if (item == null)
                {
                    continue;
                }
                switch (field.ElementKind)
                {
                    case FieldKind.Scalar:
                        writer.WriteTag(field.FieldNumber, field.WireType);
                        WriteScalarValue(writer, item, field.ElementType, field.ZigZag);
                        break;
                    case FieldKind.String:
                        writer.WriteTag(field.FieldNumber, ProtoWireType.LengthDelimited);
                        writer.WriteString((string)item);
                        break;
                    case FieldKind.Bytes:
                        writer.WriteTag(field.FieldNumber, ProtoWireType.LengthDelimited);
                        writer.WriteBytes((byte[])item);
                        break;
                    case FieldKind.Message:
                        writer.WriteTag(field.FieldNumber, ProtoWireType.LengthDelimited);
                        writer.WriteMessage((IProtoMessage)item);
                        break;
                    default:
                        throw new ProtoProtocolException("无效的重复元素类型");
                }
            }
        }

        private static int ComputeRepeatedSize(FieldDescriptor field, IList list)
        {
            int count = list.Count;
            if (count == 0)
            {
                return 0;
            }
            if (field.Packed)
            {
                int total = 0;
                for (int i = 0; i < count; i++)
                {
                    total += ComputeScalarValueSize(list[i], field.ElementType, field.ZigZag);
                }
                return TagSize(field.FieldNumber, ProtoWireType.LengthDelimited) +
                       ProtoWireFormat.GetVarintSize((ulong)total) + total;
            }
            int size = 0;
            for (int i = 0; i < count; i++)
            {
                object item = list[i];
                if (item == null)
                {
                    continue;
                }
                switch (field.ElementKind)
                {
                    case FieldKind.Scalar:
                        size += TagSize(field.FieldNumber, field.WireType) +
                                ComputeScalarValueSize(item, field.ElementType, field.ZigZag);
                        break;
                    case FieldKind.String:
                        size += TagSize(field.FieldNumber, ProtoWireType.LengthDelimited) +
                                ComputeStringSize((string)item);
                        break;
                    case FieldKind.Bytes:
                    {
                        byte[] bytes = (byte[])item;
                        size += TagSize(field.FieldNumber, ProtoWireType.LengthDelimited) +
                                ProtoWireFormat.GetVarintSize((ulong)bytes.Length) + bytes.Length;
                        break;
                    }
                    case FieldKind.Message:
                    {
                        int messageSize = ((IProtoMessage)item).ComputeSize();
                        size += TagSize(field.FieldNumber, ProtoWireType.LengthDelimited) +
                                ProtoWireFormat.GetVarintSize((ulong)messageSize) + messageSize;
                        break;
                    }
                    default:
                        throw new ProtoProtocolException("无效的重复元素类型");
                }
            }
            return size;
        }

        private static void WriteMap(ProtoWriter writer, FieldDescriptor field, IDictionary map)
        {
            foreach (DictionaryEntry entry in map)
            {
                object key = entry.Key;
                object value = entry.Value;
                if (key == null || value == null)
                {
                    continue;
                }
                int entrySize = ComputeMapKeySize(key, field.MapKeyType) +
                                ComputeMapValueSize(value, field.ElementKind, field.ElementType);
                writer.WriteTag(field.FieldNumber, ProtoWireType.LengthDelimited);
                writer.WriteLength(entrySize);
                WriteMapKey(writer, key, field.MapKeyType);
                WriteMapValue(writer, value, field.ElementKind, field.ElementType);
            }
        }

        private static int ComputeMapSize(FieldDescriptor field, IDictionary map)
        {
            int total = 0;
            foreach (DictionaryEntry entry in map)
            {
                object key = entry.Key;
                object value = entry.Value;
                if (key == null || value == null)
                {
                    continue;
                }
                int entrySize = ComputeMapKeySize(key, field.MapKeyType) +
                                ComputeMapValueSize(value, field.ElementKind, field.ElementType);
                total += TagSize(field.FieldNumber, ProtoWireType.LengthDelimited) +
                         ProtoWireFormat.GetVarintSize((ulong)entrySize) + entrySize;
            }
            return total;
        }

        private static int ComputeMapKeySize(object key, Type keyType)
        {
            if (keyType == typeof(string))
            {
                return TagSize(1, ProtoWireType.LengthDelimited) + ComputeStringSize((string)key);
            }
            return TagSize(1, ProtoWireType.Varint) + ComputeScalarValueSize(key, keyType, false);
        }

        private static int ComputeMapValueSize(object value, FieldKind kind, Type type)
        {
            switch (kind)
            {
                case FieldKind.Scalar:
                    return TagSize(2, GetScalarWireType(type)) + ComputeScalarValueSize(value, type, false);
                case FieldKind.String:
                    return TagSize(2, ProtoWireType.LengthDelimited) + ComputeStringSize((string)value);
                case FieldKind.Bytes:
                {
                    byte[] bytes = (byte[])value;
                    return TagSize(2, ProtoWireType.LengthDelimited) +
                           ProtoWireFormat.GetVarintSize((ulong)bytes.Length) + bytes.Length;
                }
                case FieldKind.Message:
                {
                    int messageSize = ((IProtoMessage)value).ComputeSize();
                    return TagSize(2, ProtoWireType.LengthDelimited) +
                           ProtoWireFormat.GetVarintSize((ulong)messageSize) + messageSize;
                }
                default:
                    throw new ProtoProtocolException("无效的 map 值类型");
            }
        }

        private static void WriteMapKey(ProtoWriter writer, object key, Type keyType)
        {
            if (keyType == typeof(string))
            {
                writer.WriteTag(1, ProtoWireType.LengthDelimited);
                writer.WriteString((string)key);
                return;
            }
            writer.WriteTag(1, ProtoWireType.Varint);
            WriteScalarValue(writer, key, keyType, false);
        }

        private static void WriteMapValue(ProtoWriter writer, object value, FieldKind kind, Type type)
        {
            switch (kind)
            {
                case FieldKind.Scalar:
                    writer.WriteTag(2, GetScalarWireType(type));
                    WriteScalarValue(writer, value, type, false);
                    break;
                case FieldKind.String:
                    writer.WriteTag(2, ProtoWireType.LengthDelimited);
                    writer.WriteString((string)value);
                    break;
                case FieldKind.Bytes:
                    writer.WriteTag(2, ProtoWireType.LengthDelimited);
                    writer.WriteBytes((byte[])value);
                    break;
                case FieldKind.Message:
                    writer.WriteTag(2, ProtoWireType.LengthDelimited);
                    writer.WriteMessage((IProtoMessage)value);
                    break;
                default:
                    throw new ProtoProtocolException("无效的 map 值类型");
            }
        }

        private static void ReadSingleFieldInto(object message, FieldDescriptor field, ProtoReader reader, ProtoWireType wireType)
        {
            switch (field.Kind)
            {
                case FieldKind.Scalar:
                    EnsureWireType(wireType, field.WireType);
                    field.SetValue(message, ReadScalarValue(reader, field.ElementType, field.ZigZag));
                    break;
                case FieldKind.String:
                    EnsureWireType(wireType, ProtoWireType.LengthDelimited);
                    field.SetValue(message, reader.ReadString());
                    break;
                case FieldKind.Bytes:
                    EnsureWireType(wireType, ProtoWireType.LengthDelimited);
                    field.SetValue(message, reader.ReadBytes());
                    break;
                case FieldKind.Message:
                {
                    EnsureWireType(wireType, ProtoWireType.LengthDelimited);
                    object existing = field.GetValue(message);
                    if (existing == null)
                    {
                        existing = CreateMessage(field.ElementType);
                        field.SetValue(message, existing);
                    }
                    IProtoMessage target = (IProtoMessage)existing;
                    reader.ReadMessageInto(target);
                    if (field.ElementType.IsValueType)
                    {
                        // struct 消息字段：GetValue 返回的是装箱副本，合并后必须回写，
                        // 否则对副本的修改全部丢失
                        field.SetValue(message, target);
                    }
                    break;
                }
                case FieldKind.Repeated:
                {
                    IList list = (IList)field.GetValue(message);
                    if (list == null)
                    {
                        list = CreateList(field.ElementType);
                        field.SetValue(message, list);
                    }
                    object item = ReadValue(reader, field.ElementKind, field.ElementType, field.WireType, wireType, field.ZigZag);
                    list.Add(item);
                    break;
                }
                case FieldKind.Map:
                {
                    IDictionary dict = (IDictionary)field.GetValue(message);
                    if (dict == null)
                    {
                        dict = CreateMap(field.MapKeyType, field.ElementType);
                        field.SetValue(message, dict);
                    }
                    ReadMapEntry(reader, dict, field);
                    break;
                }
                default:
                    throw new ProtoProtocolException("无效的字段类型");
            }
        }

        private static void ReadPackedInto(object message, FieldDescriptor field, ProtoReader reader)
        {
            IList list = (IList)field.GetValue(message);
            if (list == null)
            {
                list = CreateList(field.ElementType);
                field.SetValue(message, list);
            }
            reader.ReadLengthDelimited(r =>
            {
                while (!r.IsAtEnd)
                {
                    list.Add(ReadScalarValue(r, field.ElementType, field.ZigZag));
                }
            });
        }

        private static void ReadMapEntry(ProtoReader reader, IDictionary dict, FieldDescriptor field)
        {
            object key = GetDefaultValue(field.MapKeyType);
            object value = GetDefaultValue(field.ElementType);
            bool hasKey = false;
            bool hasValue = false;
            reader.ReadLengthDelimited(r =>
            {
                while (!r.IsAtEnd)
                {
                    uint tag = r.ReadTag();
                    int fieldNumber = ProtoWireFormat.GetFieldNumber(tag);
                    ProtoWireType wireType = ProtoWireFormat.GetWireType(tag);
                    if (fieldNumber == 1)
                    {
                        if (field.MapKeyType == typeof(string))
                        {
                            EnsureWireType(wireType, ProtoWireType.LengthDelimited);
                            key = r.ReadString();
                        }
                        else
                        {
                            EnsureWireType(wireType, ProtoWireType.Varint);
                            key = ReadScalarValue(r, field.MapKeyType, false);
                        }
                        hasKey = true;
                    }
                    else if (fieldNumber == 2)
                    {
                        value = ReadValue(r, field.ElementKind, field.ElementType, field.MapValueWireType, wireType, false);
                        hasValue = true;
                    }
                    else
                    {
                        r.SkipField(tag);
                    }
                }
            });
            if (!hasKey || !hasValue)
            {
                return;
            }
            dict[key] = value;
        }

        private static object ReadValue(ProtoReader reader, FieldKind kind, Type type,
            ProtoWireType expectedWireType, ProtoWireType actualWireType, bool zigzag)
        {
            switch (kind)
            {
                case FieldKind.Scalar:
                    EnsureWireType(actualWireType, expectedWireType);
                    return ReadScalarValue(reader, type, zigzag);
                case FieldKind.String:
                    EnsureWireType(actualWireType, ProtoWireType.LengthDelimited);
                    return reader.ReadString();
                case FieldKind.Bytes:
                    EnsureWireType(actualWireType, ProtoWireType.LengthDelimited);
                    return reader.ReadBytes();
                case FieldKind.Message:
                    EnsureWireType(actualWireType, ProtoWireType.LengthDelimited);
                    return ReadMessage(reader, type);
                default:
                    throw new ProtoProtocolException("无效的值类型");
            }
        }

        private static IProtoMessage ReadMessage(ProtoReader reader, Type type)
        {
            IProtoMessage message = CreateMessage(type);
            reader.ReadMessageInto(message);
            return message;
        }

        private static void WriteScalarValue(ProtoWriter writer, object value, Type type, bool zigzag)
        {
            if (type == typeof(int))
            {
                if (zigzag)
                {
                    writer.WriteSInt32((int)value);
                }
                else
                {
                    writer.WriteInt32((int)value);
                }
                return;
            }
            if (type == typeof(long))
            {
                if (zigzag)
                {
                    writer.WriteSInt64((long)value);
                }
                else
                {
                    writer.WriteInt64((long)value);
                }
                return;
            }
            if (type == typeof(uint))
            {
                writer.WriteUInt32((uint)value);
                return;
            }
            if (type == typeof(ulong))
            {
                writer.WriteUInt64((ulong)value);
                return;
            }
            if (type == typeof(bool))
            {
                writer.WriteBool((bool)value);
                return;
            }
            if (type == typeof(float))
            {
                writer.WriteFloat((float)value);
                return;
            }
            if (type == typeof(double))
            {
                writer.WriteDouble((double)value);
                return;
            }
            if (type.IsEnum)
            {
                writer.WriteInt64(Convert.ToInt64(value));
                return;
            }
            throw new ProtoProtocolException("不支持的标量类型：" + type);
        }

        private static object ReadScalarValue(ProtoReader reader, Type type, bool zigzag)
        {
            if (type == typeof(int))
            {
                return zigzag ? (object)reader.ReadSInt32() : reader.ReadInt32();
            }
            if (type == typeof(long))
            {
                return zigzag ? (object)reader.ReadSInt64() : reader.ReadInt64();
            }
            if (type == typeof(uint))
            {
                return reader.ReadUInt32();
            }
            if (type == typeof(ulong))
            {
                return reader.ReadUInt64();
            }
            if (type == typeof(bool))
            {
                return reader.ReadBool();
            }
            if (type == typeof(float))
            {
                return reader.ReadFloat();
            }
            if (type == typeof(double))
            {
                return reader.ReadDouble();
            }
            if (type.IsEnum)
            {
                return Enum.ToObject(type, reader.ReadInt64());
            }
            throw new ProtoProtocolException("不支持的标量类型：" + type);
        }

        private static int ComputeScalarValueSize(object value, Type type, bool zigzag)
        {
            if (type == typeof(int))
            {
                return ProtoWireFormat.GetVarintSize(zigzag
                    ? ProtoWireFormat.EncodeZigZag32((int)value)
                    : (ulong)(long)(int)value);
            }
            if (type == typeof(long))
            {
                return ProtoWireFormat.GetVarintSize(zigzag
                    ? ProtoWireFormat.EncodeZigZag64((long)value)
                    : (ulong)(long)value);
            }
            if (type == typeof(uint))
            {
                return ProtoWireFormat.GetVarintSize((uint)value);
            }
            if (type == typeof(ulong))
            {
                return ProtoWireFormat.GetVarintSize((ulong)value);
            }
            if (type == typeof(bool))
            {
                return 1;
            }
            if (type == typeof(float))
            {
                return 4;
            }
            if (type == typeof(double))
            {
                return 8;
            }
            if (type.IsEnum)
            {
                return ProtoWireFormat.GetVarintSize((ulong)Convert.ToInt64(value));
            }
            throw new ProtoProtocolException("不支持的标量类型：" + type);
        }

        private static int ComputeStringSize(string value)
        {
            int byteCount = Encoding.UTF8.GetByteCount(value);
            return ProtoWireFormat.GetVarintSize((ulong)byteCount) + byteCount;
        }

        private static int TagSize(int fieldNumber, ProtoWireType wireType)
        {
            return ProtoWireFormat.GetVarintSize(ProtoWireFormat.MakeTag(fieldNumber, wireType));
        }

        private static ProtoWireType GetScalarWireType(Type type)
        {
            if (type == typeof(float))
            {
                return ProtoWireType.Fixed32;
            }
            if (type == typeof(double))
            {
                return ProtoWireType.Fixed64;
            }
            return ProtoWireType.Varint;
        }

        private static void EnsureWireType(ProtoWireType actual, ProtoWireType expected)
        {
            if (actual != expected)
            {
                throw new ProtoProtocolException("wire type 不匹配：期望 " + expected + "，实际 " + actual);
            }
        }

        /// <summary>
        /// 判断字段是否为"默认值"。遵循 proto3 语义：标量 0/false/0f/0d、空串、空 bytes、
        /// 空列表、空 map 均不编码；struct 嵌套消息字段所有成员均为默认值时也不编码，
        /// 减少无效字节。class 嵌套消息保留"存在"语义（非 null 总是编码）。
        /// </summary>
        private static bool IsDefaultValue(FieldDescriptor field, object value)
        {
            if (value == null)
            {
                return true;
            }
            switch (field.Kind)
            {
                case FieldKind.Scalar:
                    return IsScalarDefault(value, field.ElementType);
                case FieldKind.String:
                    return ((string)value).Length == 0;
                case FieldKind.Bytes:
                    return ((byte[])value).Length == 0;
                case FieldKind.Repeated:
                    return ((IList)value).Count == 0;
                case FieldKind.Map:
                    return ((IDictionary)value).Count == 0;
                case FieldKind.Message:
                    if (field.ElementType.IsValueType)
                    {
                        return IsDefaultMessage((IProtoMessage)value, field.ElementType);
                    }
                    return false;
                default:
                    throw new ProtoProtocolException("无效的字段类型");
            }
        }

        /// <summary>
        /// 递归判断 struct 消息的所有 [ProtoField] 成员是否均为默认值。
        /// </summary>
        private static bool IsDefaultMessage(IProtoMessage message, Type type)
        {
            TypeDescriptor descriptor = GetDescriptor(type);
            foreach (FieldDescriptor field in descriptor.Fields)
            {
                object value = field.GetValue(message);
                if (!IsDefaultValue(field, value))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsScalarDefault(object value, Type type)
        {
            if (type == typeof(int))
            {
                return (int)value == 0;
            }
            if (type == typeof(long))
            {
                return (long)value == 0;
            }
            if (type == typeof(uint))
            {
                return (uint)value == 0;
            }
            if (type == typeof(ulong))
            {
                return (ulong)value == 0;
            }
            if (type == typeof(bool))
            {
                return !(bool)value;
            }
            if (type == typeof(float))
            {
                return (float)value == 0f;
            }
            if (type == typeof(double))
            {
                return (double)value == 0d;
            }
            if (type.IsEnum)
            {
                return Convert.ToInt64(value) == 0;
            }
            throw new ProtoProtocolException("不支持的标量类型：" + type);
        }

        private static IProtoMessage CreateMessage(Type type)
        {
            try
            {
                return (IProtoMessage)Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                throw new ProtoProtocolException("无法创建消息实例 " + type + "：需要公开无参构造", e);
            }
        }

        private static IList CreateList(Type elementType)
        {
            Type listType = typeof(List<>).MakeGenericType(elementType);
            return (IList)Activator.CreateInstance(listType);
        }

        private static IDictionary CreateMap(Type keyType, Type valueType)
        {
            Type mapType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            return (IDictionary)Activator.CreateInstance(mapType);
        }

        private static object GetDefaultValue(Type type)
        {
            if (type == typeof(string))
            {
                return string.Empty;
            }
            if (!type.IsValueType)
            {
                return null;
            }
            return Activator.CreateInstance(type);
        }

        /// <summary>字段在消息中的角色。</summary>
        private enum FieldKind
        {
            /// <summary>数值标量（int/long/uint/ulong/bool/float/double/enum）。</summary>
            Scalar,

            /// <summary>string。</summary>
            String,

            /// <summary>byte[]。</summary>
            Bytes,

            /// <summary>嵌套消息（IProtoMessage）。</summary>
            Message,

            /// <summary>重复字段（List&lt;T&gt;）。</summary>
            Repeated,

            /// <summary>map 字段（Dictionary&lt;K, V&gt;）。</summary>
            Map,
        }

        /// <summary>
        /// 单个 [ProtoField] 成员的静态描述；构建一次后缓存，编解码不再反射。
        /// </summary>
        private sealed class FieldDescriptor
        {
            public readonly int FieldNumber;
            public readonly FieldInfo Field;
            public readonly PropertyInfo Property;
            public readonly FieldKind Kind;
            public readonly FieldKind ElementKind;
            public readonly Type ElementType;
            public readonly Type MapKeyType;
            public readonly bool Packed;
            public readonly bool ZigZag;
            public readonly ProtoWireType WireType;
            public readonly ProtoWireType MapValueWireType;

            private FieldDescriptor(int fieldNumber, FieldInfo field, PropertyInfo property, FieldKind kind,
                FieldKind elementKind, Type elementType, Type mapKeyType, bool packed, bool zigzag,
                ProtoWireType wireType, ProtoWireType mapValueWireType)
            {
                FieldNumber = fieldNumber;
                Field = field;
                Property = property;
                Kind = kind;
                ElementKind = elementKind;
                ElementType = elementType;
                MapKeyType = mapKeyType;
                Packed = packed;
                ZigZag = zigzag;
                WireType = wireType;
                MapValueWireType = mapValueWireType;
            }

            public static FieldDescriptor Build(MemberInfo member, ProtoFieldAttribute attribute)
            {
                FieldInfo field = member as FieldInfo;
                PropertyInfo property = member as PropertyInfo;
                Type memberType;
                if (field != null)
                {
                    if (field.IsStatic)
                    {
                        throw new ProtoProtocolException("[ProtoField] 不能标注静态字段：" + field.Name);
                    }
                    memberType = field.FieldType;
                }
                else
                {
                    if (property.GetGetMethod(false) == null || property.GetSetMethod(false) == null)
                    {
                        throw new ProtoProtocolException("[ProtoField] 属性必须具有公开的 getter 与 setter：" + property.Name);
                    }
                    memberType = property.PropertyType;
                }

                if (attribute.ZigZag && memberType != typeof(int) && memberType != typeof(long))
                {
                    throw new ProtoProtocolException("[ProtoField] ZigZag 仅适用于 int/long 字段：" + member.Name);
                }

                if (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    Type[] arguments = memberType.GetGenericArguments();
                    Type keyType = arguments[0];
                    Type valueType = arguments[1];
                    ValidateMapKeyType(keyType, member.Name);
                    FieldKind valueKind;
                    ProtoWireType valueWireType;
                    ClassifyValueType(valueType, out valueKind, out valueWireType);
                    if (attribute.Packed)
                    {
                        throw new ProtoProtocolException("[ProtoField] map 字段不支持 Packed：" + member.Name);
                    }
                    return new FieldDescriptor(attribute.FieldNumber, field, property, FieldKind.Map,
                        valueKind, valueType, keyType, false, false,
                        ProtoWireType.LengthDelimited, valueWireType);
                }

                if (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type elementType = memberType.GetGenericArguments()[0];
                    FieldKind elementKind;
                    ProtoWireType elementWireType;
                    ClassifyValueType(elementType, out elementKind, out elementWireType);
                    if (attribute.Packed && elementKind != FieldKind.Scalar)
                    {
                        throw new ProtoProtocolException("[ProtoField] Packed 仅适用于数值标量重复字段：" + member.Name);
                    }
                    return new FieldDescriptor(attribute.FieldNumber, field, property, FieldKind.Repeated,
                        elementKind, elementType, null, attribute.Packed, attribute.ZigZag,
                        elementWireType, elementWireType);
                }

                FieldKind kind;
                ProtoWireType wireType;
                ClassifyValueType(memberType, out kind, out wireType);
                return new FieldDescriptor(attribute.FieldNumber, field, property, kind,
                    kind, memberType, null, false, attribute.ZigZag, wireType, wireType);
            }

            public object GetValue(object instance)
            {
                if (Field != null)
                {
                    return Field.GetValue(instance);
                }
                return Property.GetValue(instance, null);
            }

            public void SetValue(object instance, object value)
            {
                if (Field != null)
                {
                    Field.SetValue(instance, value);
                }
                else
                {
                    Property.SetValue(instance, value, null);
                }
            }

            private static void ClassifyValueType(Type type, out FieldKind kind, out ProtoWireType wireType)
            {
                if (type == typeof(int) || type == typeof(long) || type == typeof(uint) ||
                    type == typeof(ulong) || type == typeof(bool) || type.IsEnum)
                {
                    kind = FieldKind.Scalar;
                    wireType = ProtoWireType.Varint;
                }
                else if (type == typeof(float))
                {
                    kind = FieldKind.Scalar;
                    wireType = ProtoWireType.Fixed32;
                }
                else if (type == typeof(double))
                {
                    kind = FieldKind.Scalar;
                    wireType = ProtoWireType.Fixed64;
                }
                else if (type == typeof(string))
                {
                    kind = FieldKind.String;
                    wireType = ProtoWireType.LengthDelimited;
                }
                else if (type == typeof(byte[]))
                {
                    kind = FieldKind.Bytes;
                    wireType = ProtoWireType.LengthDelimited;
                }
                else if (typeof(IProtoMessage).IsAssignableFrom(type))
                {
                    kind = FieldKind.Message;
                    wireType = ProtoWireType.LengthDelimited;
                    // struct 天然具有公开无参构造（default(T)），无需检查
                    if (!type.IsValueType && type.GetConstructor(Type.EmptyTypes) == null)
                    {
                        throw new ProtoProtocolException("嵌套消息需要公开无参构造：" + type);
                    }
                }
                else
                {
                    throw new ProtoProtocolException("不支持的 protobuf 字段类型：" + type);
                }
            }

            private static void ValidateMapKeyType(Type keyType, string memberName)
            {
                if (keyType != typeof(int) && keyType != typeof(long) && keyType != typeof(uint) &&
                    keyType != typeof(ulong) && keyType != typeof(bool) && keyType != typeof(string))
                {
                    throw new ProtoProtocolException("map 键类型仅支持 int/long/uint/ulong/bool/string：" + memberName);
                }
            }
        }

        /// <summary>
        /// 一个消息类型的 [ProtoField] 成员集合，按类型缓存。
        /// </summary>
        private sealed class TypeDescriptor
        {
            public readonly List<FieldDescriptor> Fields;
            public readonly Dictionary<int, FieldDescriptor> ByNumber;

            public TypeDescriptor(Type type)
            {
                Fields = new List<FieldDescriptor>();
                ByNumber = new Dictionary<int, FieldDescriptor>();
                CollectFields(type);
                Fields.Sort((x, y) => x.FieldNumber.CompareTo(y.FieldNumber));
            }

            private void CollectFields(Type type)
            {
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
                        FieldDescriptor descriptor = FieldDescriptor.Build(member, attribute);
                        if (ByNumber.ContainsKey(descriptor.FieldNumber))
                        {
                            throw new ProtoProtocolException("重复的字段编号 " + descriptor.FieldNumber + "，类型 " + type.FullName);
                        }
                        Fields.Add(descriptor);
                        ByNumber.Add(descriptor.FieldNumber, descriptor);
                    }
                }
            }
        }
    }
}
