using System;

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
        /// <summary>全局默认 EventManager 实例。</summary>
        public static EventManager Default => EventManager.Instance;

        public static bool Subscribe(int eventKey, Action callback)
        {
            return Default.Subscribe(eventKey, callback);
        }

        public static bool Subscribe<T1>(int eventKey, Action<T1> callback)
        {
            return Default.Subscribe(eventKey, callback);
        }

        public static bool Subscribe<T1, T2>(int eventKey, Action<T1, T2> callback)
        {
            return Default.Subscribe(eventKey, callback);
        }

        public static bool Subscribe<T1, T2, T3>(int eventKey, Action<T1, T2, T3> callback)
        {
            return Default.Subscribe(eventKey, callback);
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
    }
}
