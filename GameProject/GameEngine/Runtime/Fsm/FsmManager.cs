using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 有限状态机管理器。
    /// <para>
    /// 统一创建、管理并驱动所有 <see cref="Fsm{T}"/> 实例。
    /// 需要在游戏主循环中每帧调用 <see cref="Tick"/>。
    /// </para>
    /// </summary>
    public sealed class FsmManager
    {
        // 使用非泛型包装接口统一存储不同持有者类型的 FSM
        private readonly Dictionary<string, IFsmBase> _fsms = new Dictionary<string, IFsmBase>();

        /// <summary>当前管理的有限状态机数量</summary>
        public int Count => _fsms.Count;

        /// <summary>是否开启 Debug 日志</summary>
        public bool DebugMode { get; set; }

        // ── Create ───────────────────────────────────────────────────────────────

        /// <summary>
        /// 创建有限状态机（以持有者类型 + 名称作为唯一键）。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型</typeparam>
        /// <param name="name">有限状态机名称（同类型持有者下的唯一标识）</param>
        /// <param name="owner">有限状态机持有者</param>
        /// <param name="states">初始状态集合（至少一个）</param>
        /// <returns>创建好的有限状态机</returns>
        public IFsm<T> CreateFsm<T>(string name, T owner, params FsmState<T>[] states) where T : class
        {
            var key = GetKey<T>(name);
            if (_fsms.ContainsKey(key))
                throw new InvalidOperationException(
                    $"[FsmManager] 已存在名称为 '{name}'、持有者类型为 '{typeof(T).Name}' 的有限状态机。");

            var fsm = Fsm<T>.Create(name, owner, states);
            _fsms[key] = new FsmWrapper<T>(fsm);

            if (DebugMode)
                Log.Debug($"[FsmManager] 创建 FSM  key={key}  states={states.Length}");

            return fsm;
        }

        // ── DestroyFsm ───────────────────────────────────────────────────────────

        /// <summary>
        /// 销毁指定有限状态机。
        /// </summary>
        public void DestroyFsm<T>(string name) where T : class
            => DestroyFsmByKey(GetKey<T>(name));

        /// <summary>
        /// 销毁指定有限状态机（通过实例）。
        /// </summary>
        public void DestroyFsm<T>(IFsm<T> fsm) where T : class
        {
            if (fsm == null) throw new ArgumentNullException(nameof(fsm));
            DestroyFsmByKey(GetKey<T>(fsm.Name));
        }

        private void DestroyFsmByKey(string key)
        {
            if (!_fsms.TryGetValue(key, out var wrapper))
            {
                Log.Warning($"[FsmManager] 未找到 key={key} 的有限状态机，忽略销毁请求。");
                return;
            }

            wrapper.Destroy();
            _fsms.Remove(key);

            if (DebugMode)
                Log.Debug($"[FsmManager] 销毁 FSM  key={key}");
        }

        // ── HasFsm ───────────────────────────────────────────────────────────────

        /// <summary>是否存在指定的有限状态机</summary>
        public bool HasFsm<T>(string name) where T : class
            => _fsms.ContainsKey(GetKey<T>(name));

        // ── GetFsm ───────────────────────────────────────────────────────────────

        /// <summary>
        /// 获取指定的有限状态机。
        /// </summary>
        /// <returns>找到的有限状态机，若不存在则返回 null</returns>
        public IFsm<T> GetFsm<T>(string name) where T : class
        {
            var key = GetKey<T>(name);
            if (_fsms.TryGetValue(key, out var wrapper))
                return ((FsmWrapper<T>)wrapper).Fsm;
            return null;
        }

        // ── Tick ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 推进所有有限状态机逻辑。应在 MonoBehaviour.Update 中每帧调用。
        /// </summary>
        public void Tick(float deltaTime)
        {
            foreach (var wrapper in _fsms.Values)
                wrapper.Tick(deltaTime);
        }

        // ── DestroyAll ───────────────────────────────────────────────────────────

        /// <summary>
        /// 销毁所有有限状态机。
        /// </summary>
        public void DestroyAll()
        {
            foreach (var wrapper in _fsms.Values)
                wrapper.Destroy();
            _fsms.Clear();

            if (DebugMode)
                Log.Debug("[FsmManager] 已销毁所有 FSM");
        }

        // ── 内部辅助 ─────────────────────────────────────────────────────────────

        private static string GetKey<T>(string name)
            => $"{typeof(T).FullName}@{name ?? string.Empty}";

        // 非泛型包装接口，用于统一存储不同 T 的 Fsm<T>
        private interface IFsmBase
        {
            void Tick(float deltaTime);
            void Destroy();
        }

        private sealed class FsmWrapper<T> : IFsmBase where T : class
        {
            public readonly Fsm<T> Fsm;
            public FsmWrapper(Fsm<T> fsm) { Fsm = fsm; }
            void IFsmBase.Tick(float deltaTime) => Fsm.Tick(deltaTime);
            void IFsmBase.Destroy()             => Fsm.Destroy();
        }
    }
}
