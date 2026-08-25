using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 保存一组注册的事件，并支持一次性取消全部事件。
    /// </summary>
    public sealed class EventGroup : IDisposable
    {
        private readonly EventManager _eventBus;
        private readonly List<Action> _unregisterActions = new List<Action>();

        public EventGroup()
        {
            _eventBus = EventManager.Instance;
        }

        public void Subscribe(int eventKey, Action callback)
        {
            if (_eventBus.Subscribe(eventKey, callback))
            {
                _unregisterActions.Add(() => _eventBus.Unsubscribe(eventKey, callback));
            }
        }

        public void Subscribe<T1>(int eventKey, Action<T1> callback)
        {
            if (_eventBus.Subscribe(eventKey, callback))
            {
                _unregisterActions.Add(() => _eventBus.Unsubscribe(eventKey, callback));
            }
        }

        public void Subscribe<T1, T2>(int eventKey, Action<T1, T2> callback)
        {
            if (_eventBus.Subscribe(eventKey, callback))
            {
                _unregisterActions.Add(() => _eventBus.Unsubscribe(eventKey, callback));
            }
        }

        public void Subscribe<T1, T2, T3>(int eventKey, Action<T1, T2, T3> callback)
        {
            if (_eventBus.Subscribe(eventKey, callback))
            {
                _unregisterActions.Add(() => _eventBus.Unsubscribe(eventKey, callback));
            }
        }

        public void UnsubscribeAll()
        {
            for (int i = _unregisterActions.Count - 1; i >= 0; i--)
            {
                _unregisterActions[i]();
            }

            _unregisterActions.Clear();
        }

        public void Dispose()
        {
            UnsubscribeAll();
        }
    }
}
