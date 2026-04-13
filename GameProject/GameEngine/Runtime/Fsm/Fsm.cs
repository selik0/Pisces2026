using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 有限状态机。
    /// <para>
    /// 管理一组 <see cref="FsmState{T}"/>，支持状态切换、黑板数据读写以及每帧 Tick 驱动。
    /// 通常由 <see cref="FsmManager"/> 统一创建和管理，不建议直接实例化。
    /// </para>
    /// </summary>
    /// <typeparam name="T">有限状态机持有者类型</typeparam>
    public sealed class Fsm<T> : IFsm<T> where T : class
    {
        // ── 字段 ─────────────────────────────────────────────────────────────────

        private readonly Dictionary<Type, FsmState<T>> _states   = new Dictionary<Type, FsmState<T>>();
        private readonly Dictionary<string, object>    _dataDict = new Dictionary<string, object>();

        private FsmState<T> _currentState;
        private float       _currentStateTime;
        private bool        _isDestroyed;

        // 状态切换请求（在当前 Tick 末尾执行，避免在 OnUpdate 内直接切换引发问题）
        private Type        _pendingStateType;

        // ── 构造 ─────────────────────────────────────────────────────────────────

        private Fsm() { }

        /// <summary>
        /// 创建有限状态机。
        /// </summary>
        /// <param name="name">有限状态机名称</param>
        /// <param name="owner">有限状态机持有者</param>
        /// <param name="states">有限状态机状态集合（至少一个）</param>
        internal static Fsm<T> Create(string name, T owner, params FsmState<T>[] states)
        {
            if (owner == null)   throw new ArgumentNullException(nameof(owner));
            if (states == null || states.Length == 0)
                throw new ArgumentException("FSM 状态集合不能为空。", nameof(states));

            var fsm = new Fsm<T>
            {
                Name  = name ?? string.Empty,
                Owner = owner
            };

            foreach (var state in states)
            {
                if (state == null) throw new ArgumentNullException(nameof(states), "FSM 状态不能为 null。");
                var type = state.GetType();
                if (fsm._states.ContainsKey(type))
                    throw new ArgumentException($"FSM 已存在类型为 '{type.FullName}' 的状态，不能重复添加。");

                fsm._states[type] = state;
                ((IFsmState<T>)state).OnInit(fsm);
            }

            return fsm;
        }

        // ── IFsm 属性 ────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public string Name { get; private set; }

        /// <inheritdoc/>
        public T Owner { get; private set; }

        /// <inheritdoc/>
        public int StateCount => _states.Count;

        /// <inheritdoc/>
        public bool IsRunning => _currentState != null && !_isDestroyed;

        /// <inheritdoc/>
        public bool IsDestroyed => _isDestroyed;

        /// <inheritdoc/>
        public FsmState<T> CurrentState => _currentState;

        /// <inheritdoc/>
        public float CurrentStateTime => _currentStateTime;

        // ── Start ────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public void Start<TState>() where TState : FsmState<T>
            => Start(typeof(TState));

        /// <inheritdoc/>
        public void Start(Type stateType)
        {
            if (_isDestroyed)
                throw new InvalidOperationException($"[FSM:{Name}] 有限状态机已销毁，无法启动。");
            if (_currentState != null)
                throw new InvalidOperationException($"[FSM:{Name}] 有限状态机已在运行中。");
            if (stateType == null)
                throw new ArgumentNullException(nameof(stateType));

            var state = GetStateInternal(stateType);
            _currentState     = state;
            _currentStateTime = 0f;
            ((IFsmState<T>)state).OnEnter(this);

            Log.Debug($"[FSM:{Name}] 启动，初始状态：{stateType.Name}");
        }

        // ── HasState ─────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public bool HasState<TState>() where TState : FsmState<T>
            => _states.ContainsKey(typeof(TState));

        /// <inheritdoc/>
        public bool HasState(Type stateType)
        {
            if (stateType == null) throw new ArgumentNullException(nameof(stateType));
            return _states.ContainsKey(stateType);
        }

        // ── GetState ─────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public TState GetState<TState>() where TState : FsmState<T>
            => (TState)GetStateInternal(typeof(TState));

        /// <inheritdoc/>
        public FsmState<T> GetState(Type stateType)
        {
            if (stateType == null) throw new ArgumentNullException(nameof(stateType));
            return GetStateInternal(stateType);
        }

        /// <inheritdoc/>
        public FsmState<T>[] GetAllStates()
        {
            var result = new FsmState<T>[_states.Count];
            int i = 0;
            foreach (var kv in _states)
                result[i++] = kv.Value;
            return result;
        }

        // ── 黑板数据 ─────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public TData GetData<TData>(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (_dataDict.TryGetValue(name, out var value))
                return (TData)value;
            return default;
        }

        /// <inheritdoc/>
        public void SetData<TData>(string name, TData data)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            _dataDict[name] = data;
        }

        /// <inheritdoc/>
        public bool HasData(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return _dataDict.ContainsKey(name);
        }

        /// <inheritdoc/>
        public bool RemoveData(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return _dataDict.Remove(name);
        }

        // ── ChangeState ──────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public void ChangeState<TState>() where TState : FsmState<T>
            => ChangeState(typeof(TState));

        /// <inheritdoc/>
        public void ChangeState(Type stateType)
        {
            if (stateType == null) throw new ArgumentNullException(nameof(stateType));
            if (_isDestroyed)
                throw new InvalidOperationException($"[FSM:{Name}] 有限状态机已销毁，无法切换状态。");
            if (_currentState == null)
                throw new InvalidOperationException($"[FSM:{Name}] 有限状态机尚未启动，无法切换状态。");

            // 延迟到本帧 Tick 结束后执行
            _pendingStateType = stateType;
        }

        // ── Tick ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 推进有限状态机逻辑。应在游戏主循环中每帧由 <see cref="FsmManager"/> 统一调用。
        /// </summary>
        internal void Tick(float deltaTime)
        {
            if (_isDestroyed || _currentState == null) return;

            _currentStateTime += deltaTime;
            ((IFsmState<T>)_currentState).OnUpdate(this, deltaTime);

            // 处理挂起的状态切换
            if (_pendingStateType != null)
            {
                DoChangeState(_pendingStateType);
                _pendingStateType = null;
            }
        }

        // ── Destroy ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 销毁有限状态机，依次触发当前状态的 OnLeave(isShutdown=true) 和所有状态的 OnDestroy。
        /// </summary>
        internal void Destroy()
        {
            if (_isDestroyed) return;
            _isDestroyed = true;

            if (_currentState != null)
            {
                ((IFsmState<T>)_currentState).OnLeave(this, isShutdown: true);
                _currentState = null;
            }

            foreach (var state in _states.Values)
                ((IFsmState<T>)state).OnDestroy(this);

            _states.Clear();
            _dataDict.Clear();
            _pendingStateType = null;

            Log.Debug($"[FSM:{Name}] 已销毁");
        }

        // ── 私有辅助 ─────────────────────────────────────────────────────────────

        private FsmState<T> GetStateInternal(Type stateType)
        {
            if (!_states.TryGetValue(stateType, out var state))
                throw new InvalidOperationException(
                    $"[FSM:{Name}] 不存在类型为 '{stateType.FullName}' 的状态。");
            return state;
        }

        private void DoChangeState(Type newStateType)
        {
            var newState = GetStateInternal(newStateType);
            if (ReferenceEquals(newState, _currentState)) return;

            var oldStateName = _currentState.GetType().Name;
            ((IFsmState<T>)_currentState).OnLeave(this, isShutdown: false);

            _currentState     = newState;
            _currentStateTime = 0f;
            ((IFsmState<T>)_currentState).OnEnter(this);

            Log.Debug($"[FSM:{Name}] {oldStateName} → {newStateType.Name}");
        }
    }
}
