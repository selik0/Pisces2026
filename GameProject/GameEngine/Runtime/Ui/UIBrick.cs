using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// UI 逻辑块基类。
    /// 用于绑定 UIEntity，统一管理 UI 的生命周期，并提供 GameObject、Transform、组件访问等快捷能力。
    /// 子类通过重写 OnXxx 方法实现具体 UI 逻辑。
    /// </summary>
    public abstract class UIBrick
    {
        /// <summary>UI 预制体路径</summary>
        public string PrefabPath { get; protected set; }
        
        /// <summary>绑定的 UIEntity</summary>
        public UIEntity Entity { get; protected set; }

        /// <summary>绑定的 GameObject（UIEntity.gameObject 快捷方式）</summary>
        public GameObject GameObject { get; protected set; }

        /// <summary>绑定的 Transform（UIEntity.transform 快捷方式）</summary>
        public Transform Transform { get; protected set; }

        /// <summary>是否已绑定 UIEntity</summary>
        public bool IsBound => Entity != null;

        /// <summary>是否已创建</summary>
        public bool IsCreated { get; protected set; }

        /// <summary>是否已打开</summary>
        public bool IsOpened { get; protected set; }

        /// <summary>是否已显示</summary>
        public bool IsVisible { get; protected set; }

        /// <summary>是否已销毁</summary>
        public bool IsDestroyed { get; protected set; }

        // ── UIEntity 创建 / 销毁 ──

        /// <summary>
        /// 创建 UI 逻辑块并绑定 UIEntity。通常由 UI 框架在加载或实例化 UI 后调用。
        /// </summary>
        /// <param name="entity">UI 根节点上的 UIEntity</param>
        public void Create(UIEntity entity)
        {
            if (entity == null)
            {
                Debug.LogError($"[UIBrick] Create failed: entity is null for {GetType().Name}");
                return;
            }

            if (IsDestroyed)
            {
                Debug.LogWarning($"[UIBrick] {GetType().Name} is destroyed, create ignored.");
                return;
            }

            if (Entity != null)
            {
                Debug.LogWarning($"[UIBrick] {GetType().Name} is already bound to a UIEntity, replacing.");
                Destroy();
                IsDestroyed = false;
            }

            Entity = entity;
            GameObject = entity.gameObject;
            Transform = entity.transform;
            Entity.AddDestroyListener(OnEntityDestroyed);

            OnBind();

            IsCreated = true;
            OnCreate();
        }

        /// <summary>
        /// 销毁 UI 逻辑块并解除当前 UIEntity 绑定。
        /// </summary>
        public void Destroy()
        {
            if (IsOpened)
            {
                Close();
            }

            if (IsCreated && !IsDestroyed)
            {
                OnDestroy();
                IsCreated = false;
                IsDestroyed = true;
            }

            if (Entity != null)
            {
                Entity.RemoveDestroyListener(OnEntityDestroyed);
            }

            OnUnbind();

            Entity = null;
            GameObject = null;
            Transform = null;
        }

        /// <summary>
        /// 绑定完成后调用。子类可在此阶段缓存 UIEntity 生成的组件字段或初始化组件引用。
        /// </summary>
        protected virtual void OnBind()
        {
        }

        /// <summary>
        /// 解除绑定前调用。子类可在此阶段清理与 UIEntity 相关的缓存引用。
        /// </summary>
        protected virtual void OnUnbind()
        {
        }

        // ── 生命周期（由 UI 框架或派生框架类调用） ──

        /// <summary>
        /// 打开。完成后注册事件，并调用子类的 OnOpen。
        /// </summary>
        /// <param name="args">打开时传入的参数</param>
        public void Open(object args = null)
        {
            if (IsOpened || !IsCreated || IsDestroyed) return;

            IsOpened = true;
            RegisterEvents();
            OnOpen(args);
        }

        /// <summary>
        /// 显示。完成后调用子类的 OnShow。
        /// </summary>
        public void Show()
        {
            if (!IsOpened || IsVisible || IsDestroyed) return;

            IsVisible = true;
            OnShow();
        }

        /// <summary>
        /// 隐藏。完成后调用子类的 OnHide。
        /// </summary>
        public void Hide()
        {
            if (!IsOpened || !IsVisible || IsDestroyed) return;

            OnHide();
            IsVisible = false;
        }

        /// <summary>
        /// 关闭。完成后调用子类的 OnClose，并注销事件、重置打开与显示状态。
        /// </summary>
        public void Close()
        {
            if (!IsOpened || IsDestroyed) return;

            if (IsVisible)
            {
                Hide();
            }

            OnClose();
            UnregisterEvents();
            IsOpened = false;
        }

        // ── 可重写生命周期 ──

        /// <summary>
        /// 创建时调用，用于初始化数据。在 Create 完成绑定后调用。
        /// </summary>
        protected virtual void OnCreate()
        {
        }

        /// <summary>
        /// 打开时调用，接收外部传入的参数。
        /// </summary>
        /// <param name="args">打开时传入的参数</param>
        protected virtual void OnOpen(object args)
        {
        }

        /// <summary>
        /// 显示时调用。
        /// </summary>
        protected virtual void OnShow()
        {
        }

        /// <summary>
        /// 隐藏时调用。
        /// </summary>
        protected virtual void OnHide()
        {
        }

        /// <summary>
        /// 关闭时调用，用于清理打开期间的状态。
        /// </summary>
        protected virtual void OnClose()
        {
        }

        /// <summary>
        /// 销毁时调用，用于释放生命周期内持有的资源。在 Destroy 时调用。
        /// </summary>
        protected virtual void OnDestroy()
        {
        }

        // ── 事件注册 / 反注册 ──

        /// <summary>
        /// 注册 UI 事件。在 Open 中 IsOpened 置为 true 之后、OnOpen 之前调用。
        /// </summary>
        protected virtual void RegisterEvents()
        {
        }

        /// <summary>
        /// 注销 UI 事件。在 Close 中 OnClose 之后、IsOpened 置为 false 之前调用。
        /// </summary>
        protected virtual void UnregisterEvents()
        {
        }

        // ── 其他 ──

        /// <summary>
        /// 返回操作处理。例如按 ESC 或 Android 返回键时调用。
        /// </summary>
        /// <returns>true 表示已处理返回操作，false 表示未处理</returns>
        public virtual bool OnBack()
        {
            return false;
        }

        private void OnEntityDestroyed()
        {
            Destroy();
        }
    }
}
