using System.Collections;

namespace GameEngine
{
    /// <summary>
    /// 全局协程系统静态入口，内部持有默认的 <see cref="CoroutineScheduler"/> 单例。
    ///
    /// <para>
    /// 需要在游戏主循环中每帧调用 <see cref="Tick"/>，通常放在 GameBootstrap.Update 中。
    /// </para>
    ///
    /// <para><b>支持的 yield 返回值</b></para>
    /// <list type="table">
    ///   <item><term><c>yield return null</c></term><description>等待下一帧</description></item>
    ///   <item><term><c>new WaitForSeconds(n)</c></term><description>等待 n 秒</description></item>
    ///   <item><term><c>new WaitForFrames(n)</c></term><description>等待 n 帧</description></item>
    ///   <item><term><c>new WaitUntil(() => cond)</c></term><description>等待条件为 true</description></item>
    ///   <item><term><c>new WaitWhile(() => cond)</c></term><description>等待条件为 false</description></item>
    ///   <item><term><c>new WaitForCoroutine(handle)</c></term><description>等待另一个协程完成</description></item>
    ///   <item><term><c>IEnumerator</c></term><description>内联执行子协程</description></item>
    ///   <item><term><c>CoroutineHandle</c></term><description>等待已启动的外部协程</description></item>
    /// </list>
    ///
    /// <code>
    /// // ── 游戏启动 Bootstrap ────────────────────────────────
    /// void Update() => CoroutineSystem.Tick(Time.deltaTime);
    ///
    /// // ── 启动协程 ──────────────────────────────────────────
    /// CoroutineSystem.Start(ShowTipRoutine());
    ///
    /// IEnumerator ShowTipRoutine()
    /// {
    ///     ShowTip("任务完成！");
    ///     yield return new WaitForSeconds(2f);
    ///     HideTip();
    /// }
    ///
    /// // ── 嵌套子协程 ────────────────────────────────────────
    /// IEnumerator ParentRoutine()
    /// {
    ///     yield return ChildRoutine();   // inline 等待子协程完成
    ///     Log.Debug("子协程结束");
    /// }
    ///
    /// // ── 等待外部协程 ──────────────────────────────────────
    /// var h = CoroutineSystem.Start(LoadingRoutine());
    /// CoroutineSystem.Start(WaitAndDoSomething(h));
    ///
    /// IEnumerator WaitAndDoSomething(CoroutineHandle loading)
    /// {
    ///     yield return loading;          // 等待 loading 完成
    ///     Log.Debug("加载完毕，继续执行");
    /// }
    ///
    /// // ── 停止协程 ──────────────────────────────────────────
    /// var handle = CoroutineSystem.Start(MyRoutine());
    /// handle.Stop();
    ///
    /// // ── Debug 模式 ────────────────────────────────────────
    /// CoroutineSystem.DebugMode = true;
    /// </code>
    /// </summary>
    public static class CoroutineSystem
    {
        private static CoroutineScheduler _default;

        /// <summary>全局默认 CoroutineScheduler 实例（懒初始化）</summary>
        public static CoroutineScheduler Default
        {
            get
            {
                if (_default == null) _default = new CoroutineScheduler();
                return _default;
            }
        }

        /// <summary>是否开启 Debug 模式（透传给 Default scheduler）</summary>
        public static bool DebugMode
        {
            get => Default.DebugMode;
            set => Default.DebugMode = value;
        }

        // ── Tick ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 推进所有协程一帧。应在 MonoBehaviour.Update 中每帧调用。
        /// </summary>
        /// <param name="deltaTime">帧间隔（秒），传入 <c>Time.deltaTime</c></param>
        public static void Tick(float deltaTime)
            => Default.Tick(deltaTime);

        // ── Start / Stop ─────────────────────────────────────────────────────────

        /// <inheritdoc cref="CoroutineScheduler.Start"/>
        public static CoroutineHandle Start(IEnumerator routine)
            => Default.Start(routine);

        /// <inheritdoc cref="CoroutineScheduler.Stop"/>
        public static void Stop(CoroutineHandle handle)
            => Default.Stop(handle);

        /// <inheritdoc cref="CoroutineScheduler.StopAll"/>
        public static void StopAll()
            => Default.StopAll();

        // ── 重置 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 完全重置全局协程系统（测试用，游戏运行时慎用）。
        /// </summary>
        public static void Reset()
        {
            _default?.StopAll();
            _default = null;
        }
    }
}
