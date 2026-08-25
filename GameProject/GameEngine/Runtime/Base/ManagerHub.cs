using System;
using GameNative;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 框架管理器总入口，集中访问并驱动各运行时系统的默认实例。
    /// </summary>
    public class ManagerHub : MonoSingleton<ManagerHub>, ILogin
    {
        private ILogin[] _lifecycleManagers;
        private bool _initialized;

        /// <summary>框架主循环是否已经初始化。</summary>
        public bool IsInitialized => _initialized;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            Initialize();
        }

        /// <summary>初始化日志和所有需要统一管理的框架实例。重复调用安全。</summary>
        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            Log.Initialize();
            NativeLog.SetService(new GameNativeLogService());

            _lifecycleManagers = new ILogin[]
            {
                CoroutineManager.Instance,
                TimerManager.Instance,
                EventManager.Instance,
                FsmManager.Instance,
                SceneManager.Instance,
                UIManager.Instance,
                UIJumpManager.Instance,
                UIWidgetManager.Instance,
                AssetManager.Instance,
                RedDotManager.Instance,
                AudioManager.Instance
            };

            _initialized = true;
            Log.Debug("[ManagerHub] 框架主循环初始化完成。");
        }

        protected virtual void Update()
        {
            if (!_initialized)
            {
                return;
            }

            TickSafely("AssetManager", () => AssetManager.Instance.Tick(Time.unscaledDeltaTime));
            TickSafely("CoroutineManager", CoroutineManager.Instance.Tick);
            TickSafely("TimerManager", TimerManager.Instance.Tick);
            TickSafely("FsmManager", () => FsmManager.Instance.Tick(Time.deltaTime));
            TickSafely("SceneManager", () => SceneManager.Instance.Tick(Time.deltaTime));
            TickSafely("UIManager", () => UIManager.Instance.Tick(Time.deltaTime));
            TickSafely("UIWidgetManager", () => UIWidgetManager.Instance.Tick(Time.unscaledDeltaTime));
            TickSafely("AudioManager", () => AudioManager.Instance.Tick(Time.unscaledDeltaTime));
        }

        /// <summary>进入一次游戏会话，并清理上一次会话可能残留的状态。</summary>
        public void Login()
        {
            Initialize();
            InvokeLifecycle(login: true);
        }

        /// <summary>退出当前游戏会话，按初始化逆序清理各管理器。</summary>
        public void Logout()
        {
            if (!_initialized)
            {
                return;
            }

            InvokeLifecycle(login: false);
        }

        protected override void OnDestroy()
        {
            if (_initialized)
            {
                Logout();
                _initialized = false;
                _lifecycleManagers = null;
                Log.Debug("[ManagerHub] 框架主循环已关闭。");
                NativeLog.ClearService();
                Log.Shutdown();
            }

            base.OnDestroy();
        }

        private void InvokeLifecycle(bool login)
        {
            int start = login ? 0 : _lifecycleManagers.Length - 1;
            int end = login ? _lifecycleManagers.Length : -1;
            int step = login ? 1 : -1;

            for (int i = start; i != end; i += step)
            {
                ILogin manager = _lifecycleManagers[i];
                try
                {
                    if (login)
                    {
                        manager.Login();
                    }
                    else
                    {
                        manager.Logout();
                    }
                }
                catch (Exception exception)
                {
                    string operation = login ? "Login" : "Logout";
                    Log.Error($"[ManagerHub] {manager.GetType().Name}.{operation} 异常。", exception);
                }
            }
        }

        private static void TickSafely(string managerName, Action tick)
        {
            try
            {
                tick();
            }
            catch (Exception exception)
            {
                Log.Error($"[ManagerHub] {managerName}.Tick 异常。", exception);
            }
        }
    }
}
