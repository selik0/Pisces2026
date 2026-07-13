using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// UI 界面基类。
    /// 提供生命周期管理、GameObject 绑定、组件绑定、事件注册/反注册等基础功能。
    /// 子类通过重写 OnXxx 方法实现具体逻辑。
    /// </summary>
    public abstract class UIView
    {
        /// <summary>UI 预制体路径</summary>
        public string PrefabPath { get; private set; }

        /// <summary>UI 所属层级</summary>
        public UILayer Layer { get; private set; }

        /// <summary>绑定的 GameObject</summary>
        public GameObject GameObject { get; private set; }

        /// <summary>绑定的 Transform（GameObject.transform 快捷方式）</summary>
        public Transform Transform { get; private set; }

        /// <summary>
        /// 界面是否已打开。由 UI 框架管理，子类只读。
        /// </summary>
        public bool IsOpened { get; private set; }

        /// <summary>
        /// 界面是否已销毁。由 UI 框架管理，子类只读。
        /// </summary>
        public bool IsDestroyed { get; private set; }

        // ── GameObject 绑定 ──

        /// <summary>
        /// 绑定外部传入的 GameObject。通常由 UI 框架在加载预制体后调用。
        /// </summary>
        /// <param name="gameObject">UI 对应的 GameObject 实例</param>
        public void Bind(GameObject gameObject)
        {
            if (gameObject == null)
            {
                Debug.LogError($"[UIView] Bind failed: gameObject is null for {GetType().Name}");
                return;
            }

            if (GameObject != null)
            {
                Debug.LogWarning($"[UIView] {GetType().Name} is already bound to a GameObject, replacing.");
            }

            GameObject = gameObject;
            Transform = gameObject.transform;
            OnBind();
        }

        /// <summary>
        /// 绑定完成后调用。子类可重写以在此阶段收集组件引用。
        /// </summary>
        protected virtual void OnBind()
        {
        }

        // ── 组件绑定接口 ──

        /// <summary>
        /// 从绑定的 GameObject 上获取组件。
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例，未找到返回 null</returns>
        protected T GetComponent<T>() where T : Component
        {
            if (GameObject == null) return null;
            return GameObject.GetComponent<T>();
        }

        /// <summary>
        /// 从绑定的 GameObject 上获取组件，若不存在则添加。
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例</returns>
        protected T GetOrAddComponent<T>() where T : Component
        {
            if (GameObject == null) return null;
            T comp = GameObject.GetComponent<T>();
            if (comp == null) comp = GameObject.AddComponent<T>();
            return comp;
        }

        /// <summary>
        /// 从绑定的 GameObject 的子节点上按路径查找组件。
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="path">子节点路径（相对于 GameObject）</param>
        /// <returns>组件实例，未找到返回 null</returns>
        protected T GetComponentInChildren<T>(string path = null) where T : Component
        {
            if (GameObject == null) return null;

            if (string.IsNullOrEmpty(path))
            {
                return GameObject.GetComponentInChildren<T>();
            }

            Transform child = Transform?.Find(path);
            return child != null ? child.GetComponent<T>() : null;
        }

        /// <summary>
        /// 从绑定的 GameObject 的子节点上按路径获取所有组件。
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件数组</returns>
        protected T[] GetComponentsInChildren<T>() where T : Component
        {
            if (GameObject == null) return System.Array.Empty<T>();
            return GameObject.GetComponentsInChildren<T>();
        }

        // ── 内部生命周期（protected，仅供框架子类调用） ──

        /// <summary>
        /// 内部创建。由 UI 框架调用，完成后调用子类的 OnCreate。
        /// </summary>
        protected void InternalCreate()
        {
            OnCreate();
        }

        /// <summary>
        /// 内部打开。由 UI 框架调用，注册事件、设置 IsOpened 后调用子类的 OnOpen。
        /// </summary>
        /// <param name="args">打开时传入的参数</param>
        protected void InternalOpen(object args)
        {
            if (IsOpened || IsDestroyed) return;

            IsOpened = true;
            RegisterEvents();
            OnOpen(args);
        }

        /// <summary>
        /// 内部显示。由 UI 框架调用，完成后调用子类的 OnShow。
        /// </summary>
        protected void InternalShow()
        {
            if (!IsOpened || IsDestroyed) return;

            OnShow();
        }

        /// <summary>
        /// 内部隐藏。由 UI 框架调用，完成后调用子类的 OnHide。
        /// </summary>
        protected void InternalHide()
        {
            if (!IsOpened || IsDestroyed) return;

            OnHide();
        }

        /// <summary>
        /// 内部关闭。由 UI 框架调用，完成后调用子类的 OnClose，
        /// 并注销事件、重置 IsOpened。
        /// </summary>
        protected void InternalClose()
        {
            if (!IsOpened || IsDestroyed) return;

            OnClose();
            UnregisterEvents();
            IsOpened = false;
        }

        /// <summary>
        /// 内部销毁。由 UI 框架调用，标记为已销毁。
        /// </summary>
        protected void InternalDestroy()
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