using System;
using System.IO;
using UnityEngine;

namespace GameEngine
{
    /// <summary>基于 AssetBundle.LoadFromFileAsync 的资源加载后端。</summary>
    public class AssetBundleAssetProvider : IAssetProvider
    {
        private sealed class Operation<T> : IDisposableAssetLoadOperation where T : UnityEngine.Object
        {
            private readonly string _bundlePath;
            private readonly string _assetName;
            private AssetBundleCreateRequest _bundleRequest;
            private AssetBundleRequest _assetRequest;
            private AssetBundle _bundle;
            private T _asset;
            private Exception _exception;
            private bool _released;

            public Operation(string bundlePath, string assetName)
            {
                _bundlePath = bundlePath;
                _assetName = assetName;
                _bundleRequest = AssetBundle.LoadFromFileAsync(bundlePath);
            }

            public bool IsDone
            {
                get
                {
                    if (_released || _exception != null)
                    {
                        return true;
                    }

                    if (_bundle == null)
                    {
                        if (!_bundleRequest.isDone)
                        {
                            return false;
                        }

                        _bundle = _bundleRequest.assetBundle;
                        if (_bundle == null)
                        {
                            _exception = new FileNotFoundException("AssetBundle 加载失败。", _bundlePath);
                            return true;
                        }

                        _assetRequest = _bundle.LoadAssetAsync<T>(_assetName);
                    }

                    if (!_assetRequest.isDone)
                    {
                        return false;
                    }

                    _asset = _assetRequest.asset as T;
                    if (_asset == null)
                    {
                        _exception = new InvalidOperationException($"AssetBundle 中未找到资源：{_assetName}");
                    }

                    return true;
                }
            }

            public float Progress
            {
                get
                {
                    if (_bundle == null)
                    {
                        return _bundleRequest == null ? 0f : _bundleRequest.progress * 0.5f;
                    }

                    return 0.5f + (_assetRequest == null ? 0f : _assetRequest.progress * 0.5f);
                }
            }

            public UnityEngine.Object Asset => _asset;

            public Exception Exception => _exception;

            public void Release()
            {
                if (_released)
                {
                    return;
                }

                _released = true;
                if (_bundle != null)
                {
                    _bundle.Unload(false);
                }
            }
        }

        private readonly string _rootPath;

        public AssetBundleAssetProvider(string rootPath)
        {
            _rootPath = rootPath ?? string.Empty;
        }

        public IAssetLoadOperation LoadAsync<T>(string address) where T : UnityEngine.Object
        {
            int separator = address.IndexOf('/');
            if (separator <= 0 || separator >= address.Length - 1)
            {
                throw new ArgumentException("AssetBundle 地址格式必须为 bundleName/assetName。", nameof(address));
            }

            string bundleName = address.Substring(0, separator);
            string assetName = address.Substring(separator + 1);
            return new Operation<T>(Path.Combine(_rootPath, bundleName), assetName);
        }

        public void Release(IAssetLoadOperation operation)
        {
            if (!(operation is IDisposableAssetLoadOperation disposable))
            {
                throw new ArgumentException("加载操作不是由 AssetBundle provider 创建的。", nameof(operation));
            }

            disposable.Release();
        }

        private interface IDisposableAssetLoadOperation : IAssetLoadOperation
        {
            void Release();
        }
    }
}
