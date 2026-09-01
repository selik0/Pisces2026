using System;
using System.Text;

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
                throw new ArgumentNullException(nameof(buffer));
            }

            _buffer = buffer;
            _offset = 0;
        }

        public int Position => _offset;
        public int Capacity => _buffer.Length;
        public int Remaining => _buffer.Length - _offset;

        public void WriteBoolean(bool value) => WriteByte(value ? (byte)1 : (byte)0);
        public void WriteByte(byte value) { EnsureCapacity(1); _buffer[_offset++] = value; }
        public void WriteSByte(sbyte value) => WriteByte(unchecked((byte)value));
        public void WriteInt16(short value) => WriteUInt16(unchecked((ushort)value));
        public void WriteUInt16(ushort value)
        {
            EnsureCapacity(2);
            _buffer[_offset++] = (byte)value;
            _buffer[_offset++] = (byte)(value >> 8);
        }
        public void WriteInt32(int value) => WriteUInt32(unchecked((uint)value));
        public void WriteUInt32(uint value)
        {
            EnsureCapacity(4);
            _buffer[_offset++] = (byte)value;
            _buffer[_offset++] = (byte)(value >> 8);
            _buffer[_offset++] = (byte)(value >> 16);
            _buffer[_offset++] = (byte)(value >> 24);
        }
        public void WriteInt64(long value) => WriteUInt64(unchecked((ulong)value));
        public void WriteUInt64(ulong value)
        {
            EnsureCapacity(8);
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
            int byteCount = Encoding.UTF8.GetByteCount(value);
            EnsureVariableLength(byteCount, ProtoRuntimeLimits.DefaultMaxStringBytes, "字符串");
            WriteUInt32((uint)byteCount);
            EnsureCapacity(byteCount);
            if (byteCount > 0)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                Buffer.BlockCopy(bytes, 0, _buffer, _offset, byteCount);
            }
            _offset += byteCount;
        }

        public void WriteBytes(byte[] value)
        {
            int length = value == null ? 0 : value.Length;
            EnsureVariableLength(length, ProtoRuntimeLimits.DefaultMaxBytes, "bytes");
            WriteUInt32((uint)length);
            EnsureCapacity(length);
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

        private void EnsureCapacity(int count)
        {
            if (count < 0 || count > Remaining)
            {
                throw new ProtoSerializationException($"写入空间不足：位置={Position}，需要={count}，剩余={Remaining}。");
            }
        }

        private static void EnsureVariableLength(int length, int maximum, string name)
        {
            if (length < 0 || length > maximum)
            {
                throw new ProtoSerializationException($"{name}长度超出限制：{length}，最大={maximum}。");
            }
        }
    }
}
