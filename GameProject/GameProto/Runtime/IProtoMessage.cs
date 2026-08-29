namespace GameProto
{
    /// <summary>
    /// protobuf 消息的统一接口。
    /// 嵌套消息、重复消息元素和 map 值消息都通过该接口完成长度前缀的写入与读取。
    /// class 消息继承 <see cref="ProtoMessage"/> 基类获得默认实现；
    /// struct 消息直接实现本接口，三个方法委托给 <see cref="ProtoCodec"/> 的
    /// WriteStructFields/ComputeStructSize/MergeStructFields 即可。
    /// </summary>
    public interface IProtoMessage
    {
        /// <summary>
        /// 将消息内容写入 writer（不含该消息自身的字段标签与长度前缀，由上层负责）。
        /// </summary>
        void WriteTo(ProtoWriter writer);

        /// <summary>
        /// 计算消息内容序列化后的字节数（不含该消息自身的字段标签与长度前缀）。
        /// </summary>
        int ComputeSize();

        /// <summary>
        /// 从 reader 当前位置开始解析并合并进当前实例。
        /// 重复调用具有合并语义：单数消息字段在已有值上合并，重复字段追加。
        /// </summary>
        void MergeFrom(ProtoReader reader);
    }
}
