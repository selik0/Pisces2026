using System;

namespace GameProto
{
    /// <summary>
    /// protobuf 编解码协议错误：数据格式错误、wire type 不匹配、越界读取等。
    /// </summary>
    public class ProtoProtocolException : Exception
    {
        public ProtoProtocolException()
        {
        }

        public ProtoProtocolException(string message) : base(message)
        {
        }

        public ProtoProtocolException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
