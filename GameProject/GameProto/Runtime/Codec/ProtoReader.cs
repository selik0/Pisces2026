using System;
using System.Text;

namespace GameProto
{
    /// <summary>
    /// 按固定小端序读取协议数据，并严格限制在指定缓冲区范围内。
    /// </summary>
    public struct ProtoReader
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private readonly byte[] _buffer;
        private readonly int _start;
        private readonly int _end;
        private int _offset;

        public ProtoReader(byte[] buffer)
            : this(buffer, 0, buffer == null ? 0 : buffer.Length)
        {
        }

        public ProtoReader(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || length < 0 || offset > buffer.Length - length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "读取范围无效。");
            }

            _buffer = buffer;
            _start = offset;
            _offset = offset;
            _end = offset + length;
        }

        public int Position => _offset - _start;
        public int Remaining => _end - _offset;
        public bool IsAtEnd => _offset == _end;

        public void EnsureRemaining(int count)
        {
            if (count < 0 || count > Remaining)
            {
                throw new ProtoSerializationException(
                    $"读取越界：位置={Position}，需要={count}，剩余={Remaining}。");
            }
        }

        public void EnsureFullyConsumed()
        {
            if (!IsAtEnd)
            {
                throw new ProtoSerializationException(
                    $"数据未完全读取：位置={Position}，剩余={Remaining}。");
            }
        }

        public bool ReadBoolean()
        {
            byte value = ReadByte();
            if (value == 0)
            {
                return false;
            }

            if (value == 1)
            {
                return true;
            }

            throw new ProtoSerializationException($"非法 bool 值：{value}，位置={Position - 1}。");
        }

        public byte ReadByte()
        {
            EnsureRemaining(1);
            return _buffer[_offset++];
        }

        public sbyte ReadSByte() => unchecked((sbyte)ReadByte());

        public short ReadInt16() => unchecked((short)ReadUInt16());

        public ushort ReadUInt16()
        {
            EnsureRemaining(2);
            ushort value = (ushort)(_buffer[_offset] | (_buffer[_offset + 1] << 8));
            _offset += 2;
            return value;
        }

        public int ReadInt32() => unchecked((int)ReadUInt32());

        public uint ReadUInt32()
        {
            EnsureRemaining(4);
            uint value = (uint)(_buffer[_offset]
                | (_buffer[_offset + 1] << 8)
                | (_buffer[_offset + 2] << 16)
                | (_buffer[_offset + 3] << 24));
            _offset += 4;
            return value;
        }

        public long ReadInt64() => unchecked((long)ReadUInt64());

        public ulong ReadUInt64()
        {
            EnsureRemaining(8);
            ulong value = (ulong)_buffer[_offset]
                | ((ulong)_buffer[_offset + 1] << 8)
                | ((ulong)_buffer[_offset + 2] << 16)
                | ((ulong)_buffer[_offset + 3] << 24)
                | ((ulong)_buffer[_offset + 4] << 32)
                | ((ulong)_buffer[_offset + 5] << 40)
                | ((ulong)_buffer[_offset + 6] << 48)
                | ((ulong)_buffer[_offset + 7] << 56);
            _offset += 8;
            return value;
        }

        public float ReadSingle()
        {
            return ProtoBitConverter.UInt32ToSingle(ReadUInt32());
        }

        public double ReadDouble()
        {
            return ProtoBitConverter.UInt64ToDouble(ReadUInt64());
        }

        public string ReadString()
        {
            uint byteLength = ReadUInt32();
            int length = GetSafeLength(byteLength, ProtoRuntimeLimits.DefaultMaxStringBytes, "字符串");
            EnsureRemaining(length);
            string value;
            try
            {
                value = length == 0 ? string.Empty : StrictUtf8.GetString(_buffer, _offset, length);
            }
            catch (DecoderFallbackException exception)
            {
                throw new ProtoSerializationException($"字符串包含非法 UTF-8：位置={Position}，长度={length}。", exception);
            }
            _offset += length;
            return value;
        }

        public byte[] ReadBytes()
        {
            uint byteLength = ReadUInt32();
            int length = GetSafeLength(byteLength, ProtoRuntimeLimits.DefaultMaxBytes, "bytes");
            EnsureRemaining(length);
            byte[] value = new byte[length];
            if (length > 0)
            {
                Buffer.BlockCopy(_buffer, _offset, value, 0, length);
            }

            _offset += length;
            return value;
        }

        public int ReadCollectionCount()
        {
            return GetSafeLength(ReadUInt32(), ProtoRuntimeLimits.DefaultMaxCollectionCount, "集合");
        }

        private static int GetSafeLength(uint value, int maximum, string name)
        {
            if (value > int.MaxValue || value > maximum)
            {
                throw new ProtoSerializationException($"{name}长度超出限制：{value}，最大={maximum}。");
            }

            return (int)value;
        }
    }
}
