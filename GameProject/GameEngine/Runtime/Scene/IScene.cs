namespace GameEngine
{
    /// <summary>
    /// 场景接口，定义场景的基本生命周期契约。
    /// </summary>
    public interface IScene
    {
        /// <summary>场景名称（唯一标识）</summary>
        string Name { get; }

        /// <summary>场景是否已激活</summary>
        bool IsActive { get; }

        /// <summary>
        /// 场景进入时调用（由 <see cref="SceneManager"/> 在切换时调用）。
        /// </summary>
        /// <param name="args">从上一个场景传递过来的参数，可为 null</param>
        void OnEnter(SceneArgs args);

        /// <summary>
        /// 场景每帧更新（由 <see cref="SceneManager"/> 在 Tick 时调用）。
        /// </summary>
        /// <param name="deltaTime">帧间隔（秒）</param>
        void OnUpdate(float deltaTime);

        /// <summary>
        /// 场景退出时调用（由 <see cref="SceneManager"/> 在切换时调用）。
        /// </summary>
        void OnExit();

        /// <summary>
        /// 销毁场景，释放资源（由 <see cref="SceneManager"/> 在移除/关闭时调用）。
        /// </summary>
        void OnDestroy();
    }
}
