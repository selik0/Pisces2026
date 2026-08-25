using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// UI 跳转管理器。负责通过获取途径等业务入口打开界面，并独立记录仍处于打开状态的跳转链深度。
    /// 实际界面创建、分层和关闭仍由 <see cref="UIManager"/> 负责。
    /// </summary>
    public sealed class UIJumpManager : Singleton<UIJumpManager>, ILogin
    {
        private readonly List<UIView> _jumpViews = new List<UIView>();

        public UIJumpManager()
        {
            UIManager.Instance.ViewClosed += OnViewClosed;
        }

        /// <summary>跳转深度上限。达到上限后跳转仍会执行，但会触发 <see cref="OnJumpDepthLimitReached"/>。</summary>
        public int MaxJumpDepth { get; set; } = 2;

        /// <summary>当前仍处于打开状态的跳转界面数量。</summary>
        public int CurrentJumpDepth => _jumpViews.Count;

        /// <summary>达到跳转深度上限时触发，业务可据此显示提示。</summary>
        public event Action OnJumpDepthLimitReached;

        /// <summary>
        /// 显式指定界面类型执行跳转。ownerWindow 非空时，目标界面附属于该 Window；
        /// 适用于从该 Window 的弹窗跳转到另一个 Window 时，让原弹窗随来源 Window 隐藏。
        /// </summary>
        public TView Jump<TView>(UIViewData data = null, UIView ownerWindow = null) where TView : UIView, new()
        {
            TView view = UIManager.Instance.OpenView<TView>(data, ownerWindow);
            if (view == null)
            {
                return null;
            }

            _jumpViews.Add(view);
            if (CurrentJumpDepth >= MaxJumpDepth)
            {
                try
                {
                    OnJumpDepthLimitReached?.Invoke();
                }
                catch (Exception exception)
                {
                    Log.Error("[UIJumpManager] OnJumpDepthLimitReached 回调异常。", exception);
                }
            }

            return view;
        }

        /// <summary>清空跳转记录，不负责关闭界面。</summary>
        public void Clear()
        {
            _jumpViews.Clear();
        }

        public void Login()
        {
            Clear();
        }

        public void Logout()
        {
            Clear();
        }

        private void OnViewClosed(UIView view)
        {
            _jumpViews.Remove(view);
        }
    }
}
