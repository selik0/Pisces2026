using System;

namespace GameProto
{
    /// <summary>
    /// 按固定小端序读取协议数据，并严格限制在指定缓冲区范围内。
    /// </summary>
    public struct ProtoReader
    {
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
                ConfigLog.Error("ProtoReader 创建失败：buffer 不能为 null。");
                buffer = Array.Empty<byte>();
                offset = 0;
                length = 0;
            }

            if (offset < 0 || length < 0 || offset > buffer.Length - length)
            {
                ConfigLog.Error($"ProtoReader 创建失败：读取范围无效，offset={offset}，length={length}，capacity={buffer.Length}。");
                offset = 0;
                length = 0;
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
            TryEnsureRemaining(count);
        }

        private bool TryEnsureRemaining(int count)
        {
            if (count < 0 || count > Remaining)
            {
                ConfigLog.Error($"读取越界：位置={Position}，需要={count}，剩余={Remaining}。");
                return false;
            }

            return true;
        }

        public void EnsureFullyConsumed()
        {
            if (!IsAtEnd)
            {
                ConfigLog.Error($"数据未完全读取：位置={Position}，剩余={Remaining}。");
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

            ConfigLog.Error($"非法 bool 值：{value}，位置={Position - 1}。");
            return false;
        }

        public byte ReadByte()
        {
            if (!TryEnsureRemaining(1))
            {
                return 0;
            }
            return _buffer[_offset++];
        }

        public sbyte ReadSByte() => unchecked((sbyte)ReadByte());

        public short ReadInt16() => unchecked((short)ReadUInt16());

        public ushort ReadUInt16()
        {
            if (!TryEnsureRemaining(2))
            {
                return 0;
            }
            ushort value = (ushort)(_buffer[_offset] | (_buffer[_offset + 1] << 8));
            _offset += 2;
            return value;
        }

        public int ReadInt32() => unchecked((int)ReadUInt32());

        public uint ReadUInt32()
        {
            if (!TryEnsureRemaining(4))
            {
                return 0;
            }
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
            if (!TryEnsureRemaining(8))
            {
                return 0;
            }
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
            int length = GetSafeLength(byteLength, "字符串");
            if (!TryEnsureRemaining(length))
            {
                return string.Empty;
            }
            string value;
            try
            {
                value = length == 0 ? string.Empty : ProtoEncoding.Utf8.GetString(_buffer, _offset, length);
            }
            catch (System.Text.DecoderFallbackException exception)
            {
                ConfigLog.Error($"字符串包含非法 UTF-8：位置={Position}，长度={length}。", exception);
                return string.Empty;
            }
            _offset += length;
            return value;
        }

        public byte[] ReadBytes()
        {
            uint byteLength = ReadUInt32();
            int length = GetSafeLength(byteLength, "bytes");
            if (!TryEnsureRemaining(length))
            {
                return Array.Empty<byte>();
            }
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
            return GetSafeLength(ReadUInt32(), "集合");
        }

        /// <summary>
        /// 创建限定长度的子读取器，并将当前读取位置移动到该段数据之后。
        /// </summary>
        public ProtoReader ReadSubReader(int length)
        {
            if (!TryEnsureRemaining(length))
            {
                return new ProtoReader(Array.Empty<byte>());
            }

            ProtoReader reader = new ProtoReader(_buffer, _offset, length);
            _offset += length;
            return reader;
        }

        private static int GetSafeLength(uint value, string name)
        {
            if (value > int.MaxValue)
            {
                ConfigLog.Error($"{name}长度超出 int 范围：{value}。");
                return 0;
            }

            return (int)value;
        }
    }
}
