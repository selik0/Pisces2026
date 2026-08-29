namespace GameProto
{
    /// <summary>
    /// protobuf 二进制线格式中的字段 wire type。
    /// </summary>
    public enum ProtoWireType
    {
        /// <summary>变长整数（int32/int64/uint32/uint64/bool/enum/sint32/sint64）。</summary>
        Varint = 0,

        /// <summary>固定 8 字节（double/fixed64/sfixed64）。</summary>
        Fixed64 = 1,

        /// <summary>长度前缀（string/bytes/嵌套消息/packed 重复字段）。</summary>
        LengthDelimited = 2,

        /// <summary>StartGroup，仅用于兼容跳过，不参与编解码。</summary>
        StartGroup = 3,

        /// <summary>EndGroup，仅用于兼容跳过，不参与编解码。</summary>
        EndGroup = 4,

        /// <summary>固定 4 字节（float/fixed32/sfixed32）。</summary>
        Fixed32 = 5,
    }
}
