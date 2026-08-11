using System;

namespace GameEngine
{
    /// <summary>
    /// UI 打开请求描述，用于同层队列与获取途径跳转。
    /// </summary>
    public sealed class UIOpenInfo
    {
        public UIOpenInfo(Type viewType, UIViewData data = null, bool isJump = false, string source = null)
        {
            ViewType = viewType;
            Data = data;
            IsJump = isJump;
            Source = source;
        }

        /// <summary>目标界面类型。</summary>
        public Type ViewType { get; }

        /// <summary>打开界面所需的数据。</summary>
        public UIViewData Data { get; }

        /// <summary>是否由获取途径跳转触发。</summary>
        public bool IsJump { get; }

        /// <summary>获取途径来源描述。</summary>
        public string Source { get; }
    }
}
