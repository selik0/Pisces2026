using System;
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
    /// 标准生命周期为 Create -> Open -> Show -> Hide -> Close，实体销毁后执行 OnDestroy。
    /// 生命周期回调失败不会回滚；生命周期方法仅在对应状态下执行。
    /// </summary>
    public abstract class UIBrick
    {
        private bool _pendingEntityDestroy;

        /// <summary>UI 预制体路径。</summary>
        public virtual string PrefabPath { get; }

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
                Log.Error($"[UIBrick] Create failed: entity is null for {GetType().Name}.");
                return;
            }

            if (State != UIBrickState.Uninitialized)
            {
                Log.Warning($"[UIBrick] {GetType().Name} cannot be created from state {State}.");
                return;
            }

            State = UIBrickState.Loading;
            Entity = entity;
            GameObject = entity.gameObject;
            Transform = entity.transform;
            Entity.AddDestroyListener(OnEntityDestroyed);

            try
            {
                OnBind();
            }
            catch (Exception exception)
            {
                Log.Error($"[UIBrick] {GetType().Name}.OnBind failed.", exception);
            }

            try
            {
                OnCreate();
            }
            catch (Exception exception)
            {
                Log.Error($"[UIBrick] {GetType().Name}.OnCreate failed.", exception);
            }

            try
            {
                RegisterEvents();
            }
            catch (Exception exception)
            {
                Log.Error($"[UIBrick] {GetType().Name}.RegisterEvents failed.", exception);
            }

            State = UIBrickState.Created;
            ProcessPendingEntityDestroy();
        }

        /// <summary>
        /// 打开已创建的 UI。
        /// </summary>
        public void Open()
        {
            if (State != UIBrickState.Created)
            {
                Log.Error($"[UIBrick] {GetType().Name} cannot be opened from state {State}.");
                return;
            }

            State = UIBrickState.Opening;
            try
            {
                OnOpen();
            }
            catch (Exception exception)
            {
                Log.Error($"[UIBrick] {GetType().Name}.OnOpen failed.", exception);
            }

            Show();
        }

        /// <summary>显示已打开的 UI。</summary>
        public void Show()
        {
            if (State != UIBrickState.Opening && State != UIBrickState.Hiding)
            {
                Log.Error($"[UIBrick] {GetType().Name} cannot be shown from state {State}.");
                return;
            }

            IsVisible = true;
            if (GameObject != null)
            {
                GameObject.SetActive(true);
            }

            try
            {
                OnShow();
            }
            catch (Exception exception)
            {
                Log.Error($"[UIBrick] {GetType().Name}.OnShow failed.", exception);
            }

            State = UIBrickState.Opened;
            ProcessPendingEntityDestroy();
        }

        /// <summary>隐藏 UI，但保留本次打开的数据和事件订阅。</summary>
        public void Hide()
        {
            if (State != UIBrickState.Opened)
            {
                Log.Error($"[UIBrick] {GetType().Name} cannot be hidden from state {State}.");
                return;
            }

            State = UIBrickState.Hiding;
            try
            {
                OnHide();
            }
            catch (Exception exception)
            {
                Log.Error($"[UIBrick] {GetType().Name}.OnHide failed.", exception);
            }

            IsVisible = false;
            if (GameObject != null)
            {
                GameObject.SetActive(false);
            }
        }

        /// <summary>关闭已创建、已打开或正在隐藏的 UI。</summary>
        public void Close()
        {
            if (State != UIBrickState.Created &&
                State != UIBrickState.Opened &&
                State != UIBrickState.Hiding)
            {
                Log.Error($"[UIBrick] {GetType().Name} cannot be closed from state {State}.");
                return;
            }

            if (State == UIBrickState.Opened)
            {
                Hide();
            }
            State = UIBrickState.Closing;
            try
            {
                UnregisterEvents();
            }
            catch (Exception exception)
            {
                Log.Error($"[UIBrick] {GetType().Name}.UnregisterEvents failed.", exception);
            }

            try
            {
                OnClose();
            }
            catch (Exception exception)
            {
                Log.Error($"[UIBrick] {GetType().Name}.OnClose failed.", exception);
            }

            State = UIBrickState.Closed;
            ProcessPendingEntityDestroy();
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

        private void OnEntityDestroyed()
        {
            _pendingEntityDestroy = true;
            ProcessPendingEntityDestroy();
        }

        private void ProcessPendingEntityDestroy()
        {
            if (!_pendingEntityDestroy ||
                (State != UIBrickState.Created &&
                 State != UIBrickState.Opened &&
                 State != UIBrickState.Closed))
            {
                return;
            }

            if (State == UIBrickState.Created || State == UIBrickState.Opened)
            {
                Close();
                return;
            }

            DestroyCore();
        }

        private void DestroyCore()
        {
            State = UIBrickState.Destroyed;

            try
            {
                OnUnbind();
            }
            catch (Exception exception)
            {
                Log.Error($"[UIBrick] {GetType().Name}.OnUnbind failed.", exception);
            }

            try
            {
                OnDestroy();
            }
            catch (Exception exception)
            {
                Log.Error($"[UIBrick] {GetType().Name}.OnDestroy failed.", exception);
            }

            ClearBinding();
        }

        private void ClearBinding()
        {
            if (Entity != null)
            {
                Entity.RemoveDestroyListener(OnEntityDestroyed);
            }
            Entity = null;
            GameObject = null;
            Transform = null;
        }
    }
}
