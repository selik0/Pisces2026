using System;
using System.Collections.Generic;
using System.IO;
using GameNative;

namespace GameEngine
{
    /// <summary>
    /// GameEngine 本地存档入口。
    /// </summary>
    public sealed class LocalDataManager : Singleton<LocalDataManager>
    {
        private static readonly byte[] EncryptionKey =
        {
            0x50, 0x69, 0x73, 0x63, 0x65, 0x73, 0x32, 0x30,
            0x32, 0x36, 0x4C, 0x6F, 0x63, 0x61, 0x6C, 0x44,
            0x61, 0x74, 0x61, 0x53, 0x65, 0x63, 0x75, 0x72,
            0x69, 0x74, 0x79, 0x4B, 0x65, 0x79, 0x21, 0x23
        };

        private readonly LocalDataStore _store;
        private readonly Dictionary<Type, BaseSaveData> _cacheDict = new Dictionary<Type, BaseSaveData>();
        public LocalDataManager()
        {
            _store = new LocalDataStore(FileSystem.PersistentRoot, EncryptionKey);
        }

        /// <summary>保存本地数据，数据类型决定存储文件。</summary>
        public void Save<T>(T data) where T : BaseSaveData
        {
            _store.Save(GetRelativePath<T>(), data);
        }

        /// <summary>读取本地数据，数据类型决定存储文件。</summary>
        public T Load<T>() where T : BaseSaveData, new()
        {
            if (_cacheDict.TryGetValue(typeof(T), out BaseSaveData data))
            {
                return (T)data;
            }

            var path = GetRelativePath<T>();
            if (!_store.Exists(path))
            {
                return new T();
            }

            return _store.Load<T>(path);
        }

        /// <summary>删除指定类型的本地数据。</summary>
        public void Delete<T>() where T : BaseSaveData, new()
        {
            _store.Delete(GetRelativePath<T>());
        }

        private static string GetRelativePath<T>() where T : BaseSaveData
        {
            Type dataType = typeof(T);
            if (dataType.IsAbstract || dataType.ContainsGenericParameters)
            {
                throw new InvalidOperationException($"存档数据必须是具体类型：{dataType.FullName}");
            }

            if (dataType.BaseType.Name == nameof(RoleSaveData))
            {
                var roleId = 0u;
                return Path.Combine("Saves", roleId.ToString(), $"{dataType.Name}.dat");
            }
            return Path.Combine("Saves", $"{dataType.Name}.dat");
        }
    }
}
