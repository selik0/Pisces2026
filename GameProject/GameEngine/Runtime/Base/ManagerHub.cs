namespace GameEngine
{
    /// <summary>
    /// 框架管理器总入口，集中访问并驱动各运行时系统的默认实例。
    /// </summary>
    public class ManagerHub : MonoSingleton<ManagerHub>
    {
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
