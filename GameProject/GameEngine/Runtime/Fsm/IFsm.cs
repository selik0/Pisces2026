using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 有限状态机接口。
    /// </summary>
    /// <typeparam name="T">有限状态机持有者类型</typeparam>
    public interface IFsm<T> where T : class
    {
        /// <summary>有限状态机名称</summary>
        string Name { get; }

        /// <summary>有限状态机持有者</summary>
        T Owner { get; }

        /// <summary>有限状态机状态数量</summary>
        int StateCount { get; }

        /// <summary>有限状态机是否正在运行</summary>
        bool IsRunning { get; }

        /// <summary>有限状态机是否已销毁</summary>
        bool IsDestroyed { get; }

        /// <summary>当前有限状态机状态</summary>
        FsmState<T> CurrentState { get; }

        /// <summary>当前有限状态机状态持续时间（秒）</summary>
        float CurrentStateTime { get; }

        /// <summary>
        /// 开始有限状态机，进入初始状态。
        /// </summary>
        /// <typeparam name="TState">要进入的初始状态类型</typeparam>
        void Start<TState>() where TState : FsmState<T>;

        /// <summary>
        /// 开始有限状态机，进入指定类型的初始状态。
        /// </summary>
        /// <param name="stateType">要进入的初始状态类型</param>
        void Start(Type stateType);

        /// <summary>
        /// 是否存在有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要检查的有限状态机状态类型</typeparam>
        /// <returns>是否存在指定的有限状态机状态</returns>
        bool HasState<TState>() where TState : FsmState<T>;

        /// <summary>
        /// 是否存在有限状态机状态。
        /// </summary>
        /// <param name="stateType">要检查的有限状态机状态类型</param>
        /// <returns>是否存在指定的有限状态机状态</returns>
        bool HasState(Type stateType);

        /// <summary>
        /// 获取有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要获取的有限状态机状态类型</typeparam>
        /// <returns>获取到的有限状态机状态</returns>
        TState GetState<TState>() where TState : FsmState<T>;

        /// <summary>
        /// 获取有限状态机状态。
        /// </summary>
        /// <param name="stateType">要获取的有限状态机状态类型</param>
        /// <returns>获取到的有限状态机状态</returns>
        FsmState<T> GetState(Type stateType);

        /// <summary>
        /// 获取有限状态机的所有状态。
        /// </summary>
        /// <returns>有限状态机的所有状态</returns>
        FsmState<T>[] GetAllStates();

        /// <summary>
        /// 获取有限状态机数据。
        /// </summary>
        /// <typeparam name="TData">要获取的有限状态机数据类型</typeparam>
        /// <param name="name">有限状态机数据名称</param>
        /// <returns>获取到的有限状态机数据</returns>
        TData GetData<TData>(string name);

        /// <summary>
        /// 设置有限状态机数据。
        /// </summary>
        /// <typeparam name="TData">要设置的有限状态机数据类型</typeparam>
        /// <param name="name">有限状态机数据名称</param>
        /// <param name="data">要设置的有限状态机数据</param>
        void SetData<TData>(string name, TData data);

        /// <summary>
        /// 是否存在有限状态机数据。
        /// </summary>
        /// <param name="name">有限状态机数据名称</param>
        /// <returns>有限状态机数据是否存在</returns>
        bool HasData(string name);

        /// <summary>
        /// 移除有限状态机数据。
        /// </summary>
        /// <param name="name">有限状态机数据名称</param>
        /// <returns>是否移除有限状态机数据成功</returns>
        bool RemoveData(string name);

        /// <summary>
        /// 切换当前有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要切换到的有限状态机状态类型</typeparam>
        void ChangeState<TState>() where TState : FsmState<T>;

        /// <summary>
        /// 切换当前有限状态机状态。
        /// </summary>
        /// <param name="stateType">要切换到的有限状态机状态类型</param>
        void ChangeState(Type stateType);
    }
}
