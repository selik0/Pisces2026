using System;
using GameNative;

namespace GameEngine
{
    /// <summary>将 GameNative 日志转发到 GameEngine 完整日志系统。</summary>
    internal sealed class GameNativeLogService : ILogService
    {
        public void Debug(string message)
        {
            Log.Debug(FormatMessage(message));
        }

        public void Warning(string message)
        {
            Log.Warning(FormatMessage(message));
        }

        public void Warning(string message, Exception exception)
        {
            Log.Warning(FormatMessage(message), exception);
        }

        public void Error(string message)
        {
            Log.Error(FormatMessage(message));
        }

        public void Error(string message, Exception exception)
        {
            Log.Error(FormatMessage(message), exception);
        }

        private static string FormatMessage(string message)
        {
            return $"[GameNative] {message}";
        }
    }
}
