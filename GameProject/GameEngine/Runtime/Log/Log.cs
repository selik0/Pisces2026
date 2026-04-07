using System;
using System.Threading;

namespace GameEngine
{
    /// <summary>
    /// 全局日志静态入口，线程安全。
    /// 游戏启动时调用 <see cref="Initialize"/> 完成初始化，
    /// 退出时调用 <see cref="Shutdown"/> 释放资源。
    ///
    /// <code>
    /// // 初始化（GameBootstrap.Awake）
    /// Log.Initialize();
    ///
    /// // 全局日志
    /// Log.Debug("游戏启动");
    /// Log.Warning("资源加载缓慢");
    /// Log.Error("网络连接失败", ex);
    ///
    /// // 模块专属 Logger（共享 Handler，独立 Tag）
    /// Logger netLog = Log.GetLogger("Network");
    /// netLog.Debug("连接服务器...");
    ///
    /// // 游戏退出
    /// Log.Shutdown();
    /// </code>
    /// </summary>
    public static class Log
    {
        // 用 object 做双重检查锁的 syncRoot
        private static readonly object _initLock = new object();

        private static Logger _default;
        private static UnityConsoleLogHandler _consoleHandler;
        private static FileLogHandler _fileHandler;

        /// <summary>是否已完成初始化</summary>
        public static bool IsInitialized => _default != null;

        /// <summary>
        /// 初始化全局日志系统（幂等，重复调用安全）
        /// </summary>
        /// <param name="minLevel">全局最低级别（默认 Debug）</param>
        /// <param name="logFilePath">日志文件路径，null 使用默认路径</param>
        /// <param name="enableConsole">是否输出到 Unity 控制台（默认 true）</param>
        /// <param name="enableFile">是否写入文件（默认 true）</param>
        public static void Initialize(
            LogLevel minLevel     = LogLevel.Debug,
            string logFilePath    = null,
            bool enableConsole    = true,
            bool enableFile       = true)
        {
            if (_default != null) return;   // 快速路径（无锁）

            lock (_initLock)
            {
                if (_default != null) return; // 双重检查

                var logger = new Logger { MinLevel = minLevel };

                if (enableConsole)
                {
                    _consoleHandler = new UnityConsoleLogHandler { MinLevel = minLevel };
                    logger.AddHandler(_consoleHandler);
                }

                if (enableFile)
                {
                    _fileHandler = new FileLogHandler(logFilePath) { MinLevel = minLevel };
                    logger.AddHandler(_fileHandler);
                }

                // 最后赋值，保证其他线程看到的是完整初始化的对象
                Volatile.Write(ref _default, logger);
            }
        }

        /// <summary>
        /// 获取带 Tag 的子模块 Logger。
        /// 子 Logger 直接引用全局 Handler，不拥有其生命周期，
        /// Dispose 子 Logger 不会影响全局日志系统。
        /// </summary>
        public static Logger GetLogger(string tag)
        {
            EnsureInitialized();
            var logger = new Logger(tag) { MinLevel = _default.MinLevel };
            // 直接添加共享 Handler；子 Logger.Dispose 只清空自身列表，不会 Dispose Handler
            if (_consoleHandler != null) logger.AddHandler(_consoleHandler);
            if (_fileHandler    != null) logger.AddHandler(_fileHandler);
            return logger;
        }

        /// <summary>释放所有资源，游戏退出时调用</summary>
        public static void Shutdown()
        {
            lock (_initLock)
            {
                if (_default == null) return;

                _default.Dispose();   // 清空 handler 列表（不 Dispose handler 本身）
                _default = null;

                _fileHandler?.Dispose();     // 等待后台写线程结束
                _consoleHandler?.Dispose();

                _fileHandler    = null;
                _consoleHandler = null;
            }
        }

        // ── 全局快捷方法 ─────────────────────────────────────

        public static void Debug(string message)               => Write(LogLevel.Debug,   message);
        public static void Debug(string message, Exception ex) => Write(LogLevel.Debug,   message, ex);
        public static void Warning(string message)             => Write(LogLevel.Warning, message);
        public static void Warning(string message, Exception ex) => Write(LogLevel.Warning, message, ex);
        public static void Error(string message)               => Write(LogLevel.Error,   message);
        public static void Error(string message, Exception ex) => Write(LogLevel.Error,   message, ex);

        // ── 私有辅助 ─────────────────────────────────────────

        private static void Write(LogLevel level, string message, Exception ex = null)
        {
            EnsureInitialized();
            _default.Write(level, message, ex);
        }

        private static void EnsureInitialized()
        {
            if (_default == null)
                Initialize();
        }
    }
}
