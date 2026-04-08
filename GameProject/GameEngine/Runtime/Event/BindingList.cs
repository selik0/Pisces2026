using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 泛型绑定列表，持有同一事件键下的所有 <see cref="EventBinding{T}"/>，
    /// 同时实现 <see cref="IBindingList"/> 接口以支持跨类型的 GameObject 批量解绑。
    /// </summary>
    internal sealed class BindingList<T> : List<EventBinding<T>>, IBindingList
    {
        /// <inheritdoc/>
        public void RemoveByBoundObject(GameObject target)
        {
            RemoveAll(b => b.IsBoundTo(target));
        }
    }
}
