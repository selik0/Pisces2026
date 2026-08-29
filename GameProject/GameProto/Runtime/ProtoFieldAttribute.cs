using System;

namespace GameProto
{
    /// <summary>
    /// 标注 protobuf 消息字段（公开字段或属性）。
    /// 配合 <see cref="ProtoMessage"/> 基类使用：所有带此特性的成员会被
    /// <see cref="ProtoCodec"/> 反射识别并自动参与序列化与反序列化。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ProtoFieldAttribute : Attribute
    {
        public ProtoFieldAttribute(int fieldNumber)
        {
            if (fieldNumber <= 0 || fieldNumber > ProtoWireFormat.FieldNumberMax)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldNumber), "字段编号必须在 1 到 2^29-1 之间");
            }
            FieldNumber = fieldNumber;
        }

        /// <summary>字段编号（1 到 2^29-1，同一消息内不可重复）。</summary>
        public int FieldNumber { get; }

        /// <summary>
        /// 重复数值字段（List&lt;int&gt; 等）是否使用 packed 编码。
        /// 仅对数值标量（int/long/uint/ulong/bool/float/double/enum）有效。
        /// </summary>
        public bool Packed { get; set; }

        /// <summary>
        /// int/long 字段是否使用 zigzag 编码（对应 protobuf 的 sint32/sint64）。
        /// </summary>
        public bool ZigZag { get; set; }
    }
}
