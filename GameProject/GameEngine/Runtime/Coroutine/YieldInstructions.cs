using System;

namespace GameEngine
{
    // ── WaitForSeconds ────────────────────────────────────────────────────────

    /// <summary>
    /// 等待指定秒数后继续。
    /// <code>
    /// yield return new WaitForSeconds(2f);
    /// </code>
    /// </summary>
    public sealed class WaitForSeconds : IYieldInstruction
    {
        private float _remaining;

        /// <param name="seconds">等待时间（秒），≥ 0</param>
        public WaitForSeconds(float seconds)
        {
            _remaining = seconds < 0f ? 0f : seconds;
        }

        public bool IsCompleted => _remaining <= 0f;

        public void Tick()
        {
            if (_remaining > 0f)
            {
                _remaining -= UnityEngine.Time.deltaTime;
            }
        }
    }

    // ── WaitForFrames ─────────────────────────────────────────────────────────

    /// <summary>
    /// 等待指定帧数后继续。
    /// <code>
    /// yield return new WaitForFrames(3);
    /// </code>
    /// </summary>
    public sealed class WaitForFrames : IYieldInstruction
    {
        private int _remaining;

        /// <param name="frames">等待帧数，≥ 1</param>
        public WaitForFrames(int frames)
        {
            _remaining = frames < 1 ? 1 : frames;
        }

        public bool IsCompleted => _remaining <= 0;

        public void Tick()
        {
            if (_remaining > 0)
            {
                _remaining--;
            }
        }
    }

    // ── WaitUntil ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 等待直到指定条件为 true 后继续。
    /// <code>
    /// yield return new WaitUntil(() => hp &lt;= 0);
    /// </code>
    /// </summary>
    public sealed class WaitUntil : IYieldInstruction
    {
        private readonly Func<bool> _predicate;

        public WaitUntil(Func<bool> predicate)
        {
            if (predicate == null)
            {
                Log.Error("WaitUntil 创建失败：predicate 为 null，指令将立即完成。");
                _predicate = () => true;
                return;
            }

            _predicate = predicate;
        }

        public bool IsCompleted => _predicate();

        public void Tick() { /* 无状态，每帧直接检查条件 */ }
    }

    // ── WaitWhile ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 等待直到指定条件为 false 后继续（与 <see cref="WaitUntil"/> 相反）。
    /// <code>
    /// yield return new WaitWhile(() => isLoading);
    /// </code>
    /// </summary>
    public sealed class WaitWhile : IYieldInstruction
    {
        private readonly Func<bool> _predicate;

        public WaitWhile(Func<bool> predicate)
        {
            if (predicate == null)
            {
                Log.Error("WaitWhile 创建失败：predicate 为 null，指令将立即完成。");
                _predicate = () => false;
                return;
            }

            _predicate = predicate;
        }

        public bool IsCompleted => !_predicate();

        public void Tick() { }
    }

    // ── NextFrame ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 跳过当前帧，下一帧继续（等价于 <c>yield return null</c>）。
    /// <code>
    /// yield return NextFrame.Instance;
    /// </code>
    /// </summary>
    public sealed class NextFrame : IYieldInstruction
    {
        /// <summary>单例，避免每次分配</summary>
        public static readonly NextFrame Instance = new NextFrame();

        private bool _ticked;

        // 注意：每次使用需 new 一个，或由调度器重置；单例版本由调度器特殊处理
        public bool IsCompleted => _ticked;

        public void Tick() { _ticked = true; }
    }
}
