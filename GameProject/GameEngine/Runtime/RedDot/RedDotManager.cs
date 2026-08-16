namespace GameEngine
{
    /// <summary>
    /// 全局红点管理器单例，内部持有默认的 <see cref="RedDotTree"/> 实例。
    /// 继承 <see cref="Singleton{T}"/>（实现 <see cref="ILogin"/>），
    /// 登录/登出时重置整棵树，避免跨会话残留。
    ///
    /// <para><b>设计概念</b></para>
    /// <list type="bullet">
    ///   <item>用「int Id 链」定位红点节点，从根到目标依次列出 Id。</item>
    ///   <item>节点自身计数通过 <see cref="RedDotNode.SelfCount"/> 写入，父节点自动汇总。</item>
    ///   <item>只读操作统一通过 <see cref="RedDotManager"/> 访问，无需节点级监听。</item>
    /// </list>
    ///
    /// <code>
    /// // ── 设置计数（自动向上冒泡）──────────────────
    /// RedDotManager.Instance.GetNode(MainId, MailId, UnreadId).SelfCount = 5;
    /// RedDotManager.Instance.GetNode(MainId, MailId, DraftId).SelfCount = 2;
    ///
    /// // ── 读取汇总 ──────────────────────────────────
    /// bool show = RedDotManager.Instance.HasRedDot(MainId); // = true
    /// </code>
    /// </summary>
    public sealed class RedDotManager : Singleton<RedDotManager>, ILogin
    {
        public RedDotManager()
        {
        }

        /// <summary>默认 RedDotTree 实例</summary>
        public RedDotTree Tree { get; } = new RedDotTree();

        // ── 节点访问 ─────────────────────────────────────────────────────────────

        /// <inheritdoc cref="RedDotTree.GetNode"/>
        public RedDotNode GetNode(params int[] idChain) => Tree.GetNode(idChain);

        /// <inheritdoc cref="RedDotTree.TryGetNode"/>
        public RedDotNode TryGetNode(params int[] idChain) => Tree.TryGetNode(idChain);

        // ── 查询 ────────────────────────────────────────────────────────────────

        /// <inheritdoc cref="RedDotTree.HasRedDot"/>
        public bool HasRedDot(params int[] idChain) => Tree.HasRedDot(idChain);

        // ── 重置 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 完全重置红点树，丢弃所有节点与计数。
        /// （测试用，游戏运行时慎用）
        /// </summary>
        public void Reset()
            => Tree.Reset();

        // ── ILogin ───────────────────────────────────────────────────────────────

        /// <summary>登录时重置红点树，保持初始状态。</summary>
        public override void Login()
        {
            Reset();
        }

        /// <summary>登出时重置红点树，避免跨会话残留。</summary>
        public override void Logout()
        {
            Reset();
        }
    }
}
