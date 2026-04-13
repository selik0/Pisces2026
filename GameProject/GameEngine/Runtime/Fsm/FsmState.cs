namespace GameEngine
{
    /// <summary>
    /// 有限状态机状态基类。
    /// <para>
    /// 所有自定义状态均应继承此类，并按需重写生命周期方法。
    /// 通过 <see cref="ChangeState{TState}"/> 切换到其他状态。
    /// </para>
    ///
    /// <code>
    /// // ── 示例：定义一个流程状态 ────────────────────────────────
    /// class LoadingState : FsmState&lt;GameApp&gt;
    /// {
    ///     protected override void OnEnter(IFsm&lt;GameApp&gt; fsm)
    ///     {
    ///         Log.Debug("进入 Loading 状态");
    ///     }
    ///
    ///     protected override void OnUpdate(IFsm&lt;GameApp&gt; fsm, float deltaTime)
    ///     {
    ///         if (loadDone)
    ///             ChangeState&lt;MainMenuState&gt;(fsm);
    ///     }
    ///
    ///     protected override void OnLeave(IFsm&lt;GameApp&gt; fsm, bool isShutdown)
    ///     {
    ///         Log.Debug("离开 Loading 状态");
    ///     }
    /// }
    /// </code>
    /// </summary>
    /// <typeparam name="T">有限状态机持有者类型</typeparam>
    public abstract class FsmState<T> : IFsmState<T> where T : class
    {
        // ── IFsmState 显式实现（内部路由到 protected 虚方法） ────────────────────

        void IFsmState<T>.OnInit(IFsm<T> fsm)    => OnInit(fsm);
        void IFsmState<T>.OnEnter(IFsm<T> fsm)   => OnEnter(fsm);
        void IFsmState<T>.OnUpdate(IFsm<T> fsm, float deltaTime) => OnUpdate(fsm, deltaTime);
        void IFsmState<T>.OnLeave(IFsm<T> fsm, bool isShutdown)  => OnLeave(fsm, isShutdown);
        void IFsmState<T>.OnDestroy(IFsm<T> fsm) => OnDestroy(fsm);

        // ── 生命周期虚方法（子类按需重写）────────────────────────────────────────

        /// <summary>
        /// 有限状态机状态初始化时调用（整个生命周期只调用一次）。
        /// </summary>
        protected virtual void OnInit(IFsm<T> fsm) { }

        /// <summary>
        /// 进入有限状态机状态时调用。
        /// </summary>
        protected virtual void OnEnter(IFsm<T> fsm) { }

        /// <summary>
        /// 有限状态机状态轮询时调用（每帧）。
        /// </summary>
        protected virtual void OnUpdate(IFsm<T> fsm, float deltaTime) { }

        /// <summary>
        /// 离开有限状态机状态时调用。
        /// </summary>
        /// <param name="isShutdown">是否是关闭有限状态机时触发的离开</param>
        protected virtual void OnLeave(IFsm<T> fsm, bool isShutdown) { }

        /// <summary>
        /// 有限状态机状态销毁时调用（整个生命周期只调用一次）。
        /// </summary>
        protected virtual void OnDestroy(IFsm<T> fsm) { }

        // ── 切换状态辅助 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 切换有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要切换到的有限状态机状态类型</typeparam>
        /// <param name="fsm">此状态所属的有限状态机</param>
        protected void ChangeState<TState>(IFsm<T> fsm) where TState : FsmState<T>
            => fsm.ChangeState<TState>();

        /// <summary>
        /// 切换有限状态机状态。
        /// </summary>
        /// <param name="fsm">此状态所属的有限状态机</param>
        /// <param name="stateType">要切换到的有限状态机状态类型</param>
        protected void ChangeState(IFsm<T> fsm, System.Type stateType)
            => fsm.ChangeState(stateType);
    }
}
