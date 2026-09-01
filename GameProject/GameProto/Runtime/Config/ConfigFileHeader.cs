using System;

namespace GameProto
{
    /// <summary>
    /// 配置文件固定20字节头部。
    /// </summary>
    public struct ConfigFileHeader
    {
        public const int HeaderSize = 20;
        public const uint Magic = 0x47464347;
        public const uint CurrentFormatVersion = 1;

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
            uint magic = reader.ReadUInt32();
            if (magic != Magic)
            {
                throw new ConfigSerializationException($"配置文件 Magic 错误：0x{magic:X8}。");
            }

            uint formatVersion = reader.ReadUInt32();
            if (formatVersion != CurrentFormatVersion)
            {
                throw new ConfigSerializationException($"不支持的配置格式版本：{formatVersion}。");
            }

            return new ConfigFileHeader
            {
                FormatVersion = formatVersion,
                SchemaHash = reader.ReadUInt64(),
                RecordCount = reader.ReadUInt32()
            };
        }
    }
}
