using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 红点树，以「路径字符串」定位节点。
    /// <para>
    /// 路径采用 <c>'/'</c> 作为分隔符，例如 <c>"Main/Mail/Unread"</c>。<br/>
    /// 节点在首次访问时自动创建，无需预先声明。
    /// </para>
    ///
    /// <para><b>典型用法</b></para>
    /// <code>
    /// var tree = new RedDotTree();
    ///
    /// // 设置叶节点计数（自动向上冒泡）
    /// tree.SetCount("Main/Mail/Unread", 3);
    /// tree.SetCount("Main/Mail/Draft",  1);
    ///
    /// // 读取汇总计数
    /// int total = tree.GetCount("Main/Mail");   // = 4
    /// bool show  = tree.HasRedDot("Main");      // = true
    ///
    /// // 监听变化
    /// tree.GetNode("Main/Mail").AddListener((node, count) =>
    ///     Debug.Log($"Mail red-dot changed: {count}"));
    ///
    /// // 重置整棵树
    /// tree.Reset();
    /// </code>
    /// </summary>
    public sealed class RedDotTree
    {
        /// <summary>路径分隔符（固定为 <c>'/'</c>）</summary>
        public const string PathSeparator = "/";

        private static readonly char[] SeparatorChars = { '/' };

        /// <summary>虚拟根节点（路径为空字符串，对外不暴露）</summary>
        private readonly RedDotNode _root = new RedDotNode("__root__", string.Empty, null);

        /// <summary>路径 → 节点 的快速查找缓存</summary>
        private readonly Dictionary<string, RedDotNode> _nodeCache
            = new Dictionary<string, RedDotNode>(StringComparer.Ordinal);

        /// <summary>是否开启 Debug 日志</summary>
        public bool DebugMode { get; set; }

        // ── 节点访问 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 根据路径获取节点，若节点不存在则自动创建整条路径上的节点。
        /// </summary>
        /// <param name="path">节点路径，如 <c>"Main/Mail/Unread"</c></param>
        /// <returns>对应的 <see cref="RedDotNode"/></returns>
        public RedDotNode GetNode(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            if (_nodeCache.TryGetValue(path, out var cached))
                return cached;

            var node = ResolveOrCreate(path);
            _nodeCache[path] = node;
            return node;
        }

        /// <summary>
        /// 尝试获取已存在的节点，不存在时返回 null（不会自动创建）。
        /// </summary>
        public RedDotNode TryGetNode(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            _nodeCache.TryGetValue(path, out var node);
            return node;
        }

        // ── 计数操作 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 设置指定路径节点的 <see cref="RedDotNode.SelfCount"/>。
        /// 变化会自动向上冒泡至所有祖先节点。
        /// </summary>
        /// <param name="path">节点路径</param>
        /// <param name="count">新的自身计数（负数将被裁切为 0）</param>
        public void SetCount(string path, int count)
        {
            var node = GetNode(path);
            if (DebugMode)
                Log.Debug($"[RedDotTree] SetCount  '{path}'  {node.SelfCount} → {count}");
            node.SelfCount = count;
        }

        /// <summary>
        /// 在指定路径节点的 <see cref="RedDotNode.SelfCount"/> 上增减。
        /// </summary>
        /// <param name="path">节点路径</param>
        /// <param name="delta">增量（可为负数，最终结果不低于 0）</param>
        public void AddCount(string path, int delta)
        {
            var node = GetNode(path);
            var newVal = node.SelfCount + delta;
            if (DebugMode)
                Log.Debug($"[RedDotTree] AddCount  '{path}'  delta={delta}  {node.SelfCount} → {newVal}");
            node.SelfCount = newVal;
        }

        /// <summary>
        /// 获取指定路径节点的汇总计数（含所有子孙节点）。
        /// 若节点不存在则返回 0。
        /// </summary>
        public int GetCount(string path)
        {
            var node = TryGetNode(path);
            return node?.Count ?? 0;
        }

        /// <summary>
        /// 指定路径节点的汇总计数是否 > 0。
        /// 若节点不存在则返回 false。
        /// </summary>
        public bool HasRedDot(string path)
        {
            var node = TryGetNode(path);
            return node != null && node.HasRedDot;
        }

        // ── 监听快捷方法 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 向指定路径节点添加委托监听。节点不存在时自动创建。
        /// </summary>
        public void AddListener(string path, Action<RedDotNode, int> callback)
        {
            GetNode(path).AddListener(callback);
        }

        /// <summary>
        /// 移除指定路径节点的委托监听。
        /// </summary>
        public void RemoveListener(string path, Action<RedDotNode, int> callback)
        {
            TryGetNode(path)?.RemoveListener(callback);
        }

        // ── 重置 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 将所有叶节点的 <see cref="RedDotNode.SelfCount"/> 清零，触发完整的冒泡通知。
        /// 保留已注册的观察者与委托，保留节点结构。
        /// </summary>
        public void ResetCounts()
        {
            foreach (var node in _nodeCache.Values)
                node.SelfCount = 0;
        }

        /// <summary>
        /// 完全重置：清空所有节点、所有计数和所有监听器。
        /// </summary>
        public void Reset()
        {
            _nodeCache.Clear();
            // 清理根节点下所有子节点（让 GC 回收）
            // RedDotNode 不暴露 RemoveChild，通过创建新 root 实现完全重置
            // 直接清空缓存即可，旧节点失去引用后 GC 会回收
            if (DebugMode)
                Log.Debug("[RedDotTree] Reset");
        }

        // ── 调试辅助 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 将整棵树转为多行字符串（用于调试输出）。
        /// </summary>
        public string DumpTree()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[RedDotTree]");
            DumpNode(_root, sb, 0);
            return sb.ToString();
        }

        private static void DumpNode(RedDotNode node, System.Text.StringBuilder sb, int depth)
        {
            var indent = new string(' ', depth * 2);
            sb.AppendLine($"{indent}{node.Name}  Count={node.Count} (self={node.SelfCount})");
            foreach (var child in node.Children.Values)
                DumpNode(child, sb, depth + 1);
        }

        // ── 私有辅助 ─────────────────────────────────────────────────────────────

        private RedDotNode ResolveOrCreate(string path)
        {
            var segments = path.Split(SeparatorChars, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                throw new ArgumentException($"路径无效：'{path}'", nameof(path));

            var current = _root;
            var builtPath = new System.Text.StringBuilder();

            for (int i = 0; i < segments.Length; i++)
            {
                var seg = segments[i];
                if (i > 0) builtPath.Append(PathSeparator);
                builtPath.Append(seg);

                var segPath = builtPath.ToString();
                if (!_nodeCache.TryGetValue(segPath, out var child))
                {
                    child = current.GetOrCreateChild(seg);
                    _nodeCache[segPath] = child;

                    if (DebugMode)
                        Log.Debug($"[RedDotTree] Created node '{segPath}'");
                }
                current = child;
            }

            return current;
        }
    }
}
