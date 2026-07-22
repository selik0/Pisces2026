using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 单个事件监听绑定，持有回调、单次触发标记与可选的 GameObject 生命周期对象。
    /// </summary>
    /// <typeparam name="TCallback">事件回调委托类型</typeparam>
    internal sealed class EventBinding<TCallback> where TCallback : Delegate
    {
        private readonly GameObject _boundObject;
        private readonly bool _hasBoundObject;

        public TCallback Callback { get; }

        public bool Once { get; }

        public bool IsExpired => _hasBoundObject && _boundObject == null;

        public EventBinding(TCallback callback, bool once, GameObject boundObject)
        {
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            Once = once;
            _boundObject = boundObject;
            _hasBoundObject = boundObject != null;
        }

        public bool IsBoundTo(GameObject target)
        {
            return _hasBoundObject && _boundObject == target;
        }
    }
}
