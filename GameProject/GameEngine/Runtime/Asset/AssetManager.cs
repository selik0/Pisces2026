using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    /// <summary>资源实例缓存配置。</summary>
    public sealed class AssetLoadOptions
    {
        public int MaxInstanceCount = 8;
        public float CacheDuration = 30f;
        public Transform Parent;
    }

    /// <summary>一次异步资源加载或实例租借的句柄，使用结束后必须释放。</summary>
    public sealed class AssetHandle<T> : IDisposable where T : UnityEngine.Object
    {
        private Action _release;

        internal AssetHandle(T asset, Action release)
        {
            Asset = asset;
            _release = release;
        }

        public T Asset { get; private set; }

        public bool IsReleased => _release == null;

        public void Release()
        {
            Action release = _release;
            _release = null;
            release?.Invoke();
        }

        public void Dispose()
        {
            Release();
        }
    }

    /// <summary>资源加载与实例池管理器。所有资源都通过 LoadAsync 一个入口访问。</summary>
    public sealed class AssetManager : Singleton<AssetManager>, ILogin
    {
        private readonly struct AssetKey : IEquatable<AssetKey>
        {
            public readonly string Address;
            public readonly Type Type;

            public AssetKey(string address, Type type)
            {
                Address = address;
                Type = type;
            }

            public bool Equals(AssetKey other) => Address == other.Address && Type == other.Type;
            public override bool Equals(object obj) => obj is AssetKey other && Equals(other);
            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Address == null ? 0 : Address.GetHashCode()) * 397) ^ Type.GetHashCode();
                }
            }
        }

        private sealed class CacheEntry
        {
            public AssetKey Key;
            public IAssetLoadOperation Operation;
            public int ReferenceCount;
            public float CacheDuration;
            public float ReleaseTime;
            public readonly List<LoadRequest> Requests = new List<LoadRequest>();
        }

        private sealed class LoadRequest
        {
            public Action<float> OnProgress;
            public Action<UnityEngine.Object> OnCompleted;
            public AssetLoadOptions Options;
            public float LastProgress = -1f;
        }

        private sealed class PooledInstance
        {
            public CacheEntry Entry;
            public GameObject Instance;
            public float ReleaseTime;
            public bool InUse;
        }

        private readonly Dictionary<AssetKey, CacheEntry> _entries = new Dictionary<AssetKey, CacheEntry>();
        private readonly Dictionary<AssetKey, List<PooledInstance>> _instances = new Dictionary<AssetKey, List<PooledInstance>>();
        private readonly List<CacheEntry> _tickEntries = new List<CacheEntry>();
        private readonly List<PooledInstance> _tickInstances = new List<PooledInstance>();
        private readonly List<CacheEntry> _expiredEntries = new List<CacheEntry>();
        private IAssetProvider _provider;
        private float _time;

        public AssetManager()
        {
            _provider = new AssetBundleAssetProvider("AssetBundles");
        }

        public int CachedAssetCount => _entries.Count;

        /// <summary>替换资源后端。替换时会清理已有资源句柄和实例。</summary>
        public void SetProvider(IAssetProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (ReferenceEquals(_provider, provider))
            {
                return;
            }

            ClearAll();
            _provider = provider;
        }

        /// <summary>唯一资源入口。普通资源返回资源句柄，GameObject 返回实例池句柄。</summary>
        public void LoadAsync<T>(string address, Action<AssetHandle<T>> onCompleted, Action<float> onProgress = null, AssetLoadOptions options = null)
            where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Log.Error("[AssetManager] LoadAsync 失败：address 不能为空。");
                onCompleted?.Invoke(null);
                return;
            }

            options = Normalize(options);
            AssetKey key = new AssetKey(address, typeof(T));
            if (typeof(T) == typeof(GameObject) && TryRentInstance(key, options.Parent, onCompleted))
            {
                return;
            }

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
                    onCompleted?.Invoke(null);
                    return;
                }

                if (operation == null)
                {
                    Log.Error($"[AssetManager] 资源后端返回了空加载操作：{address}");
                    onCompleted?.Invoke(null);
                    return;
                }

                entry = new CacheEntry { Key = key, Operation = operation };
                _entries.Add(key, entry);
            }

            entry.ReferenceCount++;
            entry.CacheDuration = options.CacheDuration;
            entry.ReleaseTime = float.PositiveInfinity;
            entry.Requests.Add(new LoadRequest
            {
                OnProgress = onProgress,
                Options = options,
                OnCompleted = asset => CompleteRequest(entry, asset, options, onCompleted)
            });
        }

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

            _tickInstances.Clear();
            foreach (List<PooledInstance> instances in _instances.Values)
            {
                _tickInstances.AddRange(instances);
            }

            for (int i = 0; i < _tickInstances.Count; i++)
            {
                PooledInstance pooled = _tickInstances[i];
                if (!pooled.InUse && (pooled.Instance == null || _time >= pooled.ReleaseTime))
                {
                    _instances[pooled.Entry.Key].Remove(pooled);
                    UnityEngine.Object.Destroy(pooled.Instance);
                    ReleaseReference(pooled.Entry);
                }
            }
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

        public void ClearAll()
        {
            foreach (List<PooledInstance> instances in _instances.Values)
            {
                for (int i = 0; i < instances.Count; i++)
                {
                    UnityEngine.Object.Destroy(instances[i].Instance);
                }
            }

            _instances.Clear();
            _expiredEntries.Clear();
            _expiredEntries.AddRange(_entries.Values);
            for (int i = 0; i < _expiredEntries.Count; i++)
            {
                ReleaseEntry(_expiredEntries[i]);
            }

            _expiredEntries.Clear();
        }

        private bool TryRentInstance<T>(AssetKey key, Transform parent, Action<AssetHandle<T>> onCompleted) where T : UnityEngine.Object
        {
            if (!_instances.TryGetValue(key, out List<PooledInstance> instances))
            {
                return false;
            }

            for (int i = 0; i < instances.Count; i++)
            {
                PooledInstance pooled = instances[i];
                if (pooled.InUse)
                {
                    continue;
                }

                if (pooled.Instance == null)
                {
                    instances.RemoveAt(i--);
                    ReleaseReference(pooled.Entry);
                    continue;
                }

                pooled.InUse = true;
                pooled.Instance.transform.SetParent(parent, false);
                onCompleted?.Invoke(new AssetHandle<T>((T)(UnityEngine.Object)pooled.Instance, () => ReturnInstance(pooled)));
                return true;
            }

            return false;
        }

        private void CompleteRequest<T>(CacheEntry entry, UnityEngine.Object asset, AssetLoadOptions options, Action<AssetHandle<T>> onCompleted) where T : UnityEngine.Object
        {
            if (asset == null)
            {
                onCompleted?.Invoke(null);
                ReleaseReference(entry);
                return;
            }

            if (typeof(T) != typeof(GameObject))
            {
                onCompleted?.Invoke(new AssetHandle<T>((T)asset, () => ReleaseReference(entry)));
                return;
            }

            if (!_instances.TryGetValue(entry.Key, out List<PooledInstance> instances))
            {
                instances = new List<PooledInstance>();
                _instances.Add(entry.Key, instances);
            }

            if (options.MaxInstanceCount > 0 && instances.Count >= options.MaxInstanceCount)
            {
                Log.Warning($"[AssetManager] GameObject 实例池已达到数量上限：{entry.Key.Address}");
                onCompleted?.Invoke(null);
                ReleaseReference(entry);
                return;
            }

            PooledInstance pooled = new PooledInstance
            {
                Entry = entry,
                Instance = UnityEngine.Object.Instantiate((GameObject)asset, options.Parent),
                InUse = true
            };
            instances.Add(pooled);
            onCompleted?.Invoke(new AssetHandle<T>((T)(UnityEngine.Object)pooled.Instance, () => ReturnInstance(pooled)));
        }

        private void ReturnInstance(PooledInstance pooled)
        {
            if (!pooled.InUse || pooled.Instance == null)
            {
                return;
            }

            pooled.InUse = false;
            pooled.ReleaseTime = _time + pooled.Entry.CacheDuration;
            pooled.Instance.SetActive(false);
            pooled.Instance.transform.SetParent(null, false);
        }

        private void NotifyCallbacks(CacheEntry entry)
        {
            float progress = entry.Operation.Progress;
            for (int i = 0; i < entry.Requests.Count; i++)
            {
                LoadRequest request = entry.Requests[i];
                if (!Mathf.Approximately(request.LastProgress, progress))
                {
                    request.LastProgress = progress;
                    request.OnProgress?.Invoke(progress);
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
            List<LoadRequest> requests = new List<LoadRequest>(entry.Requests);
            entry.Requests.Clear();
            for (int i = 0; i < requests.Count; i++)
            {
                requests[i].OnCompleted?.Invoke(asset);
            }
        }

        private void ReleaseEntry(CacheEntry entry)
        {
            if (!_entries.Remove(entry.Key))
            {
                return;
            }

            _provider.Release(entry.Operation);
        }

        private void ReleaseReference(CacheEntry entry)
        {
            entry.ReferenceCount--;
            if (entry.ReferenceCount == 0)
            {
                entry.ReleaseTime = _time + entry.CacheDuration;
            }
        }

        private static AssetLoadOptions Normalize(AssetLoadOptions options)
        {
            return new AssetLoadOptions
            {
                MaxInstanceCount = Math.Max(0, options == null ? 8 : options.MaxInstanceCount),
                CacheDuration = Math.Max(0f, options == null ? 30f : options.CacheDuration),
                Parent = options == null ? null : options.Parent
            };
        }
    }
}
