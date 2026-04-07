using System;

namespace GameEngine
{
    /// <summary>
    /// 日志格式化工具，统一消息格式
    /// </summary>
    internal static class LogFormatter
    {
        /// <summary>
        /// 格式化为带时间戳的完整行（用于文件输出）
        /// 格式：[yyyy-MM-dd HH:mm:ss.fff][Level][Tag] message
        /// </summary>
        internal static string FormatWithTimestamp(LogLevel level, string tag, string message, Exception exception)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string header = BuildHeader(timestamp, level, tag);
            return BuildBody(header, message, exception);
        }

        /// <summary>
        /// 格式化为不带时间戳的行（用于 Unity 控制台，Unity 自身会附加时间）
        /// 格式：[Level][Tag] message
        /// </summary>
        internal static string FormatWithoutTimestamp(LogLevel level, string tag, string message, Exception exception)
        {
            string header = BuildHeader(null, level, tag);
            return BuildBody(header, message, exception);
        }

        private static string BuildHeader(string timestamp, LogLevel level, string tag)
        {
            bool hasTime = !string.IsNullOrEmpty(timestamp);
            bool hasTag  = !string.IsNullOrEmpty(tag);

            if (hasTime && hasTag)  return $"[{timestamp}][{level}][{tag}]";
            if (hasTime)            return $"[{timestamp}][{level}]";
            if (hasTag)             return $"[{level}][{tag}]";
            return                         $"[{level}]";
        }

        private static string BuildBody(string header, string message, Exception exception)
        {
            if (exception != null)
                return $"{header} {message}\n{exception}";
            return $"{header} {message}";
        }
    }
}
