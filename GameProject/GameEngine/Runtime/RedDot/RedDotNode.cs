using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 红点树中的单个节点。
    /// <para>
    /// 每个节点维护一个「自身计数」(<see cref="SelfCount"/>) 和
    /// 一个「汇总计数」(<see cref="Count"/>)。<br/>
    /// <see cref="Count"/> = <see cref="SelfCount"/> + 所有子节点 <see cref="Count"/> 之和。<br/>
    /// 当 <see cref="Count"/> 发生变化时，节点会：
    /// <list type="number">
    ///   <item>通知所有已注册的 <see cref="IRedDotObserver"/>；</item>
    ///   <item>向上冒泡，触发父节点重新计算。</item>
    /// </list>
    /// </para>
    ///
    /// <para><b>不应直接构造此类</b>，请通过 <see cref="RedDotTree"/> 或 <see cref="RedDotSystem"/> 获取节点。</para>
    /// </summary>
    public sealed class RedDotNode
    {
        // ── 基本信息 ──────────────────────────────────────────────────────────────

        /// <summary>节点名称（路径中最后一段）</summary>
        public string Name { get; }

        /// <summary>完整路径，例如 "Main/Mail/Unread"</summary>
        public string Path { get; }

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
                if (_selfCount == clamped) return;
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

        private readonly Dictionary<string, RedDotNode> _children
            = new Dictionary<string, RedDotNode>(StringComparer.Ordinal);

        /// <summary>只读子节点字典（key = 子节点名称）</summary>
        public IReadOnlyDictionary<string, RedDotNode> Children => _children;

        // ── 观察者 ───────────────────────────────────────────────────────────────

        private List<IRedDotObserver> _observers;
        private List<Action<RedDotNode, int>> _callbacks;

        // ── 构造 ─────────────────────────────────────────────────────────────────

        internal RedDotNode(string name, string path, RedDotNode parent)
        {
            Name   = name;
            Path   = path;
            Parent = parent;
        }

        // ── 子节点管理 ───────────────────────────────────────────────────────────

        /// <summary>
        /// 获取或创建指定名称的直接子节点（内部使用）。
        /// </summary>
        internal RedDotNode GetOrCreateChild(string childName)
        {
            if (_children.TryGetValue(childName, out var child))
                return child;

            var childPath = Path.Length == 0 ? childName : Path + RedDotTree.PathSeparator + childName;
            child = new RedDotNode(childName, childPath, this);
            _children[childName] = child;
            return child;
        }

        // ── 观察者注册 ───────────────────────────────────────────────────────────

        /// <summary>
        /// 注册接口观察者。重复注册同一实例无效。
        /// </summary>
        public void AddObserver(IRedDotObserver observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            if (_observers == null) _observers = new List<IRedDotObserver>();
            if (!_observers.Contains(observer))
                _observers.Add(observer);
        }

        /// <summary>移除接口观察者。</summary>
        public void RemoveObserver(IRedDotObserver observer)
        {
            _observers?.Remove(observer);
        }

        /// <summary>
        /// 注册委托回调。<br/>
        /// 参数：(触发节点, 新数量)。重复注册同一委托无效。
        /// </summary>
        public void AddListener(Action<RedDotNode, int> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (_callbacks == null) _callbacks = new List<Action<RedDotNode, int>>();
            if (!_callbacks.Contains(callback))
                _callbacks.Add(callback);
        }

        /// <summary>移除委托回调。</summary>
        public void RemoveListener(Action<RedDotNode, int> callback)
        {
            _callbacks?.Remove(callback);
        }

        /// <summary>清除该节点上的所有观察者与委托。</summary>
        public void ClearListeners()
        {
            _observers?.Clear();
            _callbacks?.Clear();
        }

        // ── 内部计算 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 重新计算汇总计数。若结果与旧值不同则通知观察者，并向上冒泡。
        /// </summary>
        private void RecalcCount()
        {
            int newCount = _selfCount;
            foreach (var child in _children.Values)
                newCount += child.Count;

            if (newCount == Count) return;

            Count = newCount;
            NotifyObservers();
            Parent?.OnChildCountChanged();
        }

        /// <summary>子节点 Count 发生变化时由子节点调用。</summary>
        internal void OnChildCountChanged()
        {
            RecalcCount();
        }

        /// <summary>通知所有已注册的观察者与委托。</summary>
        private void NotifyObservers()
        {
            if (_observers != null)
            {
                // 快照遍历，防止回调内修改列表
                var snapshot = _observers.ToArray();
                foreach (var obs in snapshot)
                {
                    try { obs.OnRedDotChanged(this, Count); }
                    catch (Exception ex) { Log.Error($"[RedDot] Observer exception on '{Path}'", ex); }
                }
            }

            if (_callbacks != null)
            {
                var snapshot = _callbacks.ToArray();
                foreach (var cb in snapshot)
                {
                    try { cb(this, Count); }
                    catch (Exception ex) { Log.Error($"[RedDot] Callback exception on '{Path}'", ex); }
                }
            }
        }

        // ── 调试 ─────────────────────────────────────────────────────────────────

        /// <summary>打印当前节点及其子树（用于调试）。</summary>
        public override string ToString() => $"RedDotNode[{Path}] Count={Count} (self={_selfCount})";
    }
}
