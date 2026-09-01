using System;

namespace GameProto
{
    /// <summary>
    /// 配置二进制文件格式错误时抛出的异常。
    /// </summary>
    public class ConfigSerializationException : Exception
    {
        public ConfigSerializationException(string message)
            : base(message)
        {
        }

        public ConfigSerializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
