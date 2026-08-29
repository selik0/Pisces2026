using System;

namespace GameProto
{
    /// <summary>
    /// protobuf 线格式的标签（tag）构造/解析与 zigzag、varint 长度计算等基础工具。
    /// </summary>
    public static class ProtoWireFormat
    {
        /// <summary>字段编号最大值（protobuf 规范限制）。</summary>
        public const int FieldNumberMax = (1 << 29) - 1;

        /// <summary>wire type 占用的低 3 位。</summary>
        private const int TagTypeBits = 3;

        /// <summary>
        /// 由字段编号和 wire type 构造 tag。
        /// </summary>
        public static ulong MakeTag(int fieldNumber, ProtoWireType wireType)
        {
            if (fieldNumber <= 0 || fieldNumber > FieldNumberMax)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldNumber), "字段编号必须在 1 到 2^29-1 之间");
            }
            return ((ulong)fieldNumber << TagTypeBits) | (ulong)wireType;
        }

        /// <summary>
        /// 从 tag 中取出字段编号。
        /// </summary>
        public static int GetFieldNumber(uint tag)
        {
            return (int)(tag >> TagTypeBits);
        }

        /// <summary>
        /// 从 tag 中取出 wire type。
        /// </summary>
        public static ProtoWireType GetWireType(uint tag)
        {
            return (ProtoWireType)(tag & 0x7);
        }

        /// <summary>
        /// 将 int32 编码为 zigzag 无符号形式（对应 sint32）。
        /// </summary>
        public static uint EncodeZigZag32(int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }

        /// <summary>
        /// 将 zigzag 无符号形式解码为 int32。
        /// </summary>
        public static int DecodeZigZag32(uint value)
        {
            return (int)(value >> 1) ^ -(int)(value & 1);
        }

        /// <summary>
        /// 将 int64 编码为 zigzag 无符号形式（对应 sint64）。
        /// </summary>
        public static ulong EncodeZigZag64(long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
        }

        /// <summary>
        /// 将 zigzag 无符号形式解码为 int64。
        /// </summary>
        public static long DecodeZigZag64(ulong value)
        {
            return (long)(value >> 1) ^ -(long)(value & 1);
        }

        /// <summary>
        /// 计算 varint 编码所需的字节数。
        /// </summary>
        public static int GetVarintSize(ulong value)
        {
            int size = 1;
            while ((value & ~0x7FUL) != 0)
            {
                value >>= 7;
                size++;
            }
            return size;
        }
    }
}
