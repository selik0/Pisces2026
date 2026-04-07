using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace GameEngine
{
    /// <summary>
    /// 文件日志处理器
    /// 使用后台线程 + BlockingCollection 实现非阻塞异步写入，业务线程不会因 IO 阻塞。
    /// 默认路径：Application.persistentDataPath/Logs/game_yyyyMMdd.log
    /// </summary>
    public sealed class FileLogHandler : ILogHandler
    {
        public LogLevel MinLevel { get; set; } = LogLevel.Debug;

        private readonly BlockingCollection<string> _queue =
            new BlockingCollection<string>(boundedCapacity: 4096);
        private readonly Thread _writeThread;
        private readonly StreamWriter _writer;
        private bool _disposed;

        /// <param name="filePath">日志文件完整路径，null 则使用默认路径</param>
        public FileLogHandler(string filePath = null)
        {
            filePath = ResolveFilePath(filePath);

            _writer = new StreamWriter(filePath, append: true, encoding: Encoding.UTF8)
            {
                AutoFlush = false   // 由后台线程定时 Flush，避免每条都 IO
            };

            _writeThread = new Thread(ConsumeLoop)
            {
                Name = "FileLogHandler",
                IsBackground = true   // 随主进程退出，不阻止关闭
            };
            _writeThread.Start();
        }

        public void Write(LogLevel level, string tag, string message, Exception exception = null)
        {
            if (level < MinLevel || _disposed) return;

            string line = LogFormatter.FormatWithTimestamp(level, tag, message, exception);

            // TryAdd 失败（队列满）时静默丢弃，避免业务阻塞
            _queue.TryAdd(line);
        }

        /// <summary>后台消费线程：逐行写入并定期 Flush</summary>
        private void ConsumeLoop()
        {
            try
            {
                foreach (string line in _queue.GetConsumingEnumerable())
                {
                    _writer.WriteLine(line);

                    // 队列空时 Flush，减少 IO 次数
                    if (_queue.Count == 0)
                        _writer.Flush();
                }
            }
            catch (ObjectDisposedException) { }
            finally
            {
                // 确保退出前 Flush
                try { _writer.Flush(); } catch { }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _queue.CompleteAdding();         // 通知后台线程不再有新消息
            _writeThread.Join(millisecondsTimeout: 3000); // 最多等 3 秒让后台线程写完

            _writer.Flush();
            _writer.Close();
            _writer.Dispose();
            _queue.Dispose();
        }

        // ── 私有辅助 ─────────────────────────────────────────

        private static string ResolveFilePath(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                return filePath;
            }

            string logsDir = Path.Combine(
                UnityEngine.Application.persistentDataPath, "Logs");
            Directory.CreateDirectory(logsDir);
            return Path.Combine(logsDir, $"game_{DateTime.Now:yyyyMMdd}.log");
        }
    }
}
