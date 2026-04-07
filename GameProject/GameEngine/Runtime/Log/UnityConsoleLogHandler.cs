using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// Unity 控制台日志处理器
    /// Debug   → UnityEngine.Debug.Log
    /// Warning → UnityEngine.Debug.LogWarning
    /// Error   → UnityEngine.Debug.LogError
    /// </summary>
    public sealed class UnityConsoleLogHandler : ILogHandler
    {
        public LogLevel MinLevel { get; set; } = LogLevel.Debug;

        public void Write(LogLevel level, string tag, string message, Exception exception = null)
        {
            if (level < MinLevel) return;

            string text = LogFormatter.FormatWithoutTimestamp(level, tag, message, exception);

            switch (level)
            {
                case LogLevel.Debug:
                    Debug.Log(text);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(text);
                    break;
                case LogLevel.Error:
                    Debug.LogError(text);
                    break;
            }
        }

        public void Dispose() { }
    }
}
