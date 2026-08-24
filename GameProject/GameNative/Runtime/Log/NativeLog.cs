using System;

namespace GameNative
{
    /// <summary>
    /// GameNative 日志入口。未注入宿主日志服务时静默忽略，避免底层能力依赖具体日志实现。
    /// </summary>
    public sealed class NativeLog : ServiceBase<ILogService>
    {
        private NativeLog()
        {
        }

        public static void Debug(string message)
        {
            Service?.Debug(message);
        }

        public static void Warning(string message)
        {
            Service?.Warning(message);
        }

        public static void Warning(string message, Exception exception)
        {
            Service?.Warning(message, exception);
        }

        public static void Error(string message)
        {
            Service?.Error(message);
        }

        public static void Error(string message, Exception exception)
        {
            Service?.Error(message, exception);
        }
    }
}
