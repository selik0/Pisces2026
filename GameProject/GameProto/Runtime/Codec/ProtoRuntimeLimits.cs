namespace GameProto
{
    /// <summary>
    /// 二进制运行时的安全分配上限。格式使用 uint，但运行时不会因此允许无限分配。
    /// </summary>
    public static class ProtoRuntimeLimits
    {
        public const int DefaultMaxStringBytes = 16 * 1024 * 1024;
        public const int DefaultMaxBytes = 16 * 1024 * 1024;
        public const int DefaultMaxCollectionCount = 1 * 1024 * 1024;
        public const int DefaultMaxPayloadBytes = 4 * 1024 * 1024;
        public const int DefaultMaxConfigRecordCount = 4 * 1024 * 1024;
    }
}
