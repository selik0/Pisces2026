namespace GameProto
{
    /// <summary>
    /// 网络消息固定16字节包头，所有字段使用小端序。
    /// </summary>
    public struct ProtoPacketHeader
    {
        public const int HeaderSize = 16;
        public uint PayloadLength;
        public uint MessageId;
        public uint ProtocolVersion;
        public uint Sequence;

        public void Encode(ref ProtoWriter writer)
        {
            writer.WriteUInt32(PayloadLength);
            writer.WriteUInt32(MessageId);
            writer.WriteUInt32(ProtocolVersion);
            writer.WriteUInt32(Sequence);
        }

        public static ProtoPacketHeader Decode(ref ProtoReader reader)
        {
            return new ProtoPacketHeader
            {
                PayloadLength = reader.ReadUInt32(),
                MessageId = reader.ReadUInt32(),
                ProtocolVersion = reader.ReadUInt32(),
                Sequence = reader.ReadUInt32()
            };
        }
    }
}
