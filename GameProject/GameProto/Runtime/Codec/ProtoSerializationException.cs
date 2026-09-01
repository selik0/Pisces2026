using System;

namespace GameProto
{
    /// <summary>
    /// 二进制协议数据格式错误或无法编码时抛出的异常。
    /// </summary>
    public class ProtoSerializationException : Exception
    {
        public ProtoSerializationException(string message)
            : base(message)
        {
        }

        public ProtoSerializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
