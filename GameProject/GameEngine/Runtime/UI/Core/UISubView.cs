namespace GameEngine
{
    /// <summary>
    /// 全屏界面内部的子界面基类，例如活动中的抽奖、战令、任务或小游戏页面。
    /// 子界面不进入全局层级栈，由父界面的 <see cref="ChildUIManager"/> 独占管理。
    /// </summary>
    public abstract class UISubView : UIBrick
    {
        /// <summary>持有当前子界面的父界面。</summary>
        public UIView ParentView { get; internal set; }

        /// <summary>打开该子界面时传入的数据。</summary>
        public UIViewData Data { get; internal set; }
    }

    /// <summary>强类型子界面基类。</summary>
    public abstract class UISubView<TData> : UISubView where TData : UIViewData
    {
        public new TData Data => base.Data as TData;
    }
}
