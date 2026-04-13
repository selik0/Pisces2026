namespace GameEngine
{
    /// <summary>
    /// 有限状态机状态接口。
    /// </summary>
    /// <typeparam name="T">有限状态机持有者类型</typeparam>
    public interface IFsmState<T> where T : class
    {
        /// <summary>
        /// 有限状态机状态初始化时调用。
        /// </summary>
        /// <param name="fsm">此状态所属的有限状态机</param>
        void OnInit(IFsm<T> fsm);

        /// <summary>
        /// 进入有限状态机状态时调用。
        /// </summary>
        /// <param name="fsm">此状态所属的有限状态机</param>
        void OnEnter(IFsm<T> fsm);

        /// <summary>
        /// 有限状态机状态轮询时调用。
        /// </summary>
        /// <param name="fsm">此状态所属的有限状态机</param>
        /// <param name="deltaTime">逻辑流逝时间（秒）</param>
        void OnUpdate(IFsm<T> fsm, float deltaTime);

        /// <summary>
        /// 离开有限状态机状态时调用。
        /// </summary>
        /// <param name="fsm">此状态所属的有限状态机</param>
        /// <param name="isShutdown">是否是关闭有限状态机时触发</param>
        void OnLeave(IFsm<T> fsm, bool isShutdown);

        /// <summary>
        /// 有限状态机状态销毁时调用。
        /// </summary>
        /// <param name="fsm">此状态所属的有限状态机</param>
        void OnDestroy(IFsm<T> fsm);
    }
}
