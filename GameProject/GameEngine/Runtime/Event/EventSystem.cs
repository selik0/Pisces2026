using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 全局事件系统静态入口，内部持有默认的 <see cref="EventBus"/> 单例。
    /// 游戏启动时无需显式初始化，第一次访问时自动创建。
    ///
    /// <code>
    /// // 定义事件参数类型（普通 struct 或 class 均可）
    /// public struct ScoreChangedEvent { public int Score; }
    /// public struct PlayerDiedEvent   { public string Reason; }
    ///
    /// // 订阅
    /// EventSystem.Subscribe&lt;ScoreChangedEvent&gt;(e => RefreshUI(e.Score));
    ///
    /// // 只订阅一次
    /// EventSystem.Subscribe&lt;PlayerDiedEvent&gt;(e => ShowGameOver(), once: true);
    ///
    /// // 绑定 GameObject 生命周期（对象销毁后自动解绑）
    /// EventSystem.Subscribe&lt;ScoreChangedEvent&gt;(e => RefreshUI(e.Score), boundObject: gameObject);
    ///
    /// // 发布
    /// EventSystem.Emit(new ScoreChangedEvent { Score = 100 });
    ///
    /// // 取消订阅
    /// EventSystem.Unsubscribe&lt;ScoreChangedEvent&gt;(myCallback);
    ///
    /// // 开启 Debug 模式
    /// EventSystem.DebugMode = true;
    /// </code>
    /// </summary>
    public static class EventSystem
    {
        private static EventBus _default;

        /// <summary>全局默认 EventBus 实例（懒初始化）</summary>
        public static EventBus Default
        {
            get
            {
                if (_default == null) _default = new EventBus();
                return _default;
            }
        }

        /// <summary>
        /// 是否开启 Debug 模式（透传给 Default bus）。
        /// 开启后所有 Subscribe / Emit / Unsubscribe 均会打印调用日志。
        /// </summary>
        public static bool DebugMode
        {
            get => Default.DebugMode;
            set => Default.DebugMode = value;
        }

        // ── 全局快捷方法（委托给 Default bus）───────────────────────────────────

        /// <inheritdoc cref="EventBus.Subscribe{T}"/>
        public static void Subscribe<T>(Action<T> callback,
                                        bool once = false,
                                        GameObject boundObject = null)
            => Default.Subscribe(callback, once, boundObject);

        /// <inheritdoc cref="EventBus.Unsubscribe{T}"/>
        public static void Unsubscribe<T>(Action<T> callback)
            => Default.Unsubscribe(callback);

        /// <inheritdoc cref="EventBus.UnsubscribeAll(GameObject)"/>
        public static void UnsubscribeAll(GameObject boundObject)
            => Default.UnsubscribeAll(boundObject);

        /// <inheritdoc cref="EventBus.Emit{T}"/>
        public static void Emit<T>(T arg)
            => Default.Emit(arg);

        /// <inheritdoc cref="EventBus.Clear{T}"/>
        public static void Clear<T>()
            => Default.Clear<T>();

        /// <inheritdoc cref="EventBus.ClearAll"/>
        public static void ClearAll()
            => Default.ClearAll();

        /// <summary>
        /// 重置全局 bus（测试用，游戏运行时慎用）。
        /// 会丢弃所有订阅。
        /// </summary>
        public static void Reset()
        {
            _default?.ClearAll();
            _default = null;
        }
    }
}
