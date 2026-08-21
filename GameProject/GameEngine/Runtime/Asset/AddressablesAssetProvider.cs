using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GameEngine
{
    /// <summary>基于 Unity Addressables 的默认资源加载后端。</summary>
    public sealed class AddressablesAssetProvider : IAssetProvider
    {
        private interface IAddressablesOperation : IAssetLoadOperation
        {
            void Release();
        }

        private sealed class Operation<T> : IAddressablesOperation where T : UnityEngine.Object
        {
            public AsyncOperationHandle<T> Handle;

            public bool IsDone => Handle.IsDone;

            public float Progress => Handle.PercentComplete;

            public UnityEngine.Object Asset => Handle.Status == AsyncOperationStatus.Succeeded ? Handle.Result : null;

            public Exception Exception => Handle.OperationException;

            public void Release()
            {
                Addressables.Release(Handle);
            }
        }

        public IAssetLoadOperation LoadAsync<T>(string address) where T : UnityEngine.Object
        {
            return new Operation<T>
            {
                Handle = Addressables.LoadAssetAsync<T>(address)
            };
        }

        public void Release(IAssetLoadOperation operation)
        {
            if (!(operation is IAddressablesOperation addressablesOperation))
            {
                throw new ArgumentException("加载操作不是由当前 Addressables provider 创建的。", nameof(operation));
            }

            addressablesOperation.Release();
        }
    }
}
