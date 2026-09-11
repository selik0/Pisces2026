namespace GameProto
{
    /// <summary>
    /// 配置文件固定20字节头部。
    /// </summary>
    public struct ConfigFileHeader
    {
        public const int HeaderSize = 20;

        public uint Magic;
        public uint FormatVersion;
        public ulong SchemaHash;
        public uint RecordCount;

        public void Encode(ref ProtoWriter writer)
        {
            writer.WriteUInt32(Magic);
            writer.WriteUInt32(FormatVersion);
            writer.WriteUInt64(SchemaHash);
            writer.WriteUInt32(RecordCount);
        }

        public static ConfigFileHeader Decode(ref ProtoReader reader)
        {
            return new ConfigFileHeader
            {
                Magic = reader.ReadUInt32(),
                FormatVersion = reader.ReadUInt32(),
                SchemaHash = reader.ReadUInt64(),
                RecordCount = reader.ReadUInt32()
            };
        }
    }
}
