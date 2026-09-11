using System;

namespace GameProto
{
    /// <summary>
    /// 按固定小端序写入协议数据。
    /// </summary>
    public struct ProtoWriter
    {
        private byte[] _buffer;
        private int _offset;

        public ProtoWriter(byte[] buffer)
        {
            if (buffer == null)
            {
                ConfigLog.Error("ProtoWriter 创建失败：buffer 不能为 null。");
                buffer = Array.Empty<byte>();
            }

            _buffer = buffer;
            _offset = 0;
        }

        public int Position => _offset;
        public int Capacity => _buffer.Length;
        public int Remaining => _buffer.Length - _offset;

        public void WriteBoolean(bool value) => WriteByte(value ? (byte)1 : (byte)0);
        public void WriteByte(byte value)
        {
            if (EnsureCapacity(1))
            {
                _buffer[_offset++] = value;
            }
        }
        public void WriteSByte(sbyte value) => WriteByte(unchecked((byte)value));
        public void WriteInt16(short value) => WriteUInt16(unchecked((ushort)value));
        public void WriteUInt16(ushort value)
        {
            if (!EnsureCapacity(2))
            {
                return;
            }
            _buffer[_offset++] = (byte)value;
            _buffer[_offset++] = (byte)(value >> 8);
        }
        public void WriteInt32(int value) => WriteUInt32(unchecked((uint)value));
        public void WriteUInt32(uint value)
        {
            if (!EnsureCapacity(4))
            {
                return;
            }
            _buffer[_offset++] = (byte)value;
            _buffer[_offset++] = (byte)(value >> 8);
            _buffer[_offset++] = (byte)(value >> 16);
            _buffer[_offset++] = (byte)(value >> 24);
        }
        public void WriteInt64(long value) => WriteUInt64(unchecked((ulong)value));
        public void WriteUInt64(ulong value)
        {
            if (!EnsureCapacity(8))
            {
                return;
            }
            for (int i = 0; i < 8; i++)
            {
                _buffer[_offset++] = (byte)(value >> (i * 8));
            }
        }
        public void WriteSingle(float value) => WriteUInt32(ProtoBitConverter.SingleToUInt32(value));
        public void WriteDouble(double value) => WriteUInt64(ProtoBitConverter.DoubleToUInt64(value));

        public void WriteString(string value)
        {
            value = value ?? string.Empty;
            int byteCount;
            try
            {
                byteCount = ProtoEncoding.Utf8.GetByteCount(value);
            }
            catch (System.Text.EncoderFallbackException exception)
            {
                ConfigLog.Error("字符串包含非法 UTF-16。", exception);
                return;
            }

            if (!EnsureCapacity(4 + byteCount))
            {
                return;
            }

            WriteUInt32((uint)byteCount);
            if (byteCount > 0)
            {
                try
                {
                    ProtoEncoding.Utf8.GetBytes(value, 0, value.Length, _buffer, _offset);
                }
                catch (System.Text.EncoderFallbackException exception)
                {
                    ConfigLog.Error("字符串包含非法 UTF-16。", exception);
                    return;
                }
            }
            _offset += byteCount;
        }

        public void WriteBytes(byte[] value)
        {
            int length = value == null ? 0 : value.Length;
            if (!EnsureCapacity(4 + length))
            {
                return;
            }

            WriteUInt32((uint)length);
            if (length > 0)
            {
                Buffer.BlockCopy(value, 0, _buffer, _offset, length);
            }
            _offset += length;
        }

        public byte[] ToArray()
        {
            byte[] result = new byte[_offset];
            Buffer.BlockCopy(_buffer, 0, result, 0, _offset);
            return result;
        }

        private bool EnsureCapacity(int count)
        {
            if (count < 0 || count > Remaining)
            {
                ConfigLog.Error($"写入空间不足：位置={Position}，需要={count}，剩余={Remaining}。");
                return false;
            }

            return true;
        }
    }
}
