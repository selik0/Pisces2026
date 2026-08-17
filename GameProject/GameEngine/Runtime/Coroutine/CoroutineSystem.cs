using System.Collections;

namespace GameEngine
{
    /// <summary>
    /// 全局协程系统静态入口，内部持有默认的 <see cref="CoroutineManager"/> 单例。
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
    ///   <item><term><c>IEnumerator</c></term><description>内联执行子协程</description></item>
    /// </list>
    ///
    /// <code>
    /// // ── 游戏启动 Bootstrap ────────────────────────────────
    /// void Update() => CoroutineSystem.Tick();
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
    /// // ── 停止协程 ──────────────────────────────────────────
    /// int id = CoroutineSystem.Start(MyRoutine());
    /// CoroutineSystem.Stop(id);
    /// </code>
    /// </summary>
    public static class CoroutineSystem
    {
        /// <summary>全局默认 CoroutineManager 实例。</summary>
        public static CoroutineManager Default => CoroutineManager.Instance;

        // ── Start / Stop ─────────────────────────────────────────────────────────

        /// <inheritdoc cref="CoroutineManager.Start"/>
        public static int Start(IEnumerator routine) => Default.Start(routine);

        /// <inheritdoc cref="CoroutineManager.Stop"/>
        public static void Stop(int id) => Default.Stop(id);

        /// <inheritdoc cref="CoroutineManager.StopAll"/>
        public static void StopAll() => Default.StopAll();

        // ── 重置 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 完全重置全局协程系统（测试用，游戏运行时慎用）。
        /// </summary>
        public static void Reset()
        {
            Default.StopAll();
        }
    }
}
