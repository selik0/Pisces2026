using System;

namespace GameEngine
{
    /// <summary>
    /// 日志处理器接口，可扩展多种输出目标
    /// </summary>
    public interface ILogHandler : IDisposable
    {
        /// <summary>
        /// 处理器支持的最低日志级别，低于此级别的日志不处理
        /// </summary>
        LogLevel MinLevel { get; set; }

        /// <summary>
        /// 写入一条日志
        /// </summary>
        void Write(LogLevel level, string tag, string message, Exception exception = null);
    }
}
