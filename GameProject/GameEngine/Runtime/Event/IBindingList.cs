using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 非泛型内部接口，用于 <see cref="EventBus"/> 对任意 <c>List&lt;EventBinding&lt;T&gt;&gt;</c>
    /// 执行跨类型操作（例如按 GameObject 批量解绑），规避 C# 泛型擦除问题。
    /// </summary>
    internal interface IBindingList
    {
        /// <summary>当前绑定数量</summary>
        int Count { get; }

        /// <summary>移除所有绑定了指定 <paramref name="target"/> 的项</summary>
        void RemoveByBoundObject(GameObject target);
    }
}
