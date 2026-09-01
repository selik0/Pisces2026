namespace GameProto
{
    /// <summary>
    /// 所有网络协议消息的运行时基类。
    /// </summary>
    public abstract class ProtoMessage
    {
        public abstract uint MessageId { get; }
        public abstract int GetEncodedSize();
        public abstract void Encode(ref ProtoWriter writer);
        public abstract void Decode(ref ProtoReader reader);
    }
}
