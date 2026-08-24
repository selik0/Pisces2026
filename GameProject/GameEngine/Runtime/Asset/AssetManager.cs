using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 资源加载与缓存管理器。相同地址和类型共享底层加载操作，并按加载和释放次数计数。
    /// 需要在主循环中调用 <see cref="Tick"/> 才会回收已过缓存期的资源。
    /// </summary>
    public sealed class AssetManager : Singleton<AssetManager>, ILogin
    {
        internal readonly struct AssetKey : IEquatable<AssetKey>
        {
            public readonly string Address;
            public readonly Type Type;

            public AssetKey(string address, Type type)
            {
                Address = address;
                Type = type;
            }

            public bool Equals(AssetKey other)
            {
                return Address == other.Address && Type == other.Type;
            }

            public override bool Equals(object obj)
            {
                return obj is AssetKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Address != null ? Address.GetHashCode() : 0) * 397) ^ Type.GetHashCode();
                }
            }
        }

        internal sealed class CacheEntry
        {
            public AssetKey Key;
            public IAssetLoadOperation Operation;
            public int ReferenceCount;
            public float CacheDuration;
            public float ReleaseTime;
            public readonly List<LoadCallback> Callbacks = new List<LoadCallback>();
        }

        internal sealed class LoadCallback
        {
            public Action<float> OnProgress;
            public Action<UnityEngine.Object> OnCompleted;
            public float LastProgress = -1f;
        }

        private readonly Dictionary<AssetKey, CacheEntry> _entries = new Dictionary<AssetKey, CacheEntry>();
        private readonly List<CacheEntry> _tickEntries = new List<CacheEntry>();
        private readonly List<CacheEntry> _expiredEntries = new List<CacheEntry>();
        private readonly IAssetProvider _provider;
        private float _time;

        public AssetManager()
        {
            _provider = new AddressablesAssetProvider();
        }

        public int CachedAssetCount => _entries.Count;

        /// <summary>
        /// 异步加载资源。相同地址和类型只执行一次底层加载，每次调用增加一次引用。
        /// 使用方不再需要资源时，必须调用 <see cref="Release{T}"/> 释放对应引用。
        /// </summary>
        /// <param name="address">由当前资源后端解释的资源地址。</param>
        /// <param name="cacheDuration">最后一个引用释放后继续缓存的秒数，每个资源可独立设置。</param>
        /// <param name="onProgress">加载进度变化时调用，范围为 0 到 1。</param>
        /// <param name="onCompleted">加载完成时调用；加载失败时传入 null。</param>
        public void LoadAsync<T>(
            string address,
            Action<T> onCompleted,
            Action<float> onProgress = null,
            float cacheDuration = 30f) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Log.Error("[AssetManager] LoadAsync 失败：address 不能为空。");
                InvokeCompleted(onCompleted, null, address);
                return;
            }

            AssetKey key = new AssetKey(address, typeof(T));
            if (!_entries.TryGetValue(key, out CacheEntry entry))
            {
                IAssetLoadOperation operation;
                try
                {
                    operation = _provider.LoadAsync<T>(address);
                }
                catch (Exception exception)
                {
                    Log.Error($"[AssetManager] 启动资源加载失败：{address}", exception);
                    InvokeCompleted(onCompleted, null, address);
                    return;
                }

                if (operation == null)
                {
                    Log.Error($"[AssetManager] 资源后端返回了空加载操作：{address}");
                    InvokeCompleted(onCompleted, null, address);
                    return;
                }

                entry = new CacheEntry
                {
                    Key = key,
                    Operation = operation
                };
                _entries.Add(key, entry);
            }

            entry.ReferenceCount++;
            entry.CacheDuration = Math.Max(0f, cacheDuration);
            entry.ReleaseTime = float.PositiveInfinity;
            entry.Callbacks.Add(new LoadCallback
            {
                OnProgress = onProgress,
                OnCompleted = asset => onCompleted?.Invoke(asset as T)
            });
        }

        /// <summary>释放一次指定资源引用。引用归零后按该资源的缓存时间延迟卸载。</summary>
        public void Release<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                return;
            }

            AssetKey key = new AssetKey(address, typeof(T));
            if (!_entries.TryGetValue(key, out CacheEntry entry) || entry.ReferenceCount <= 0)
            {
                Log.Warning($"[AssetManager] 释放了未持有的资源引用：{address} ({typeof(T).Name})");
                return;
            }

            entry.ReferenceCount--;
            if (entry.ReferenceCount == 0)
            {
                entry.ReleaseTime = _time + entry.CacheDuration;
            }
        }

        /// <summary>推进缓存时间并释放到期资源，应传入不受 Time.timeScale 影响的帧间隔。</summary>
        public void Tick(float unscaledDeltaTime)
        {
            _time += Math.Max(0f, unscaledDeltaTime);
            _tickEntries.Clear();
            _tickEntries.AddRange(_entries.Values);
            _expiredEntries.Clear();

            for (int i = 0; i < _tickEntries.Count; i++)
            {
                CacheEntry entry = _tickEntries[i];
                NotifyCallbacks(entry);
                if (entry.ReferenceCount == 0 && _time >= entry.ReleaseTime)
                {
                    _expiredEntries.Add(entry);
                }
            }

            for (int i = 0; i < _expiredEntries.Count; i++)
            {
                ReleaseEntry(_expiredEntries[i]);
            }
        }

        public void ClearAll()
        {
            _expiredEntries.Clear();
            _tickEntries.Clear();
            _expiredEntries.AddRange(_entries.Values);
            for (int i = 0; i < _expiredEntries.Count; i++)
            {
                ReleaseEntry(_expiredEntries[i]);
            }

            _expiredEntries.Clear();
        }

        public void Login()
        {
            ClearAll();
            _time = 0f;
        }

        public void Logout()
        {
            ClearAll();
        }

        private void NotifyCallbacks(CacheEntry entry)
        {
            if (entry.Callbacks.Count == 0)
            {
                return;
            }

            float progress = entry.Operation.Progress;
            for (int i = 0; i < entry.Callbacks.Count; i++)
            {
                LoadCallback callback = entry.Callbacks[i];
                if (!Mathf.Approximately(callback.LastProgress, progress))
                {
                    callback.LastProgress = progress;
                    try
                    {
                        callback.OnProgress?.Invoke(progress);
                    }
                    catch (Exception exception)
                    {
                        Log.Error($"[AssetManager] 资源进度回调异常：{entry.Key.Address}", exception);
                    }
                }
            }

            if (!entry.Operation.IsDone)
            {
                return;
            }

            if (entry.Operation.Exception != null)
            {
                Log.Error($"[AssetManager] 资源加载失败：{entry.Key.Address}", entry.Operation.Exception);
            }

            UnityEngine.Object asset = entry.Operation.Asset;
            for (int i = 0; i < entry.Callbacks.Count; i++)
            {
                try
                {
                    entry.Callbacks[i].OnCompleted?.Invoke(asset);
                }
                catch (Exception exception)
                {
                    Log.Error($"[AssetManager] 资源完成回调异常：{entry.Key.Address}", exception);
                }
            }

            entry.Callbacks.Clear();
        }

        private void ReleaseEntry(CacheEntry entry)
        {
            if (!_entries.Remove(entry.Key))
            {
                return;
            }

            try
            {
                _provider.Release(entry.Operation);
            }
            catch (Exception exception)
            {
                Log.Error($"[AssetManager] 释放资源失败：{entry.Key.Address}", exception);
            }
        }

        private static void InvokeCompleted<T>(Action<T> onCompleted, T asset, string address) where T : UnityEngine.Object
        {
            try
            {
                onCompleted?.Invoke(asset);
            }
            catch (Exception exception)
            {
                Log.Error($"[AssetManager] 资源完成回调异常：{address}", exception);
            }
        }
    }
}
