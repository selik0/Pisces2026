namespace GameNative
{
    /// <summary>本地数据序列化接口。</summary>
    public interface ILocalDataSerializer
    {
        byte[] Serialize<T>(T value);

        T Deserialize<T>(byte[] data);
    }
}
