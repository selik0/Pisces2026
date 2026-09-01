namespace GameProto
{
    /// <summary>
    /// 网络消息固定16字节包头，所有字段使用小端序。
    /// </summary>
    public struct ProtoPacketHeader
    {
        public const int HeaderSize = 16;
        public const uint CurrentProtocolVersion = 1;
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
            ProtoPacketHeader header = new ProtoPacketHeader
            {
                PayloadLength = reader.ReadUInt32(),
                MessageId = reader.ReadUInt32(),
                ProtocolVersion = reader.ReadUInt32(),
                Sequence = reader.ReadUInt32()
            };

            if (header.ProtocolVersion != CurrentProtocolVersion)
            {
                throw new ProtoSerializationException($"不支持的协议版本：{header.ProtocolVersion}。");
            }

            if (header.PayloadLength > ProtoRuntimeLimits.DefaultMaxPayloadBytes)
            {
                throw new ProtoSerializationException(
                    $"Payload 长度超出限制：{header.PayloadLength}，最大={ProtoRuntimeLimits.DefaultMaxPayloadBytes}。");
            }

            return header;
        }
    }
}
