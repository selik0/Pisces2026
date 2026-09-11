using System;

namespace GameProto
{
    /// <summary>
    /// GameProto 配置日志入口。未注入宿主日志服务时静默忽略，避免协议层依赖具体日志实现。
    /// </summary>
    public static class ConfigLog
    {
        private static ILogService _service;

        /// <summary>注入宿主提供的日志服务，重复调用会覆盖旧实现。</summary>
        /// <param name="service">日志服务实现，不能为 null。</param>
        public static void SetService(ILogService service)
        {
            if (service == null)
            {
                return;
            }

            _service = service;
        }

        /// <summary>清空注入的日志服务。</summary>
        public static void ClearService()
        {
            _service = null;
        }

        public static void Debug(string message)
        {
            _service?.Debug(message);
        }

        public static void Warning(string message)
        {
            _service?.Warning(message);
        }

        public static void Warning(string message, Exception exception)
        {
            _service?.Warning(message, exception);
        }

        public static void Error(string message)
        {
            _service?.Error(message);
        }

        public static void Error(string message, Exception exception)
        {
            _service?.Error(message, exception);
        }
    }
}
