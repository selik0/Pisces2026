using System;
using System.Runtime.ExceptionServices;
using UnityEngine;

namespace GameEngine
{
    /// <summary>UI 逻辑块生命周期状态。</summary>
    public enum UIBrickState
    {
        Uninitialized,
        Creating,
        Created,
        Opening,
        Opened,
        Showing,
        Visible,
        Hiding,
        Closing,
        Destroying,
        Destroyed
    }

    /// <summary>
    /// UI 逻辑块基类。
    /// 标准生命周期为 Create -> Open -> Show -> Hide -> Close -> Destroy。
    /// Open 会自动 Show；Close 和 Destroy 会按需执行前置阶段。
    /// </summary>
    public abstract class UIBrick
    {
        private bool _eventsRegistered;
        private bool _destroyRequested;

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
        public bool IsCreated { get; private set; }
        public bool IsOpened { get; private set; }
        public bool IsVisible { get; private set; }
        public bool IsPaused { get; private set; }
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

            State = UIBrickState.Creating;
            Entity = entity;
            GameObject = entity.gameObject;
            Transform = entity.transform;
            Entity.AddDestroyListener(OnEntityDestroyed);

            try
            {
                OnBind();
                IsCreated = true;
                OnCreate();
                State = UIBrickState.Created;
            }
            catch (Exception exception)
            {
                bool destroyRequested = _destroyRequested;
                RollbackCreate();

                if (destroyRequested)
                {
                    _destroyRequested = true;
                    ProcessDestroyRequest(ref exception);
                }

                Throw(exception);
            }

            ProcessDestroyRequest();
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

            if (State != UIBrickState.Created)
            {
                return;
            }

            State = UIBrickState.Opening;
            IsOpened = true;
            bool opened = false;

            try
            {
                _eventsRegistered = true;
                RegisterEvents();

                if (!_destroyRequested)
                {
                    OnOpen();
                    opened = true;
                }

                if (_destroyRequested)
                {
                    RollbackOpen(opened);
                }
                else
                {
                    State = UIBrickState.Opened;
                    Show();
                }
            }
            catch (Exception exception)
            {
                if (State != UIBrickState.Destroyed)
                {
                    RollbackOpen(opened);
                }

                ProcessDestroyRequest(ref exception);
                Throw(exception);
            }

            ProcessDestroyRequest();
        }

        /// <summary>显示已打开的 UI。</summary>
        public void Show()
        {
            if (State != UIBrickState.Opened || _destroyRequested)
            {
                return;
            }

            State = UIBrickState.Showing;
            IsVisible = true;

            try
            {
                OnShow();
                State = UIBrickState.Visible;
            }
            catch (Exception exception)
            {
                InvokeCleanup(OnHide);
                IsVisible = false;
                IsPaused = false;
                State = UIBrickState.Opened;
                ProcessDestroyRequest(ref exception);
                Throw(exception);
            }

            ProcessDestroyRequest();
        }

        /// <summary>隐藏 UI，但保留本次打开的数据和事件订阅。</summary>
        public void Hide()
        {
            if (State != UIBrickState.Visible)
            {
                return;
            }

            Exception exception = null;
            State = UIBrickState.Hiding;
            InvokeLifecycle(OnHide, ref exception);
            IsVisible = false;
            IsPaused = false;
            State = UIBrickState.Opened;

            ProcessDestroyRequest(ref exception);
            Throw(exception);
        }

        /// <summary>UI 被同层级中的其他界面覆盖时暂停。</summary>
        public void Pause()
        {
            if (State != UIBrickState.Visible || IsPaused || _destroyRequested)
            {
                return;
            }

            IsPaused = true;
            try
            {
                OnPause();
            }
            catch (Exception exception)
            {
                IsPaused = false;
                ProcessDestroyRequest(ref exception);
                Throw(exception);
            }

            ProcessDestroyRequest();
        }

        /// <summary>UI 不再被覆盖时恢复。</summary>
        public void Resume()
        {
            if (State != UIBrickState.Visible || !IsPaused || _destroyRequested)
            {
                return;
            }

            IsPaused = false;
            try
            {
                OnResume();
            }
            catch (Exception exception)
            {
                if (State == UIBrickState.Visible && !_destroyRequested)
                {
                    IsPaused = true;
                }

                ProcessDestroyRequest(ref exception);
                Throw(exception);
            }

            ProcessDestroyRequest();
        }

