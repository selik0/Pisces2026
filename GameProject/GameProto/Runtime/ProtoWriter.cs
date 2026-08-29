using System;
using System.Text;

namespace GameProto
{
    /// <summary>
    /// protobuf 二进制写入器：按线格式规则把字段值写入可增长的字节缓冲区。
    /// 只负责写"值"和"标签"，是否写某个字段由调用方（消息或 ProtoCodec）决定。
    /// </summary>
    public sealed class ProtoWriter
    {
        private byte[] _buffer;
        private int _position;

        public ProtoWriter() : this(256)
        {
        }

        /// <param name="capacity">初始缓冲区大小，按需自动增长。</param>
        public ProtoWriter(int capacity)
        {
            if (capacity < 16)
            {
                capacity = 16;
            }
            _buffer = new byte[capacity];
        }

        /// <summary>当前写入位置（已写入的字节数）。</summary>
        public int Position
        {
            get { return _position; }
        }

        /// <summary>
        /// 清空已写入内容，复用缓冲区。
        /// </summary>
        public void Clear()
        {
            _position = 0;
        }

        /// <summary>
        /// 输出已写入的字节数组。
        /// </summary>
        public byte[] ToArray()
        {
            byte[] result = new byte[_position];
            Buffer.BlockCopy(_buffer, 0, result, 0, _position);
            return result;
        }

        /// <summary>写入一个原始字节。</summary>
        public void WriteRawByte(byte value)
        {
            EnsureCapacity(1);
            _buffer[_position++] = value;
        }

        /// <summary>写入一段原始字节。</summary>
        public void WriteRawBytes(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }
            WriteRawBytes(bytes, 0, bytes.Length);
        }

        /// <summary>写入一段原始字节。</summary>
        public void WriteRawBytes(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }
            if (offset < 0 || count < 0 || (long)offset + count > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "offset/count 超出字节数组范围");
            }
            EnsureCapacity(count);
            Buffer.BlockCopy(bytes, offset, _buffer, _position, count);
            _position += count;
        }

        /// <summary>写入一个无符号 varint。</summary>
        public void WriteRawVarint(ulong value)
        {
            while ((value & ~0x7FUL) != 0)
            {
                WriteRawByte((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }
            WriteRawByte((byte)value);
        }

        /// <summary>写入字段标签（tag）。</summary>
        public void WriteTag(int fieldNumber, ProtoWireType wireType)
        {
            WriteRawVarint(ProtoWireFormat.MakeTag(fieldNumber, wireType));
        }

        /// <summary>写入长度前缀。</summary>
        public void WriteLength(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            WriteRawVarint((ulong)length);
        }

        /// <summary>写入 int32（负数按 64 位补码 varint 编码）。</summary>
        public void WriteInt32(int value)
        {
            WriteRawVarint((ulong)(long)value);
        }

        /// <summary>写入 uint32。</summary>
        public void WriteUInt32(uint value)
        {
            WriteRawVarint(value);
        }

        /// <summary>写入 int64。</summary>
        public void WriteInt64(long value)
        {
            WriteRawVarint((ulong)value);
        }

        /// <summary>写入 uint64。</summary>
        public void WriteUInt64(ulong value)
        {
            WriteRawVarint(value);
        }

        /// <summary>写入 bool。</summary>
        public void WriteBool(bool value)
        {
            WriteRawByte(value ? (byte)1 : (byte)0);
        }

        /// <summary>写入 sint32（zigzag 编码）。</summary>
        public void WriteSInt32(int value)
        {
            WriteRawVarint(ProtoWireFormat.EncodeZigZag32(value));
        }

        /// <summary>写入 sint64（zigzag 编码）。</summary>
        public void WriteSInt64(long value)
        {
            WriteRawVarint(ProtoWireFormat.EncodeZigZag64(value));
        }

        /// <summary>写入 float（fixed32，小端）。</summary>
        public void WriteFloat(float value)
        {
            EnsureCapacity(4);
            WriteFixed32LittleEndian(BitConverter.GetBytes(value));
        }

        /// <summary>写入 double（fixed64，小端）。</summary>
        public void WriteDouble(double value)
        {
            EnsureCapacity(8);
            WriteFixed64LittleEndian(BitConverter.GetBytes(value));
        }

        /// <summary>写入 string（长度前缀 + UTF-8 字节）。</summary>
        public void WriteString(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            int byteCount = Encoding.UTF8.GetByteCount(value);
            WriteLength(byteCount);
            EnsureCapacity(byteCount);
            Encoding.UTF8.GetBytes(value, 0, value.Length, _buffer, _position);
            _position += byteCount;
        }

        /// <summary>写入 bytes（长度前缀 + 字节内容）。</summary>
        public void WriteBytes(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            WriteLength(value.Length);
            WriteRawBytes(value);
        }

        /// <summary>写入嵌套消息（长度前缀 + 消息内容），长度由 ComputeSize 计算。</summary>
        public void WriteMessage(IProtoMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            int size = message.ComputeSize();
            WriteLength(size);
            message.WriteTo(this);
        }

        private void WriteFixed32LittleEndian(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                _buffer[_position++] = bytes[0];
                _buffer[_position++] = bytes[1];
                _buffer[_position++] = bytes[2];
                _buffer[_position++] = bytes[3];
            }
            else
            {
                _buffer[_position++] = bytes[3];
                _buffer[_position++] = bytes[2];
                _buffer[_position++] = bytes[1];
                _buffer[_position++] = bytes[0];
            }
        }

        private void WriteFixed64LittleEndian(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < 8; i++)
                {
                    _buffer[_position++] = bytes[i];
                }
            }
            else
            {
                for (int i = 7; i >= 0; i--)
                {
                    _buffer[_position++] = bytes[i];
                }
            }
        }

        private void EnsureCapacity(int additional)
        {
            if (additional < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(additional));
            }
            int required = _position + additional;
            if (required <= _buffer.Length)
            {
                return;
            }
            int newSize = _buffer.Length * 2;
            if (newSize < required)
            {
                newSize = required;
            }
            Array.Resize(ref _buffer, newSize);
        }
    }
}
