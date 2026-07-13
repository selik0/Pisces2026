using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// GameObject 销毁事件监听组件。
    /// 挂载到 GameObject 上后，外部代码可订阅 OnDestroyed 事件以获知其销毁。
    /// 适用于 UIView 等非 MonoBehaviour 类监听其所绑定 GameObject 的销毁。
    /// </summary>
    public class DestroyListener : MonoBehaviour
    {
        /// <summary>
        /// GameObject 即将销毁时触发。
        /// </summary>
        public event Action OnDestroyed;

        private void OnDestroy()
        {
            OnDestroyed?.Invoke();
            OnDestroyed = null;
        }
    }
}