        /// <summary>关闭 UI。可见时先 Hide，随后执行 OnClose 并注销事件。</summary>
        public void Close()
        {
            if (State != UIBrickState.Opened && State != UIBrickState.Visible)
            {
                return;
            }

            Exception exception = null;
            CloseCore(UIBrickState.Created, ref exception);
            ProcessDestroyRequest(ref exception);
            Throw(exception);
        }

        /// <summary>
        /// 终止生命周期并解除绑定。此操作幂等，销毁后的实例不可再次创建。
        /// GameObject 和预制体资源的所有权由 UI 管理器负责。
        /// </summary>
        public void Destroy()
        {
            if (State == UIBrickState.Destroyed || State == UIBrickState.Destroying)
            {
                return;
            }

            if (!IsStableState())
            {
                _destroyRequested = true;
                return;
            }

            DestroyCore();
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

        protected virtual void OnPause()
        {
        }

        protected virtual void OnResume()
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

        private void CloseCore(UIBrickState completedState, ref Exception exception)
        {
            bool wasVisible = IsVisible;
            State = UIBrickState.Closing;

            if (wasVisible)
            {
                InvokeLifecycle(OnHide, ref exception);
            }

            IsVisible = false;
            IsPaused = false;
            InvokeLifecycle(OnClose, ref exception);
            UnregisterRegisteredEvents(ref exception);
            IsOpened = false;
            State = completedState;
        }

        private void DestroyCore()
        {
            _destroyRequested = false;
            Exception exception = null;
            bool wasOpened = IsOpened;
            State = UIBrickState.Destroying;

            if (wasOpened)
            {
                CloseCore(UIBrickState.Destroying, ref exception);
            }

            if (IsCreated)
            {
                InvokeLifecycle(OnDestroy, ref exception);
            }

            if (IsBound)
            {
                Entity.RemoveDestroyListener(OnEntityDestroyed);
                InvokeLifecycle(OnUnbind, ref exception);
            }

            ClearBinding();
            IsCreated = false;
            IsOpened = false;
            IsVisible = false;
            IsPaused = false;
            State = UIBrickState.Destroyed;
            Throw(exception);
        }

        private void RollbackCreate()
        {
            Exception ignored = null;

            if (IsOpened)
            {
                CloseCore(UIBrickState.Creating, ref ignored);
            }

            if (IsCreated)
            {
                InvokeLifecycle(OnDestroy, ref ignored);
            }

            if (IsBound)
            {
                Entity.RemoveDestroyListener(OnEntityDestroyed);
                InvokeLifecycle(OnUnbind, ref ignored);
            }

            ClearBinding();
            IsCreated = false;
            IsOpened = false;
            IsVisible = false;
            IsPaused = false;
            State = UIBrickState.Uninitialized;
        }

        private void RollbackOpen(bool opened)
        {
            Exception ignored = null;
            bool wasVisible = IsVisible;
            State = UIBrickState.Closing;

            if (wasVisible)
            {
                InvokeLifecycle(OnHide, ref ignored);
            }

            IsVisible = false;
            IsPaused = false;
            if (opened)
            {
                InvokeLifecycle(OnClose, ref ignored);
            }
            UnregisterRegisteredEvents(ref ignored);
            IsOpened = false;
            State = UIBrickState.Created;
        }

        private bool IsStableState()
        {
            return State == UIBrickState.Uninitialized ||
                   State == UIBrickState.Created ||
                   State == UIBrickState.Opened ||
                   State == UIBrickState.Visible;
        }

        private void ProcessDestroyRequest()
        {
            if (_destroyRequested && IsStableState())
            {
                DestroyCore();
            }
        }

        private void ProcessDestroyRequest(ref Exception exception)
        {
            if (!_destroyRequested || !IsStableState())
            {
                return;
            }

            try
            {
                DestroyCore();
            }
            catch (Exception destroyException)
            {
                Capture(destroyException, ref exception);
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
            _destroyRequested = false;
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

        private static void InvokeCleanup(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
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
