using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 日志核心类，管理多个 ILogHandler，支持 Tag 分类输出
    /// </summary>
    public class Logger : IDisposable
    {
        private readonly List<ILogHandler> _handlers = new List<ILogHandler>();
        private bool _disposed;

        /// <summary>全局最低日志级别过滤，低于此级别直接丢弃</summary>
        public LogLevel MinLevel { get; set; } = LogLevel.Debug;

        /// <summary>日志 Tag，用于标识模块来源</summary>
        public string Tag { get; }

        public Logger(string tag = "")
        {
            Tag = tag;
        }

        /// <summary>添加输出处理器</summary>
        public Logger AddHandler(ILogHandler handler)
        {
            if (handler != null && !_handlers.Contains(handler))
                _handlers.Add(handler);
            return this;
        }

        /// <summary>移除输出处理器</summary>
        public Logger RemoveHandler(ILogHandler handler)
        {
            _handlers.Remove(handler);
            return this;
        }

        // ── 公共写入方法 ──────────────────────────────────────

        public void Debug(string message) =>
            Write(LogLevel.Debug, message);

        public void Debug(string message, Exception ex) =>
            Write(LogLevel.Debug, message, ex);

        public void Warning(string message) =>
            Write(LogLevel.Warning, message);

        public void Warning(string message, Exception ex) =>
            Write(LogLevel.Warning, message, ex);

        public void Error(string message) =>
            Write(LogLevel.Error, message);

        public void Error(string message, Exception ex) =>
            Write(LogLevel.Error, message, ex);

        // ── 内部分发 ─────────────────────────────────────────

        public void Write(LogLevel level, string message, Exception exception = null)
        {
            if (level < MinLevel) return;

            foreach (var handler in _handlers)
            {
                try
                {
                    handler.Write(level, Tag, message, exception);
                }
                catch
                {
                    // 不让日志系统本身的异常影响业务逻辑
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var handler in _handlers)
            {
                try { handler.Dispose(); } catch { }
            }
            _handlers.Clear();
        }
    }
}
