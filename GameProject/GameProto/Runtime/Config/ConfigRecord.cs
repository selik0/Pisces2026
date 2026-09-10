namespace GameProto
{
    /// <summary>
    /// 所有配置记录的运行时基类。
    /// </summary>
    public abstract class ConfigRecord
    {
        public abstract int GetEncodedSize();
        public abstract void Encode(ref ProtoWriter writer);
        public abstract void Decode(ref ProtoReader reader);
    }
}
