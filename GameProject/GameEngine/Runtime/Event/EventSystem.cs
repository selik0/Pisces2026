using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 全局事件系统静态入口，以 int 类型 EventKey 路由，支持零到三个参数。
    /// </summary>
    /// <code>
    /// const int ScoreChanged = 1;
    /// EventSystem.Subscribe&lt;int&gt;(ScoreChanged, score => RefreshUI(score));
    /// EventSystem.Emit(ScoreChanged, 100);
    /// </code>
    public static class EventSystem
    {
        private static EventBus _default;

        /// <summary>全局默认 EventBus 实例。</summary>
        public static EventBus Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new EventBus();
                }

                return _default;
            }
        }

        public static bool DebugMode
        {
            get => Default.DebugMode;
            set => Default.DebugMode = value;
        }

        public static void Subscribe(int eventKey, Action callback, bool once = false, GameObject boundObject = null)
        {
            Default.Subscribe(eventKey, callback, once, boundObject);
        }

        public static void Subscribe<T1>(int eventKey, Action<T1> callback, bool once = false, GameObject boundObject = null)
        {
            Default.Subscribe(eventKey, callback, once, boundObject);
        }

        public static void Subscribe<T1, T2>(int eventKey, Action<T1, T2> callback, bool once = false, GameObject boundObject = null)
        {
            Default.Subscribe(eventKey, callback, once, boundObject);
        }

        public static void Subscribe<T1, T2, T3>(int eventKey, Action<T1, T2, T3> callback, bool once = false, GameObject boundObject = null)
        {
            Default.Subscribe(eventKey, callback, once, boundObject);
        }

        public static void Unsubscribe(int eventKey, Action callback)
        {
            Default.Unsubscribe(eventKey, callback);
        }

        public static void Unsubscribe<T1>(int eventKey, Action<T1> callback)
        {
            Default.Unsubscribe(eventKey, callback);
        }

        public static void Unsubscribe<T1, T2>(int eventKey, Action<T1, T2> callback)
        {
            Default.Unsubscribe(eventKey, callback);
        }

        public static void Unsubscribe<T1, T2, T3>(int eventKey, Action<T1, T2, T3> callback)
        {
            Default.Unsubscribe(eventKey, callback);
        }

        public static void UnsubscribeAll(GameObject boundObject)
        {
            Default.UnsubscribeAll(boundObject);
        }

        public static void Emit(int eventKey)
        {
            Default.Emit(eventKey);
        }

        public static void Emit<T1>(int eventKey, T1 arg1)
        {
            Default.Emit(eventKey, arg1);
        }

        public static void Emit<T1, T2>(int eventKey, T1 arg1, T2 arg2)
        {
            Default.Emit(eventKey, arg1, arg2);
        }

        public static void Emit<T1, T2, T3>(int eventKey, T1 arg1, T2 arg2, T3 arg3)
        {
            Default.Emit(eventKey, arg1, arg2, arg3);
        }

        public static void Clear(int eventKey)
        {
            Default.Clear(eventKey);
        }

        public static void ClearAll()
        {
            Default.ClearAll();
        }

        /// <summary>重置全局事件总线并丢弃全部订阅。</summary>
        public static void Reset()
        {
            _default?.ClearAll();
            _default = null;
        }
    }
}
