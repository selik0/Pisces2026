using System;
using System.IO;
using System.Text;

namespace GameEngine
{
    /// <summary>
    /// 文件日志处理器，将日志异步写入本地文件
    /// 默认文件路径：Application.persistentDataPath/Logs/game_yyyyMMdd.log
    /// 可在构造时传入自定义路径
    /// </summary>
    public class FileLogHandler : ILogHandler
    {
        public LogLevel MinLevel { get; set; } = LogLevel.Debug;

        private readonly StreamWriter _writer;
        private readonly object _lock = new object();

        /// <param name="filePath">日志文件完整路径，为 null 时使用默认路径</param>
        public FileLogHandler(string filePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                string dir = Path.Combine(
                    UnityEngine.Application.persistentDataPath, "Logs");
                Directory.CreateDirectory(dir);
                filePath = Path.Combine(dir,
                    $"game_{DateTime.Now:yyyyMMdd}.log");
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }

            _writer = new StreamWriter(filePath, append: true, encoding: Encoding.UTF8)
            {
                AutoFlush = true
            };
        }

        public void Write(LogLevel level, string tag, string message, Exception exception = null)
        {
            if (level < MinLevel) return;

            string line = FormatLine(level, tag, message, exception);
            lock (_lock)
            {
                _writer.WriteLine(line);
            }
        }

        private static string FormatLine(LogLevel level, string tag, string message, Exception exception)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string header = string.IsNullOrEmpty(tag)
                ? $"[{timestamp}][{level}]"
                : $"[{timestamp}][{level}][{tag}]";

            if (exception != null)
                return $"{header} {message}\n{exception}";

            return $"{header} {message}";
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _writer?.Flush();
                _writer?.Close();
                _writer?.Dispose();
            }
        }
    }
}
