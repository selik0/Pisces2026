using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 事件绑定列表的非泛型接口，用于按 GameObject 跨 EventKey 解绑。
    /// </summary>
    internal interface IBindingList
    {
        int Count { get; }

        Type CallbackType { get; }

        void RemoveByBoundObject(GameObject target);
    }
}
