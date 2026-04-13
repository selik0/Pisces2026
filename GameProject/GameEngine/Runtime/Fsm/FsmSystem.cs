namespace GameEngine
{
    /// <summary>
    /// 全局有限状态机系统静态入口，内部持有默认的 <see cref="FsmManager"/> 单例。
    ///
    /// <para>
    /// 需要在游戏主循环中每帧调用 <see cref="Tick"/>，通常放在 GameBootstrap.Update 中。
    /// </para>
    ///
    /// <code>
    /// // ── 游戏启动 Bootstrap ────────────────────────────────────
    /// void Update() => FsmSystem.Tick(Time.deltaTime);
    ///
    /// // ── 创建 FSM ──────────────────────────────────────────────
    /// var fsm = FsmSystem.CreateFsm("GameProcedure", app,
    ///     new LaunchProcedure(),
    ///     new LoadingProcedure(),
    ///     new MainMenuProcedure());
    ///
    /// // ── 启动，进入初始状态 ────────────────────────────────────
    /// fsm.Start&lt;LaunchProcedure&gt;();
    ///
    /// // ── 状态内切换 ────────────────────────────────────────────
    /// // （在 FsmState 子类的 OnUpdate 中调用）
    /// ChangeState&lt;LoadingProcedure&gt;(fsm);
    ///
    /// // ── 查询 FSM ──────────────────────────────────────────────
    /// var fsm = FsmSystem.GetFsm&lt;GameApp&gt;("GameProcedure");
    ///
    /// // ── 销毁 FSM ──────────────────────────────────────────────
    /// FsmSystem.DestroyFsm&lt;GameApp&gt;("GameProcedure");
    ///
    /// // ── Debug 模式 ────────────────────────────────────────────
    /// FsmSystem.DebugMode = true;
    /// </code>
    /// </summary>
    public static class FsmSystem
    {
        private static FsmManager _default;

        /// <summary>全局默认 FsmManager 实例（懒初始化）</summary>
        public static FsmManager Default
        {
            get
            {
                if (_default == null) _default = new FsmManager();
                return _default;
            }
        }

        /// <summary>是否开启 Debug 模式（透传给 Default manager）</summary>
        public static bool DebugMode
        {
            get => Default.DebugMode;
            set => Default.DebugMode = value;
        }

        /// <summary>当前管理的有限状态机数量</summary>
        public static int Count => Default.Count;

        // ── Tick ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 推进所有有限状态机逻辑。应在 MonoBehaviour.Update 中每帧调用。
        /// </summary>
        /// <param name="deltaTime">帧间隔（秒），传入 <c>Time.deltaTime</c></param>
        public static void Tick(float deltaTime)
            => Default.Tick(deltaTime);

        // ── CreateFsm ────────────────────────────────────────────────────────────

        /// <inheritdoc cref="FsmManager.CreateFsm{T}"/>
        public static IFsm<T> CreateFsm<T>(string name, T owner, params FsmState<T>[] states) where T : class
            => Default.CreateFsm(name, owner, states);

        // ── HasFsm ───────────────────────────────────────────────────────────────

        /// <inheritdoc cref="FsmManager.HasFsm{T}"/>
        public static bool HasFsm<T>(string name) where T : class
            => Default.HasFsm<T>(name);

        // ── GetFsm ───────────────────────────────────────────────────────────────

        /// <inheritdoc cref="FsmManager.GetFsm{T}"/>
        public static IFsm<T> GetFsm<T>(string name) where T : class
            => Default.GetFsm<T>(name);

        // ── DestroyFsm ───────────────────────────────────────────────────────────

        /// <inheritdoc cref="FsmManager.DestroyFsm{T}(string)"/>
        public static void DestroyFsm<T>(string name) where T : class
            => Default.DestroyFsm<T>(name);

        /// <inheritdoc cref="FsmManager.DestroyFsm{T}(IFsm{T})"/>
        public static void DestroyFsm<T>(IFsm<T> fsm) where T : class
            => Default.DestroyFsm(fsm);

        // ── DestroyAll ───────────────────────────────────────────────────────────

        /// <inheritdoc cref="FsmManager.DestroyAll"/>
        public static void DestroyAll()
            => Default.DestroyAll();

        // ── 重置 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 完全重置全局有限状态机系统（测试用，游戏运行时慎用）。
        /// </summary>
        public static void Reset()
        {
            _default?.DestroyAll();
            _default = null;
        }
    }
}
