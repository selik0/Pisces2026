using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 将事件订阅与 GameObject 生命周期绑定的便捷方法。
    /// </summary>
    public static class EventSystemExtensions
    {
        public static void Subscribe(this GameObject gameObject, int eventKey, Action callback, bool once = false)
        {
            ValidateGameObject(gameObject);
            EventSystem.Subscribe(eventKey, callback, once, gameObject);
        }

        public static void Subscribe<T1>(this GameObject gameObject, int eventKey, Action<T1> callback, bool once = false)
        {
            ValidateGameObject(gameObject);
            EventSystem.Subscribe(eventKey, callback, once, gameObject);
        }

        public static void Subscribe<T1, T2>(this GameObject gameObject, int eventKey, Action<T1, T2> callback, bool once = false)
        {
            ValidateGameObject(gameObject);
            EventSystem.Subscribe(eventKey, callback, once, gameObject);
        }

        public static void Subscribe<T1, T2, T3>(this GameObject gameObject, int eventKey, Action<T1, T2, T3> callback, bool once = false)
        {
            ValidateGameObject(gameObject);
            EventSystem.Subscribe(eventKey, callback, once, gameObject);
        }

        public static void Unsubscribe(this GameObject gameObject, int eventKey, Action callback)
        {
            if (gameObject != null)
            {
                EventSystem.Unsubscribe(eventKey, callback);
            }
        }

        public static void Unsubscribe<T1>(this GameObject gameObject, int eventKey, Action<T1> callback)
        {
            if (gameObject != null)
            {
                EventSystem.Unsubscribe(eventKey, callback);
            }
        }

        public static void Unsubscribe<T1, T2>(this GameObject gameObject, int eventKey, Action<T1, T2> callback)
        {
            if (gameObject != null)
            {
                EventSystem.Unsubscribe(eventKey, callback);
            }
        }

        public static void Unsubscribe<T1, T2, T3>(this GameObject gameObject, int eventKey, Action<T1, T2, T3> callback)
        {
            if (gameObject != null)
            {
                EventSystem.Unsubscribe(eventKey, callback);
            }
        }

        /// <summary>取消此 GameObject 绑定到全局事件总线的全部事件。</summary>
        public static void UnsubscribeAll(this GameObject gameObject)
        {
            if (gameObject != null)
            {
                EventSystem.UnsubscribeAll(gameObject);
            }
        }

        public static void Subscribe(this GameObject gameObject, EventBus bus, int eventKey, Action callback, bool once = false)
        {
            Validate(gameObject, bus);
            bus.Subscribe(eventKey, callback, once, gameObject);
        }

        public static void Subscribe<T1>(this GameObject gameObject, EventBus bus, int eventKey, Action<T1> callback, bool once = false)
        {
            Validate(gameObject, bus);
            bus.Subscribe(eventKey, callback, once, gameObject);
        }

        public static void Subscribe<T1, T2>(this GameObject gameObject, EventBus bus, int eventKey, Action<T1, T2> callback, bool once = false)
        {
            Validate(gameObject, bus);
            bus.Subscribe(eventKey, callback, once, gameObject);
        }

        public static void Subscribe<T1, T2, T3>(this GameObject gameObject, EventBus bus, int eventKey, Action<T1, T2, T3> callback, bool once = false)
        {
            Validate(gameObject, bus);
            bus.Subscribe(eventKey, callback, once, gameObject);
        }

        /// <summary>取消此 GameObject 绑定到指定事件总线的全部事件。</summary>
        public static void UnsubscribeAll(this GameObject gameObject, EventBus bus)
        {
            if (gameObject != null && bus != null)
            {
                bus.UnsubscribeAll(gameObject);
            }
        }

        private static void Validate(GameObject gameObject, EventBus bus)
        {
            ValidateGameObject(gameObject);
            if (bus == null)
            {
                throw new ArgumentNullException(nameof(bus));
            }
        }

        private static void ValidateGameObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }
        }
    }
}
