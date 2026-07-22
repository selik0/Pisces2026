using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 同一 EventKey 下固定回调签名的绑定列表。
    /// </summary>
    internal sealed class BindingList<TCallback> : List<EventBinding<TCallback>>, IBindingList
        where TCallback : Delegate
    {
        public Type CallbackType => typeof(TCallback);

        public void RemoveByBoundObject(GameObject target)
        {
            RemoveAll(binding => binding.IsBoundTo(target));
        }
    }
}
