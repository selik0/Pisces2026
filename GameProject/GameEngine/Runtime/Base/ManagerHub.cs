namespace GameEngine
{
    /// <summary>
    /// 框架管理器总入口，集中访问并驱动各运行时系统的默认实例。
    /// </summary>
    public class ManagerHub : MonoSingleton<ManagerHub>
    {
        public CoroutineManager Coroutine => CoroutineSystem.Default;

        public EventManager Event => EventSystem.Default;

        public FsmManager Fsm => FsmSystem.Default;

        public RedDotManager RedDot => RedDotManager.Instance;

        public SceneManager Scene => SceneSystem.Default;

        public TimerManager Timer => TimerSystem.Default;

        public UIManager UI => UISystem.Default;

        protected virtual void Update()
        {
        }

        protected override void OnDestroy()
        {
            if (Instance == this)
            {
                
            }

            base.OnDestroy();
        }
    }
}
