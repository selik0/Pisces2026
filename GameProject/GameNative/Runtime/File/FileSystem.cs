using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GameNative
{
    /// <summary>
    /// 跨平台文件读写工具。
    /// <para>
    /// 覆盖 PC（Windows/macOS/Linux）、Android、iOS、WebGL 平台。
    /// 各平台统一使用 <see cref="UnityEngine.Application.persistentDataPath"/> 作为可写根目录，
    /// 使用 <see cref="UnityEngine.Application.streamingAssetsPath"/> 作为只读资源目录。
    /// </para>
    /// <para>
    /// 注意：Android / WebGL 的 StreamingAssets 位于压缩包或远端，无法直接用 File 读取，
    /// 这里统一走 <see cref="UnityEngine.Networking.UnityWebRequest"/>（阻塞式，需在主线程调用）。
    /// </para>
    /// </summary>
    public static class FileSystem
    {

        // ── 根目录 ─────────────────────────────────────────────────────────────

        /// <summary>可写持久化根目录（各平台自动映射）</summary>
        public static string PersistentRoot => Application.persistentDataPath;

        /// <summary>只读资源根目录（StreamingAssets）</summary>
        public static string StreamingRoot => Application.streamingAssetsPath;

        /// <summary>临时缓存目录</summary>
        public static string CacheRoot => Application.temporaryCachePath;

        /// <summary>工程资源目录（Assets，运行时只读，编辑器可写）</summary>
        public static string DataPath => Application.dataPath;

        /// <summary>与 Assets 同级的 EditorData 目录</summary>
        public static string EditorDataPath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "EditorData");

        // ── 路径解析 ───────────────────────────────────────────────────────────

        /// <summary>拼接持久化目录下的完整路径</summary>
        public static string ResolvePersistentPath(string relativePath)
        {
            return Path.Combine(PersistentRoot, relativePath);
        }

        /// <summary>拼接只读资源目录下的完整路径</summary>
        public static string ResolveStreamingPath(string relativePath)
        {
            return Path.Combine(StreamingRoot, relativePath);
        }

        // ── 目录与文件管理 ─────────────────────────────────────────────────────

        /// <summary>确保目录存在，不存在则创建</summary>
        public static void EnsureDirectory(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>确保文件所在目录存在</summary>
        public static void EnsureFileDirectory(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                EnsureDirectory(directory);
            }
        }

        /// <summary>判断普通文件是否存在（不适用于 Android/WebGL 的 StreamingAssets）</summary>
        public static bool Exists(string path)
        {
            return File.Exists(path);
        }

        /// <summary>删除文件，不存在时忽略</summary>
        public static void Delete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>获取目录下的全部文件</summary>
        public static string[] GetFiles(string directoryPath, string searchPattern = "*", bool recursive = false)
        {
            if (!Directory.Exists(directoryPath))
            {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(directoryPath, searchPattern, recursive
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly);
        }

        // ── 文本读写 ───────────────────────────────────────────────────────────

        /// <summary>写入文本（默认 UTF-8，覆盖写）</summary>
        /// <param name="path">文件完整路径</param>
        /// <param name="content">文本内容</param>
        /// <param name="append">是否追加到文件末尾，默认 false 覆盖写</param>
        /// <param name="encoding">编码，默认 UTF-8</param>
        public static void WriteAllText(string path, string content, bool append = false, Encoding encoding = null)
        {
            EnsureFileDirectory(path);
            File.WriteAllText(path, content ?? string.Empty, encoding ?? Encoding.UTF8);
        }

        /// <summary>读取文本（UTF-8），文件不存在时抛出异常</summary>
        public static string ReadAllText(string path, Encoding encoding = null)
        {
            return File.ReadAllText(path, encoding ?? Encoding.UTF8);
        }

        // ── 二进制读写 ─────────────────────────────────────────────────────────

        /// <summary>写入二进制数据（覆盖写）</summary>
        public static void WriteAllBytes(string path, byte[] data)
        {
            EnsureFileDirectory(path);
            File.WriteAllBytes(path, data ?? Array.Empty<byte>());
        }

        /// <summary>读取二进制数据，文件不存在时抛出异常</summary>
        public static byte[] ReadAllBytes(string path)
        {
            return File.ReadAllBytes(path);
        }

        // ── 持久化目录便捷读写 ─────────────────────────────────────────────────

        /// <summary>写入文本到持久化目录</summary>
        /// <param name="relativePath">相对持久化目录的路径，例如 "User/save.txt"</param>
        public static void WritePersistentText(string relativePath, string content, bool append = false)
        {
            WriteAllText(ResolvePersistentPath(relativePath), content, append);
        }

        /// <summary>从持久化目录读取文本</summary>
        public static string ReadPersistentText(string relativePath)
        {
            return ReadAllText(ResolvePersistentPath(relativePath));
        }

        /// <summary>写入二进制数据到持久化目录</summary>
        public static void WritePersistentBytes(string relativePath, byte[] data)
        {
            WriteAllBytes(ResolvePersistentPath(relativePath), data);
        }

        /// <summary>从持久化目录读取二进制数据</summary>
        public static byte[] ReadPersistentBytes(string relativePath)
        {
            return ReadAllBytes(ResolvePersistentPath(relativePath));
        }

        // ── StreamingAssets 读取 ──────────────────────────────────────────────

        /// <summary>
        /// 从只读资源目录读取文本。
        /// Android/WebGL 的 StreamingAssets 无法用 File 直接读取，统一走 UnityWebRequest。
        /// 阻塞式读取，需在 Unity 主线程调用。
        /// </summary>
        /// <param name="relativePath">相对 StreamingAssets 的路径，例如 "Config/game.json"</param>
        public static string ReadStreamingText(string relativePath, Encoding encoding = null)
        {
            byte[] data = ReadStreamingBytes(relativePath);
            return (encoding ?? Encoding.UTF8).GetString(data);
        }

        /// <summary>
        /// 从只读资源目录读取二进制数据。
        /// Android/WebGL 的 StreamingAssets 无法用 File 直接读取，统一走 UnityWebRequest。
        /// 阻塞式读取，需在 Unity 主线程调用。
        /// </summary>
        /// <param name="relativePath">相对 StreamingAssets 的路径，例如 "Config/game.json"</param>
        public static byte[] ReadStreamingBytes(string relativePath)
        {
            string path = ResolveStreamingPath(relativePath);

            if (Platform.IsAndroid || Platform.IsWebGL)
            {
                return ReadViaWebRequest(path);
            }

            return File.ReadAllBytes(path);
        }

        // ── 私有辅助 ───────────────────────────────────────────────────────────

        /// <summary>通过 UnityWebRequest 阻塞读取资源（用于 Android/WebGL 的 StreamingAssets）</summary>
        private static byte[] ReadViaWebRequest(string uri)
        {
            using (var request = UnityWebRequest.Get(uri))
            {
                var operation = request.SendWebRequest();

                // 阻塞等待完成，同步 API 需要主线程执行
                while (!operation.isDone)
                {
                    // 让出当前帧，避免长时间占用主线程
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new IOException($"读取资源失败: {uri}，错误: {request.error}");
                }

                return request.downloadHandler.data;
            }
        }
    }
}
