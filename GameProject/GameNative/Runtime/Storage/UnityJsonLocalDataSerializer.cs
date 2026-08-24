using System;
using System.Text;
using UnityEngine;

namespace GameNative
{
    /// <summary>使用 Unity JsonUtility 的 UTF-8 JSON 序列化器。</summary>
    public sealed class UnityJsonLocalDataSerializer : ILocalDataSerializer
    {
        public byte[] Serialize<T>(T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(value));
        }

        public T Deserialize<T>(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            T value = JsonUtility.FromJson<T>(Encoding.UTF8.GetString(data));
            if (value == null)
            {
                throw new InvalidOperationException($"无法将本地数据反序列化为 {typeof(T).FullName}。");
            }

            return value;
        }
    }
}
