using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// Unity 控制台日志处理器，将日志输出到 Unity Editor Console 及运行时日志
    /// </summary>
    public class UnityConsoleLogHandler : ILogHandler
    {
        public LogLevel MinLevel { get; set; } = LogLevel.Debug;

        public void Write(LogLevel level, string tag, string message, Exception exception = null)
        {
            if (level < MinLevel) return;

            string formatted = FormatMessage(level, tag, message, exception);

            switch (level)
            {
                case LogLevel.Debug:
                    Debug.Log(formatted);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formatted);
                    break;
                case LogLevel.Error:
                    Debug.LogError(formatted);
                    break;
            }
        }

        private static string FormatMessage(LogLevel level, string tag, string message, Exception exception)
        {
            string header = string.IsNullOrEmpty(tag)
                ? $"[{level}]"
                : $"[{level}][{tag}]";

            if (exception != null)
                return $"{header} {message}\n{exception}";

            return $"{header} {message}";
        }

        public void Dispose() { }
    }
}
