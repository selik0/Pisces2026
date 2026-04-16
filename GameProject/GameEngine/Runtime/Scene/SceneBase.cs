namespace GameEngine
{
    /// <summary>
    /// 场景抽象基类，实现 <see cref="IScene"/> 并提供生命周期钩子的默认空实现。
    /// <para>
    /// 所有自定义场景均应继承此类，按需重写以下虚方法：
    /// <list type="bullet">
    ///   <item><see cref="OnEnter"/>  —— 进入场景时调用</item>
    ///   <item><see cref="OnUpdate"/> —— 每帧更新时调用</item>
    ///   <item><see cref="OnExit"/>   —— 退出场景时调用</item>
    ///   <item><see cref="OnDestroy"/>—— 场景被销毁时调用</item>
    /// </list>
    /// </para>
    /// <code>
    /// public class MainMenuScene : SceneBase
    /// {
    ///     public override string Name => "MainMenu";
    ///
    ///     public override void OnEnter(SceneArgs args)
    ///     {
    ///         Log.Debug("[MainMenuScene] 进入主菜单");
    ///     }
    ///
    ///     public override void OnUpdate(float deltaTime) { }
    ///
    ///     public override void OnExit()
    ///     {
    ///         Log.Debug("[MainMenuScene] 退出主菜单");
    ///     }
    /// }
    /// </code>
    /// </summary>
    public abstract class SceneBase : IScene
    {
        /// <inheritdoc/>
        public abstract string Name { get; }

        /// <inheritdoc/>
        public bool IsActive { get; private set; }

        /// <summary>
        /// 由 <see cref="SceneManager"/> 内部调用，设置激活状态。
        /// </summary>
        internal void SetActive(bool active) => IsActive = active;

        /// <inheritdoc/>
        public virtual void OnEnter(SceneArgs args) { }

        /// <inheritdoc/>
        public virtual void OnUpdate(float deltaTime) { }

        /// <inheritdoc/>
        public virtual void OnExit() { }

        /// <inheritdoc/>
        public virtual void OnDestroy() { }
    }
}
