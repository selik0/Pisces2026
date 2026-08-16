namespace GameEngine
{
    /// <summary>
    /// 框架管理器总入口，集中访问并驱动各运行时系统的默认实例。
    /// </summary>
    public class ManagerHub : MonoSingleton<ManagerHub>
    {
        public CoroutineScheduler Coroutine => CoroutineSystem.Default;

        public EventManager Event => EventSystem.Default;

        public FsmManager Fsm => FsmSystem.Default;

        public RedDotTree RedDot => RedDotSystem.Default;

        public SceneManager Scene => SceneSystem.Default;

        public TimerManager Timer => TimerSystem.Default;

        public UIManager UI => UISystem.Default;

        protected virtual void Update()
        {
            float deltaTime = UnityEngine.Time.deltaTime;
            CoroutineSystem.Tick(deltaTime);
            TimerSystem.Tick(deltaTime);
            FsmSystem.Tick(deltaTime);
            SceneSystem.Tick(deltaTime);
            UISystem.Tick(deltaTime);
        }

        protected override void OnDestroy()
        {
            if (Instance == this)
            {
                UISystem.Reset();
                SceneSystem.Reset();
                FsmSystem.Reset();
                TimerSystem.Reset();
                CoroutineSystem.Reset();
                RedDotSystem.Reset();
                EventSystem.Reset();
            }

            base.OnDestroy();
        }
    }
}
