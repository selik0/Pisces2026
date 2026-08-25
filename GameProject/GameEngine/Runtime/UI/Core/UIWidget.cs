namespace GameEngine
{
    /// <summary>
    /// 可复用 UI 组件基类，例如货币条、货币栏、道具 Item 和道具列表。
    /// Widget 拥有独立生命周期，其父级可以是 UIView 或另一个 UIWidget。
    /// </summary>
    public abstract class UIWidget : UIBrick
    {
        /// <summary>逻辑父级；可以是 UIView、UIWidget，直接通过节点打开时也可以为 null。</summary>
        public UIBrick Parent { get; internal set; }

        /// <summary>所属顶层界面；没有界面父级时为 null。</summary>
        public UIView ParentView
        {
            get
            {
                if (Parent is UIView view)
                {
                    return view;
                }

                return (Parent as UIWidget)?.ParentView;
            }
        }

        /// <summary>直接父 Widget；直接父级不是 Widget 时为 null。</summary>
        public UIWidget ParentWidget => Parent as UIWidget;

        /// <summary>该实例是否由类型打开并在关闭后进入对象池。</summary>
        public bool IsPooled { get; internal set; }
    }
}
