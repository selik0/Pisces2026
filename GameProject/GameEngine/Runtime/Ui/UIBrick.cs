using System;
using System.Runtime.ExceptionServices;
using UnityEngine;

namespace GameEngine
{
    /// <summary>UI 逻辑块生命周期状态。</summary>
    public enum UIBrickState
    {
        Uninitialized,
        Loading,
        Created,
        Opening,
        Opened,
        Hiding,
        Closing,
        Closed,
        Destroyed
    }

    /// <summary>
    /// UI 逻辑块基类。
    /// 标准生命周期为 Create -> Open -> Show -> Hide -> Close -> Destroy。
    /// 生命周期回调失败不会回滚；执行中的 Close 或 Destroy 会立即终止当前阶段并进入目标流程。
    /// </summary>
    public abstract class UIBrick
    {
        private bool _eventsRegistered;

        /// <summary>UI 预制体路径。</summary>
        public string PrefabPath { get; protected set; }

        /// <summary>绑定的 UIEntity。</summary>
        public UIEntity Entity { get; private set; }

        /// <summary>绑定的 GameObject。</summary>
        public GameObject GameObject { get; private set; }

        /// <summary>绑定的 Transform。</summary>
        public Transform Transform { get; private set; }

        /// <summary>当前生命周期状态。</summary>
        public UIBrickState State { get; private set; } = UIBrickState.Uninitialized;

        public bool IsBound => Entity != null;
        public bool IsVisible { get; private set; }
        public bool IsDestroyed => State == UIBrickState.Destroyed;

        /// <summary>
        /// 绑定 UIEntity 并执行一次性初始化。一个实例只能创建一次。
        /// </summary>
        public void Create(UIEntity entity)
        {
            if (entity == null)
            {
                Debug.LogError($"[UIBrick] Create failed: entity is null for {GetType().Name}.");
                return;
            }

            if (State != UIBrickState.Uninitialized)
            {
                Debug.LogWarning($"[UIBrick] {GetType().Name} cannot be created from state {State}.");
                return;
            }

            Exception exception = null;
            State = UIBrickState.Loading;
            Entity = entity;
            GameObject = entity.gameObject;
            Transform = entity.transform;
            Entity.AddDestroyListener(OnEntityDestroyed);

            InvokeLifecycle(OnBind, ref exception);
            if (State != UIBrickState.Loading)
            {
                Throw(exception);
                return;
            }

            InvokeLifecycle(OnCreate, ref exception);
            if (State == UIBrickState.Loading)
            {
                State = UIBrickState.Created;
            }

            Throw(exception);
        }

        /// <summary>
        /// 打开 UI。注册事件并执行 OnOpen 后自动进入显示阶段。
        /// </summary>
        public void Open()
        {
            if (State == UIBrickState.Opened)
            {
                Show();
                return;
            }

            if (!IsBound || (State != UIBrickState.Created && State != UIBrickState.Closed))
            {
                return;
            }

            Exception exception = null;
            State = UIBrickState.Opening;
            _eventsRegistered = true;
            InvokeLifecycle(RegisterEvents, ref exception);

            if (State == UIBrickState.Opening)
            {
                InvokeLifecycle(OnOpen, ref exception);
            }

            if (State == UIBrickState.Opening)
            {
                State = UIBrickState.Opened;
                Show(ref exception);
            }

            Throw(exception);
        }

        /// <summary>显示已打开的 UI。</summary>
        public void Show()
        {
            Exception exception = null;
            Show(ref exception);
            Throw(exception);
        }

        /// <summary>隐藏 UI，但保留本次打开的数据和事件订阅。</summary>
        public void Hide()
        {
            if (State != UIBrickState.Opened || !IsVisible)
            {
                return;
            }

            Exception exception = null;
            State = UIBrickState.Hiding;
            InvokeLifecycle(OnHide, ref exception);

            if (State == UIBrickState.Hiding)
            {
                IsVisible = false;
                State = UIBrickState.Opened;
            }

            Throw(exception);
        }

