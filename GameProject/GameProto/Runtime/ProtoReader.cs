using System;
using System.Text;

namespace GameProto
{
    /// <summary>
    /// protobuf 二进制读取器：从字节数组按线格式规则读取字段值。
    /// 支持长度受限区域（嵌套消息、packed 字段、map 条目），
    /// 遇到未知字段时按 wire type 跳过，保证前向兼容。
    /// </summary>
    public sealed class ProtoReader
    {
        private readonly byte[] _buffer;
        private int _position;
        private int _limit;

        public ProtoReader(byte[] buffer) : this(buffer, 0, buffer != null ? buffer.Length : 0)
        {
        }

        /// <param name="buffer">待解析的字节数组。</param>
        /// <param name="offset">起始偏移。</param>
        /// <param name="length">有效长度。</param>
        public ProtoReader(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0 || length < 0 || (long)offset + length > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "offset/length 超出字节数组范围");
            }
            _buffer = buffer;
            _position = offset;
            _limit = offset + length;
        }

        /// <summary>当前读取位置。</summary>
        public int Position
        {
            get { return _position; }
        }

        /// <summary>是否已到达当前有效区域末尾。</summary>
        public bool IsAtEnd
        {
            get { return _position >= _limit; }
        }

        /// <summary>
        /// 读取下一个字段标签；到达区域末尾返回 0，遇到非法标签（0）抛异常。
        /// </summary>
        public uint ReadTag()
        {
            if (IsAtEnd)
            {
                return 0;
            }
            uint tag = (uint)ReadRawVarint();
            if (tag == 0)
            {
                throw new ProtoProtocolException("非法标签：字段编号不能为 0");
            }
            return tag;
        }

        /// <summary>读取一个无符号 varint（最多 10 字节，带溢出检查）。</summary>
        public ulong ReadRawVarint()
        {
            ulong result = 0;
            int shift = 0;
            while (true)
            {
                byte b = ReadRawByte();
                if (shift == 63 && (b & 0x7E) != 0)
                {
                    throw new ProtoProtocolException("varint 溢出：超过 64 位");
                }
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                {
                    return result;
                }
                shift += 7;
                if (shift >= 64)
                {
                    throw new ProtoProtocolException("varint 格式错误：超过 10 字节");
                }
            }
        }

        /// <summary>读取一个原始字节。</summary>
        public byte ReadRawByte()
        {
            if (_position >= _limit)
            {
                throw new ProtoProtocolException("读取越界：已到达数据末尾");
            }
            return _buffer[_position++];
        }

        /// <summary>读取 count 个原始字节。</summary>
        public byte[] ReadRawBytes(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if ((long)_position + count > _limit)
            {
                throw new ProtoProtocolException("读取越界：长度超出有效区域");
            }
            byte[] result = new byte[count];
            Buffer.BlockCopy(_buffer, _position, result, 0, count);
            _position += count;
            return result;
        }

        /// <summary>跳过 count 个原始字节。</summary>
        public void SkipRawBytes(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if ((long)_position + count > _limit)
            {
                throw new ProtoProtocolException("读取越界：长度超出有效区域");
            }
            _position += count;
        }

        /// <summary>
        /// 读取长度前缀并校验其不越过当前有效区域。
        /// </summary>
        public int ReadLength()
        {
            long length = (long)ReadRawVarint();
            if (length < 0 || length > int.MaxValue || (long)_position + length > _limit)
            {
                throw new ProtoProtocolException("长度前缀非法或超出有效区域");
            }
            return (int)length;
        }

        /// <summary>
        /// 读取一个长度受限区域，在区域内执行 body 后把位置推进到区域末尾。
        /// 用于嵌套消息、packed 重复字段和 map 条目的解析。
        /// </summary>
        public void ReadLengthDelimited(Action<ProtoReader> body)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }
            int length = ReadLength();
            int end = _position + length;
            int savedLimit = _limit;
            _limit = end;
            try
            {
                body(this);
            }
            finally
            {
                _position = _limit;
                _limit = savedLimit;
            }
        }

        /// <summary>
        /// 读取一个嵌套消息并返回新实例。
        /// class 与 struct 消息均支持：class 直接引用合并；struct 装箱合并后解箱回写，
        /// 避免对装箱副本的修改丢失。
        /// </summary>
        public T ReadMessage<T>() where T : IProtoMessage, new()
        {
            T message = new T();
            object box = message;
            ReadMessageInto((IProtoMessage)box);
            message = (T)box;
            return message;
        }

        /// <summary>
        /// 读取一个嵌套消息并合并进现有实例（遵循 protobuf 的合并语义）。
        /// </summary>
        public void ReadMessageInto(IProtoMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            ReadLengthDelimited(reader => message.MergeFrom(reader));
        }

        /// <summary>
        /// 跳过未知字段（支持 group 的递归跳过）。
        /// </summary>
        public void SkipField(uint tag)
        {
            switch (ProtoWireFormat.GetWireType(tag))
            {
                case ProtoWireType.Varint:
                    ReadRawVarint();
                    break;
                case ProtoWireType.Fixed64:
                    SkipRawBytes(8);
                    break;
                case ProtoWireType.LengthDelimited:
                    SkipRawBytes(ReadLength());
                    break;
                case ProtoWireType.Fixed32:
                    SkipRawBytes(4);
                    break;
                case ProtoWireType.StartGroup:
                    while (true)
                    {
                        uint innerTag = ReadTag();
                        if (innerTag == 0)
                        {
                            throw new ProtoProtocolException("group 未闭合");
                        }
                        if (ProtoWireFormat.GetWireType(innerTag) == ProtoWireType.EndGroup)
                        {
                            break;
                        }
                        SkipField(innerTag);
                    }
                    break;
                case ProtoWireType.EndGroup:
                    throw new ProtoProtocolException("意外的 EndGroup 标签");
                default:
                    throw new ProtoProtocolException("未知 wire type：" + ProtoWireFormat.GetWireType(tag));
            }
        }

        /// <summary>读取 int32（按 varint 读取并截断）。</summary>
        public int ReadInt32()
        {
            return (int)ReadRawVarint();
        }

        /// <summary>读取 uint32。</summary>
        public uint ReadUInt32()
        {
            return (uint)ReadRawVarint();
        }

        /// <summary>读取 int64。</summary>
        public long ReadInt64()
        {
            return (long)ReadRawVarint();
        }

        /// <summary>读取 uint64。</summary>
        public ulong ReadUInt64()
        {
            return ReadRawVarint();
        }

        /// <summary>读取 bool（非零即 true）。</summary>
        public bool ReadBool()
        {
            return ReadRawVarint() != 0;
        }

        /// <summary>读取 sint32（zigzag 解码）。</summary>
        public int ReadSInt32()
        {
            return ProtoWireFormat.DecodeZigZag32((uint)ReadRawVarint());
        }

        /// <summary>读取 sint64（zigzag 解码）。</summary>
        public long ReadSInt64()
        {
            return ProtoWireFormat.DecodeZigZag64(ReadRawVarint());
        }

        /// <summary>读取 float（fixed32，小端）。</summary>
        public float ReadFloat()
        {
            byte[] bytes = ReadRawBytes(4);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>读取 double（fixed64，小端）。</summary>
        public double ReadDouble()
        {
            byte[] bytes = ReadRawBytes(8);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToDouble(bytes, 0);
        }

        /// <summary>读取 string（长度前缀 + UTF-8）。</summary>
        public string ReadString()
        {
            int length = ReadLength();
            string result = Encoding.UTF8.GetString(_buffer, _position, length);
            _position += length;
            return result;
        }

        /// <summary>读取 bytes（长度前缀 + 内容）。</summary>
        public byte[] ReadBytes()
        {
            return ReadRawBytes(ReadLength());
        }
    }
}
