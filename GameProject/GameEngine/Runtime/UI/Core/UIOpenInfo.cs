using System;

namespace GameEngine
{
    /// <summary>
    /// UI 打开请求描述，用于同层串行队列。
    /// </summary>
    public sealed class UIOpenInfo
    {
        public UIOpenInfo(Type viewType, UIViewData data = null, UIView ownerWindow = null)
        {
            ViewType = viewType;
            Data = data;
            OwnerWindow = ownerWindow;
        }

        /// <summary>目标界面类型。</summary>
        public Type ViewType { get; }

        /// <summary>打开界面所需的数据。</summary>
        public UIViewData Data { get; }

        /// <summary>打开界面所附属的 Window 界面。</summary>
        public UIView OwnerWindow { get; }
    }
}
