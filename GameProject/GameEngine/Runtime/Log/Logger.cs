using System;
using System.Collections.Generic;
using System.Threading;

namespace GameEngine
{
    /// <summary>
    /// 日志核心类，管理多个 ILogHandler，支持 Tag 分类输出。
    /// 线程安全：Write 使用快照遍历，AddHandler/RemoveHandler 持锁操作。
    /// </summary>
    public class Logger : IDisposable
    {
        private readonly List<ILogHandler> _handlers = new List<ILogHandler>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private volatile bool _disposed;

        /// <summary>最低日志级别过滤，低于此级别直接丢弃</summary>
        public LogLevel MinLevel { get; set; } = LogLevel.Debug;

        /// <summary>模块标识 Tag</summary>
        public string Tag { get; }

        public Logger(string tag = "")
        {
            Tag = tag ?? string.Empty;
        }

        /// <summary>添加输出处理器（重复添加会被忽略）</summary>
        public Logger AddHandler(ILogHandler handler)
        {
            if (handler == null || _disposed) return this;
            _lock.EnterWriteLock();
            try
            {
                if (!_handlers.Contains(handler))
                    _handlers.Add(handler);
            }
            finally { _lock.ExitWriteLock(); }
            return this;
        }

        /// <summary>移除输出处理器</summary>
        public Logger RemoveHandler(ILogHandler handler)
        {
            if (handler == null || _disposed) return this;
            _lock.EnterWriteLock();
            try { _handlers.Remove(handler); }
            finally { _lock.ExitWriteLock(); }
            return this;
        }

        // ── 快捷写入 ─────────────────────────────────────────

        public void Debug(string message)                    => Write(LogLevel.Debug,   message);
        public void Debug(string message, Exception ex)      => Write(LogLevel.Debug,   message, ex);
        public void Warning(string message)                  => Write(LogLevel.Warning, message);
        public void Warning(string message, Exception ex)    => Write(LogLevel.Warning, message, ex);
        public void Error(string message)                    => Write(LogLevel.Error,   message);
        public void Error(string message, Exception ex)      => Write(LogLevel.Error,   message, ex);

        // ── 核心分发 ─────────────────────────────────────────

        public void Write(LogLevel level, string message, Exception exception = null)
        {
            if (_disposed || level < MinLevel) return;

            // 读锁下复制快照，避免遍历中 Add/Remove 导致的竞态
            ILogHandler[] snapshot;
            _lock.EnterReadLock();
            try { snapshot = _handlers.ToArray(); }
            finally { _lock.ExitReadLock(); }

            foreach (var handler in snapshot)
            {
                try { handler.Write(level, Tag, message, exception); }
                catch { /* 不让 handler 异常影响业务 */ }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _lock.EnterWriteLock();
            try { _handlers.Clear(); }
            finally { _lock.ExitWriteLock(); }

            _lock.Dispose();
        }
    }
}
