namespace GameEngine
{
    /// <summary>
    /// UI 界面基类。
    /// 提供生命周期管理、事件注册/反注册等基础功能。
    /// 子类通过重写 OnXxx 方法实现具体逻辑。
    /// </summary>
    public abstract class UIView
    {
        /// <summary>UI 预制体路径</summary>
        public string PrefabPath { get; private set; }

        /// <summary>UI 所属层级</summary>
        public UILayer Layer { get; private set; }

        /// <summary>
        /// 界面是否已打开。由 UI 框架管理，子类只读。
        /// </summary>
        public bool IsOpened { get; private set; }

        /// <summary>
        /// 界面是否已销毁。由 UI 框架管理，子类只读。
        /// </summary>
        public bool IsDestroyed { get; private set; }

        /// <summary>
        /// 内部创建。由 UI 框架调用，完成后调用子类的 OnCreate。
        /// </summary>
        public void InternalCreate()
        {
            OnCreate();
        }

        /// <summary>
        /// 内部打开。由 UI 框架调用，注册事件、设置 IsOpened 后调用子类的 OnOpen。
        /// </summary>
        /// <param name="args">打开时传入的参数</param>
        public void InternalOpen(object args)
        {
            if (IsOpened || IsDestroyed) return;

            IsOpened = true;
            RegisterEvents();
            OnOpen(args);
        }

        /// <summary>
        /// 内部显示。由 UI 框架调用，完成后调用子类的 OnShow。
        /// </summary>
        public void InternalShow()
        {
            if (!IsOpened || IsDestroyed) return;

            OnShow();
        }

        /// <summary>
        /// 内部隐藏。由 UI 框架调用，完成后调用子类的 OnHide。
        /// </summary>
        public void InternalHide()
        {
            if (!IsOpened || IsDestroyed) return;

            OnHide();
        }

        /// <summary>
        /// 内部关闭。由 UI 框架调用，完成后调用子类的 OnClose，
        /// 并注销事件、重置 IsOpened。
        /// </summary>
        public void InternalClose()
        {
            if (!IsOpened || IsDestroyed) return;

            OnClose();
            UnregisterEvents();
            IsOpened = false;
        }

        /// <summary>
        /// 内部销毁。由 UI 框架调用，标记为已销毁。
        /// </summary>
        public void InternalDestroy()
        {
            if (IsDestroyed) return;

            if (IsOpened)
            {
                InternalClose();
            }

            IsDestroyed = true;
        }

        // ── 可重写生命周期 ──

        /// <summary>
        /// 界面创建时调用，用于初始化数据和组件。
        /// 在 InternalCreate 中调用。
        /// </summary>
        protected virtual void OnCreate()
        {
        }

        /// <summary>
        /// 界面打开时调用，接收外部传入的参数。
        /// 在 InternalOpen 中注册事件后调用。
        /// </summary>
        /// <param name="args">打开时传入的参数</param>
        protected virtual void OnOpen(object args)
        {
        }

        /// <summary>
        /// 界面显示时调用。
        /// 在 InternalShow 中调用。
        /// </summary>
        protected virtual void OnShow()
        {
        }

        /// <summary>
        /// 界面隐藏时调用。
        /// 在 InternalHide 中调用。
        /// </summary>
        protected virtual void OnHide()
        {
        }

        /// <summary>
        /// 界面关闭时调用，用于清理资源。
        /// 在 InternalClose 中调用（在注销事件之前）。
        /// </summary>
        protected virtual void OnClose()
        {
        }

        // ── 事件注册 / 反注册 ──

        /// <summary>
        /// 注册 UI 事件。在 InternalOpen 中 IsOpened 置为 true 之后、OnOpen 之前调用。
        /// 子类重写以注册按钮点击等事件监听。
        /// </summary>
        protected virtual void RegisterEvents()
        {
        }

        /// <summary>
        /// 注销 UI 事件。在 InternalClose 中 OnClose 之后、IsOpened 置为 false 之前调用。
        /// 子类重写以注销所有事件监听。
        /// </summary>
        protected virtual void UnregisterEvents()
        {
        }

        // ── 其他 ──

        /// <summary>
        /// 返回操作处理。例如按 ESC 或 Android 返回键时调用。
        /// </summary>
        /// <returns>true 表示已处理返回操作，false 表示未处理（由上层继续处理）</returns>
        public virtual bool OnBack()
        {
            return false;
        }
    }
}