using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// FNV-1a 32 位稳定哈希算法，结果与进程、运行时版本无关，适用于需要跨会话保持一致的 Key 生成。
    /// </summary>
    public static class Hash
    {
        private static readonly Dictionary<string, int> _keyCache = new Dictionary<string, int>(StringComparer.Ordinal);
        private static readonly HashSet<int> _usedKeys = new HashSet<int>();

        /// <summary>将字符串转换为稳定的 int 类型哈希值，重复调用同一字符串始终返回同一值。</summary>
        /// <exception cref="ArgumentNullException">字符串为 null。</exception>
        public static int StringToHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Log.Error("Hash.StringToHash: value is null or empty.");
                return -1;
            }
            
            if (_keyCache.TryGetValue(value, out int cached))
            {
                return cached;
            }

            int hash = StableHash(value);
            while (!_usedKeys.Add(hash))
            {
                hash++;
            }

            _keyCache.Add(value, hash);
            return hash;
        }

        /// <summary>计算字符串的 FNV-1a 32 位哈希值。</summary>
        public static int StableHash(string value)
        {
            unchecked
            {
                const uint offsetBasis = 2166136261u;
                const uint prime = 16777619u;
                uint hash = offsetBasis;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= prime;
                }

                return (int)hash;
            }
        }
    }
}
