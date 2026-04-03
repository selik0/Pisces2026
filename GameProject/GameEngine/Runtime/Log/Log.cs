using System;

namespace GameEngine
{
    /// <summary>
    /// 全局日志静态入口
    /// 在游戏启动时调用 Log.Initialize() 完成初始化，
    /// 退出时调用 Log.Shutdown() 释放资源。
    ///
    /// 使用示例：
    /// <code>
    /// // 初始化（在 GameBootstrap 或 Awake 中）
    /// Log.Initialize();
    ///
    /// // 输出日志
    /// Log.Debug("游戏启动");
    /// Log.Warning("资源加载缓慢");
    /// Log.Error("网络连接失败", ex);
    ///
    /// // 带 Tag 的子模块 Logger
    /// var netLog = Log.GetLogger("Network");
    /// netLog.Debug("连接服务器...");
    /// </code>
    /// </summary>
    public static class Log
    {
        private static Logger _default;
        private static UnityConsoleLogHandler _consoleHandler;
        private static FileLogHandler _fileHandler;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public static bool IsInitialized => _default != null;

        /// <summary>
        /// 初始化全局日志系统
        /// </summary>
        /// <param name="minLevel">全局最低日志级别（默认 Debug）</param>
        /// <param name="logFilePath">日志文件路径，null 表示使用默认路径</param>
        /// <param name="enableConsole">是否启用 Unity 控制台输出（默认 true）</param>
        /// <param name="enableFile">是否启用文件输出（默认 true）</param>
        public static void Initialize(
            LogLevel minLevel = LogLevel.Debug,
            string logFilePath = null,
            bool enableConsole = true,
            bool enableFile = true)
        {
            if (_default != null)
                return;

            _default = new Logger();
            _default.MinLevel = minLevel;

            if (enableConsole)
            {
                _consoleHandler = new UnityConsoleLogHandler { MinLevel = minLevel };
                _default.AddHandler(_consoleHandler);
            }

            if (enableFile)
            {
                _fileHandler = new FileLogHandler(logFilePath) { MinLevel = minLevel };
                _default.AddHandler(_fileHandler);
            }
        }

        /// <summary>
        /// 创建带 Tag 的子模块 Logger，共享同一批 Handler
        /// </summary>
        public static Logger GetLogger(string tag)
        {
            EnsureInitialized();
            var logger = new Logger(tag);
            logger.MinLevel = _default.MinLevel;
            if (_consoleHandler != null) logger.AddHandler(_consoleHandler);
            if (_fileHandler != null)    logger.AddHandler(_fileHandler);
            return logger;
        }

        /// <summary>释放所有资源，游戏退出时调用</summary>
        public static void Shutdown()
        {
            _default?.Dispose();
            _default = null;
            _consoleHandler = null;
            _fileHandler = null;
        }

        // ── 快捷方法 ─────────────────────────────────────────

        public static void Debug(string message)
        {
            EnsureInitialized();
            _default.Debug(message);
        }

        public static void Debug(string message, Exception ex)
        {
            EnsureInitialized();
            _default.Debug(message, ex);
        }

        public static void Warning(string message)
        {
            EnsureInitialized();
            _default.Warning(message);
        }

        public static void Warning(string message, Exception ex)
        {
            EnsureInitialized();
            _default.Warning(message, ex);
        }

        public static void Error(string message)
        {
            EnsureInitialized();
            _default.Error(message);
        }

        public static void Error(string message, Exception ex)
        {
            EnsureInitialized();
            _default.Error(message, ex);
        }

        // ── 私有辅助 ─────────────────────────────────────────

        private static void EnsureInitialized()
        {
            if (_default == null)
                Initialize();
        }
    }
}
