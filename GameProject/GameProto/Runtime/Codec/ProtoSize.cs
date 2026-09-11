using System;

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
            try
            {
                int byteCount = ProtoEncoding.Utf8.GetByteCount(value ?? string.Empty);
                return checked(4 + byteCount);
            }
            catch (System.Text.EncoderFallbackException exception)
            {
                ConfigLog.Error("字符串包含非法 UTF-16。", exception);
                return 0;
            }
            catch (OverflowException exception)
            {
                ConfigLog.Error("字符串编码尺寸溢出。", exception);
                return 0;
            }
        }

        public static int Bytes(byte[] value)
        {
            int length = value == null ? 0 : value.Length;
            return checked(4 + length);
        }

        public static int Array<T>(T[] value, Func<T, int> elementSize)
        {
            if (elementSize == null)
            {
                ConfigLog.Error("数组尺寸计算失败：elementSize 不能为 null。");
                return 0;
            }

            int count = value == null ? 0 : value.Length;
            int size = 4;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    int currentElementSize = elementSize(value[i]);
                    if (currentElementSize < 0)
                    {
                        ConfigLog.Error($"数组元素编码尺寸不能为负数：索引={i}，尺寸={currentElementSize}。");
                        return 0;
                    }

                    size = checked(size + currentElementSize);
                }
            }
            catch (OverflowException exception)
            {
                ConfigLog.Error("数组编码尺寸溢出。", exception);
                return 0;
            }

            return size;
        }
    }
}
