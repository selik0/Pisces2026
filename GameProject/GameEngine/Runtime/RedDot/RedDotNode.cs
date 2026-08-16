using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 红点树中的单个节点。
    /// <para>
    /// 每个节点维护一个「自身计数」(<see cref="SelfCount"/>) 和
    /// 一个「汇总计数」(<see cref="Count"/>)。<br/>
    /// <see cref="Count"/> = <see cref="SelfCount"/> + 所有子节点 <see cref="Count"/> 之和。<br/>
    /// 当 <see cref="Count"/> 发生变化时，节点会向上冒泡，触发父节点重新计算。
    /// </para>
    ///
    /// <para><b>不应直接构造此类</b>，请通过 <see cref="RedDotTree"/> 或 <see cref="RedDotManager"/> 获取节点。</para>
    /// </summary>
    public sealed class RedDotNode
    {
        // ── 基本信息 ──────────────────────────────────────────────────────────────

        /// <summary>节点 Id，同一父节点下必须唯一</summary>
        public int Id { get; }

        /// <summary>父节点，根节点的父节点为 null</summary>
        public RedDotNode Parent { get; }

        // ── 计数 ─────────────────────────────────────────────────────────────────

        private int _selfCount;

        /// <summary>
        /// 叶子层面由外部直接设置的计数（不含子节点）。
        /// 设置后会触发 <see cref="Count"/> 重新计算并向上冒泡。
        /// 不允许设置为负数，负数将被裁切为 0。
        /// </summary>
        public int SelfCount
        {
            get => _selfCount;
            set
            {
                var clamped = value < 0 ? 0 : value;
                if (_selfCount == clamped)
                {
                    return;
                }

                _selfCount = clamped;
                RecalcCount();
            }
        }

        /// <summary>
        /// 汇总计数 = <see cref="SelfCount"/> + 所有子节点 <see cref="Count"/> 之和。
        /// 只读，由系统内部维护。
        /// </summary>
        public int Count { get; private set; }

        /// <summary>快捷属性：<see cref="Count"/> > 0 时返回 true</summary>
        public bool HasRedDot => Count > 0;

        // ── 子节点 ───────────────────────────────────────────────────────────────

        private readonly Dictionary<int, RedDotNode> _children = new Dictionary<int, RedDotNode>();

        /// <summary>只读子节点字典（key = 子节点 Id）</summary>
        public IReadOnlyDictionary<int, RedDotNode> Children => _children;

        // ── 构造 ─────────────────────────────────────────────────────────────────

        internal RedDotNode(int id, RedDotNode parent)
        {
            Id = id;
            Parent = parent;
        }

        // ── 子节点管理 ───────────────────────────────────────────────────────────

        /// <summary>
        /// 获取或创建指定 Id 的直接子节点（内部使用）。
        /// </summary>
        internal RedDotNode GetOrCreateChild(int childId)
        {
            if (_children.TryGetValue(childId, out var child))
            {
                return child;
            }

            child = new RedDotNode(childId, this);
            _children[childId] = child;
            return child;
        }

        // ── 内部计算 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 重新计算汇总计数。若结果与旧值不同则向上冒泡触发父节点重算。
        /// </summary>
        private void RecalcCount()
        {
            int newCount = _selfCount;
            foreach (var child in _children.Values)
            {
                newCount += child.Count;
            }

            if (newCount == Count)
            {
                return;
            }

            Count = newCount;
            Parent?.OnChildCountChanged();
        }

        /// <summary>子节点 Count 发生变化时由子节点调用。</summary>
        internal void OnChildCountChanged()
        {
            RecalcCount();
        }

        // ── 调试 ─────────────────────────────────────────────────────────────────

        /// <summary>打印当前节点的 Id 链及计数（用于调试）。</summary>
        public override string ToString()
        {
            var chain = new List<int> { Id };
            var parent = Parent;
            while (parent != null)
            {
                chain.Insert(0, parent.Id);
                parent = parent.Parent;
            }

            return $"RedDotNode[IdChain={string.Join("/", chain)}] Count={Count} (self={_selfCount})";
        }
    }
}