        /// <summary>关闭 UI。执行中调用时会立即终止当前阶段并进入关闭流程。</summary>
        public void Close()
        {
            if (State == UIBrickState.Uninitialized ||
                State == UIBrickState.Closing ||
                State == UIBrickState.Closed ||
                State == UIBrickState.Destroyed)
            {
                return;
            }

            Exception exception = null;
            CloseCore(ref exception);
            Throw(exception);
        }

        /// <summary>
        /// 终止生命周期并解除绑定。此操作幂等，销毁后的实例不可再次创建。
        /// GameObject 和预制体资源的所有权由 UI 管理器负责。
        /// </summary>
        public void Destroy()
        {
            if (State == UIBrickState.Destroyed)
            {
                return;
            }

            Exception exception = null;

            if (State != UIBrickState.Uninitialized &&
                State != UIBrickState.Closing &&
                State != UIBrickState.Closed)
            {
                CloseCore(ref exception);
            }

            if (State == UIBrickState.Destroyed)
            {
                Throw(exception);
                return;
            }

            bool wasBound = IsBound;
            State = UIBrickState.Destroyed;

            if (wasBound)
            {
                InvokeLifecycle(OnDestroy, ref exception);
            }

            if (wasBound)
            {
                Entity.RemoveDestroyListener(OnEntityDestroyed);
                InvokeLifecycle(OnUnbind, ref exception);
            }

            ClearBinding();
            IsVisible = false;
            Throw(exception);
        }

        protected virtual void OnBind()
        {
        }

        protected virtual void OnUnbind()
        {
        }

        protected virtual void OnCreate()
        {
        }

        protected virtual void OnOpen()
        {
        }

        protected virtual void OnShow()
        {
        }

        protected virtual void OnHide()
        {
        }

        protected virtual void OnClose()
        {
        }

        protected virtual void OnDestroy()
        {
        }

        protected virtual void RegisterEvents()
        {
        }

        protected virtual void UnregisterEvents()
        {
        }

        /// <summary>处理 ESC 或 Android 返回键等返回操作。</summary>
        public virtual bool OnBack()
        {
            return false;
        }

        private void Show(ref Exception exception)
        {
            if (State != UIBrickState.Opened || IsVisible)
            {
                return;
            }

            IsVisible = true;
            InvokeLifecycle(OnShow, ref exception);
        }

        private void CloseCore(ref Exception exception)
        {
            bool isHiding = State == UIBrickState.Hiding;
            State = UIBrickState.Closing;

            if (IsVisible && !isHiding)
            {
                InvokeLifecycle(OnHide, ref exception);
            }

            if (State == UIBrickState.Destroyed)
            {
                return;
            }

            IsVisible = false;
            InvokeLifecycle(OnClose, ref exception);

            if (State == UIBrickState.Destroyed)
            {
                return;
            }

            UnregisterRegisteredEvents(ref exception);

            if (State == UIBrickState.Closing)
            {
                State = UIBrickState.Closed;
            }
        }

        private void UnregisterRegisteredEvents(ref Exception exception)
        {
            if (!_eventsRegistered)
            {
                return;
            }

            _eventsRegistered = false;
            InvokeLifecycle(UnregisterEvents, ref exception);
        }

        private void ClearBinding()
        {
            Entity = null;
            GameObject = null;
            Transform = null;
            _eventsRegistered = false;
        }

        private static void InvokeLifecycle(Action action, ref Exception exception)
        {
            try
            {
                action();
            }
            catch (Exception lifecycleException)
            {
                Capture(lifecycleException, ref exception);
            }
        }

        private static void Capture(Exception candidate, ref Exception exception)
        {
            if (exception == null)
            {
                exception = candidate;
            }
            else
            {
                Debug.LogException(candidate);
            }
        }

        private static void Throw(Exception exception)
        {
            if (exception != null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
        }

        private void OnEntityDestroyed()
        {
            try
            {
                Destroy();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
