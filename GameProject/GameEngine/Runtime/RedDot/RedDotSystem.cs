using System;

namespace GameEngine
{
    /// <summary>
    /// 全局红点系统静态入口，内部持有默认的 <see cref="RedDotTree"/> 单例。
    /// 游戏启动时无需显式初始化，第一次访问时自动创建。
    ///
    /// <para><b>设计概念</b></para>
    /// <list type="bullet">
    ///   <item>用「路径字符串」定位红点节点，<c>'/'</c> 作为层级分隔符。</item>
    ///   <item>叶节点写入计数 (<see cref="SetCount"/>)，父节点自动汇总并通知。</item>
    ///   <item>支持委托 (<see cref="AddListener"/>) 和接口 (<see cref="IRedDotObserver"/>) 两种监听方式。</item>
    /// </list>
    ///
    /// <code>
    /// // ── 设置计数 ──────────────────────────────────
    /// RedDotSystem.SetCount("Main/Mail/Unread", 5);
    /// RedDotSystem.SetCount("Main/Mail/Draft",  2);
    ///
    /// // ── 读取汇总 ──────────────────────────────────
    /// int  count = RedDotSystem.GetCount("Main/Mail");  // = 7
    /// bool show  = RedDotSystem.HasRedDot("Main");      // = true
    ///
    /// // ── 增减计数 ──────────────────────────────────
    /// RedDotSystem.AddCount("Main/Mail/Unread", -1);    // 未读 -1，变为 4
    ///
    /// // ── 委托监听 ──────────────────────────────────
    /// RedDotSystem.AddListener("Main/Mail", (node, count) =>
    ///     mailIcon.SetBadge(count));
    ///
    /// // ── 接口监听 ──────────────────────────────────
    /// // class MailUI : IRedDotObserver
    /// // {
    /// //     public void OnRedDotChanged(RedDotNode node, int newCount) { ... }
    /// // }
    /// RedDotSystem.GetNode("Main/Mail").AddObserver(mailUI);
    ///
    /// // ── Debug 模式 ────────────────────────────────
    /// RedDotSystem.DebugMode = true;
    /// </code>
    /// </summary>
    public static class RedDotSystem
    {
        private static RedDotTree _default;

        /// <summary>全局默认 RedDotTree 实例（懒初始化）</summary>
        public static RedDotTree Default
        {
            get
            {
                if (_default == null) _default = new RedDotTree();
                return _default;
            }
        }

        /// <summary>
        /// 是否开启 Debug 模式（透传给 Default tree）。
        /// 开启后，节点创建、计数变更均会打印调用日志。
        /// </summary>
        public static bool DebugMode
        {
            get => Default.DebugMode;
            set => Default.DebugMode = value;
        }

        // ── 节点访问 ─────────────────────────────────────────────────────────────

        /// <inheritdoc cref="RedDotTree.GetNode"/>
        public static RedDotNode GetNode(string path)
            => Default.GetNode(path);

        /// <inheritdoc cref="RedDotTree.TryGetNode"/>
        public static RedDotNode TryGetNode(string path)
            => Default.TryGetNode(path);

        // ── 计数操作 ─────────────────────────────────────────────────────────────

        /// <inheritdoc cref="RedDotTree.SetCount"/>
        public static void SetCount(string path, int count)
            => Default.SetCount(path, count);

        /// <inheritdoc cref="RedDotTree.AddCount"/>
        public static void AddCount(string path, int delta)
            => Default.AddCount(path, delta);

        /// <inheritdoc cref="RedDotTree.GetCount"/>
        public static int GetCount(string path)
            => Default.GetCount(path);

        /// <inheritdoc cref="RedDotTree.HasRedDot"/>
        public static bool HasRedDot(string path)
            => Default.HasRedDot(path);

        // ── 监听快捷方法 ─────────────────────────────────────────────────────────

        /// <inheritdoc cref="RedDotTree.AddListener"/>
        public static void AddListener(string path, Action<RedDotNode, int> callback)
            => Default.AddListener(path, callback);

        /// <inheritdoc cref="RedDotTree.RemoveListener"/>
        public static void RemoveListener(string path, Action<RedDotNode, int> callback)
            => Default.RemoveListener(path, callback);

        // ── 重置 ─────────────────────────────────────────────────────────────────

        /// <inheritdoc cref="RedDotTree.ResetCounts"/>
        public static void ResetCounts()
            => Default.ResetCounts();

        /// <summary>
        /// 完全重置全局红点系统，丢弃所有节点、计数与监听器。
        /// （测试用，游戏运行时慎用）
        /// </summary>
        public static void Reset()
        {
            _default?.Reset();
            _default = null;
        }

        // ── 调试 ─────────────────────────────────────────────────────────────────

        /// <inheritdoc cref="RedDotTree.DumpTree"/>
        public static string DumpTree()
            => Default.DumpTree();
    }
}
