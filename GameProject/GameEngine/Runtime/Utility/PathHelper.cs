using System;
using System.IO;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 游戏运行时路径解析工具。仅负责提供根目录和拼接受限的子路径，不执行文件读写。
    /// </summary>
    public static class PathHelper
    {
        /// <summary>可写持久化根目录（各平台自动映射）。</summary>
        public static string PersistentRoot => Application.persistentDataPath;

        /// <summary>只读资源根目录（StreamingAssets）。</summary>
        public static string StreamingRoot => Application.streamingAssetsPath;

        /// <summary>临时缓存目录。</summary>
        public static string CacheRoot => Application.temporaryCachePath;

        /// <summary>工程资源目录（Assets，运行时只读，编辑器可写）。</summary>
        public static string DataPath => Application.dataPath;

        /// <summary>与 Assets 同级的 EditorData 目录。</summary>
        public static string EditorDataPath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "EditorData");

        /// <summary>拼接持久化目录下的完整路径。</summary>
        public static string ResolvePersistentPath(string relativePath)
        {
            return ResolveChildPath(PersistentRoot, relativePath);
        }

        /// <summary>拼接只读资源目录下的完整路径。</summary>
        public static string ResolveStreamingPath(string relativePath)
        {
            return ResolveChildPath(StreamingRoot, relativePath);
        }

        /// <summary>拼接持久化存档的相对路径。</summary>
        public static string GetSavePath(string roleId, string dataTypeName, bool roleData)
        {
            if (roleData)
            {
                return Path.Combine("Saves", roleId, $"{dataTypeName}.dat");
            }

            return Path.Combine("Saves", $"{dataTypeName}.dat");
        }

        /// <summary>获取文件所在目录。</summary>
        public static string GetDirectoryName(string filePath)
        {
            return Path.GetDirectoryName(filePath);
        }

        private static string ResolveChildPath(string root, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            {
                throw new ArgumentException("Path must be a non-empty relative path.", nameof(relativePath));
            }

            string fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
            string rootPrefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Path cannot escape its root directory.", nameof(relativePath));
            }

            return fullPath;
        }
    }
}
