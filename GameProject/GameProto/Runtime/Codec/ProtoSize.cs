using System;
using System.Text;

namespace GameProto
{
    /// <summary>
    /// 计算协议字段编码后的字节数。
    /// </summary>
    public static class ProtoSize
    {
        public static int Boolean(bool value) => 1;
        public static int Byte(byte value) => 1;
        public static int SByte(sbyte value) => 1;
        public static int Int16(short value) => 2;
        public static int UInt16(ushort value) => 2;
        public static int Int32(int value) => 4;
        public static int UInt32(uint value) => 4;
        public static int Int64(long value) => 8;
        public static int UInt64(ulong value) => 8;
        public static int Single(float value) => 4;
        public static int Double(double value) => 8;

        public static int String(string value)
        {
            int byteCount = Encoding.UTF8.GetByteCount(value ?? string.Empty);
            EnsureLength(byteCount, ProtoRuntimeLimits.DefaultMaxStringBytes, "字符串");
            return checked(4 + byteCount);
        }

        public static int Bytes(byte[] value)
        {
            int length = value == null ? 0 : value.Length;
            EnsureLength(length, ProtoRuntimeLimits.DefaultMaxBytes, "bytes");
            return checked(4 + length);
        }

        public static int Array<T>(T[] value, Func<T, int> elementSize)
        {
            if (elementSize == null)
            {
                throw new ArgumentNullException(nameof(elementSize));
            }

            int count = value == null ? 0 : value.Length;
            EnsureLength(count, ProtoRuntimeLimits.DefaultMaxCollectionCount, "集合数量");
            int size = 4;
            for (int i = 0; i < count; i++)
            {
                size = checked(size + elementSize(value[i]));
            }

            return size;
        }

        private static void EnsureLength(int length, int maximum, string name)
        {
            if (length < 0 || length > maximum)
            {
                throw new ProtoSerializationException($"{name}长度超出限制：{length}，最大={maximum}。");
            }
        }
    }
}
