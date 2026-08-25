namespace GameEngine
{
    /// <summary>
    /// 强类型 UI 界面基类，约定一个界面对应一个数据类。
    /// 派生类可直接以强类型方式访问 <see cref="Data"/>。
    /// </summary>
    public abstract class UIView<TData> : UIView where TData : UIViewData
    {
        /// <summary>界面数据（强类型）。</summary>
        public new TData Data
        {
            get { return base.Data as TData; }
        }
    }

    /// <summary>
    /// UI 界面基类。
    /// 提供生命周期管理、GameObject 绑定、组件绑定、事件注册/反注册等基础功能。
    /// 子类通过重写 OnXxx 方法实现具体逻辑。
    /// </summary>
    public abstract class UIView : UIBrick
    {
        /// <summary>UI 所属层级。</summary>
        public virtual UILayer Layer { get; } = UILayer.Window;

        /// <summary>
        /// 打开该界面时传入的数据，未传时为 null。
        /// 约定一个界面对应一个数据类，由 <see cref="UIManager"/> 打开界面时写入。
        /// </summary>
        public UIViewData Data { get; internal set; }

        /// <summary>
        /// 当前界面独占的子界面管理器。同一时刻只显示一个 <see cref="UISubView"/>。
        /// 仅顶层界面拥有该管理器。
        /// </summary>
        public ChildUIManager ChildUIManager { get; internal set; }

        /// <summary>处理 ESC 或 Android 返回键等返回操作。</summary>
        public virtual bool OnBack()
        {
            return false;
        }
    }
}
