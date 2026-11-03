using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 单次底层资源加载操作，仅供资源管理器与加载后端交互。
    /// </summary>
    public interface IAssetLoadOperation
    {
        bool IsDone { get; }

        float Progress { get; }

        UnityEngine.Object Asset { get; }

        Exception Exception { get; }
    }

    /// <summary>
    /// 资源加载后端。运行时默认使用自定义 AssetBundle 后端，也可以替换为其他自定义实现。
    /// </summary>
    public interface IAssetProvider
    {
        IAssetLoadOperation LoadAsync<T>(string address) where T : UnityEngine.Object;

        void Release(IAssetLoadOperation operation);
    }
}
