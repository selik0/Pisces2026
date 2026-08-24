using System;

namespace GameNative
{
    /// <summary>由宿主提供的日志输出服务。</summary>
    public interface ILogService
    {
        void Debug(string message);

        void Warning(string message);

        void Warning(string message, Exception exception);

        void Error(string message);

        void Error(string message, Exception exception);
    }
}
