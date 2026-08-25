using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 单个 UI 层级：已打开界面的视图栈，以及待打开的同层队列。
    /// </summary>
    public sealed class UILayerStack
    {
        private readonly List<UIView> _views = new List<UIView>();
        private readonly Queue<UIOpenInfo> _pendingOpens = new Queue<UIOpenInfo>();

        public UILayerStack(UILayer layer)
        {
            Layer = layer;
        }

        /// <summary>所属层级。</summary>
        public UILayer Layer { get; }

        /// <summary>当前打开的界面数量。</summary>
        public int ViewCount => _views.Count;

        /// <summary>该层是否没有任何已打开的界面。</summary>
        public bool IsEmpty => _views.Count == 0;

        /// <summary>待打开的同层队列数量。</summary>
        public int PendingCount => _pendingOpens.Count;

        /// <summary>该层最上层界面，空层返回 null。</summary>
        public UIView Top => _views.Count > 0 ? _views[_views.Count - 1] : null;

        public UIView GetView(int index) => _views[index];

        public UIView[] GetAllViews() => _views.ToArray();

        public void PushView(UIView view) => _views.Add(view);

        public bool RemoveView(UIView view) => _views.Remove(view);

        public void EnqueuePending(UIOpenInfo info) => _pendingOpens.Enqueue(info);

        public UIOpenInfo DequeuePending() => _pendingOpens.Dequeue();

        public void ClearPending() => _pendingOpens.Clear();
    }
}
