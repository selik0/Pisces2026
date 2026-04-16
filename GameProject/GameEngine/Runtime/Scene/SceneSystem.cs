namespace GameEngine
{
    /// <summary>
    /// 全局场景系统静态入口，内部持有默认的 <see cref="SceneManager"/> 单例。
    ///
    /// <para>
    /// 需要在游戏主循环中每帧调用 <see cref="Tick"/>，通常放在 GameBootstrap.Update 中。
    /// </para>
    ///
    /// <code>
    /// // ── 游戏启动 Bootstrap ────────────────────────────────────
    /// void Update() => SceneSystem.Tick(Time.deltaTime);
    ///
    /// // ── 注册所有场景 ──────────────────────────────────────────
    /// SceneSystem.Register(new MainMenuScene());
    /// SceneSystem.Register(new GameScene());
    /// SceneSystem.Register(new LoadingScene());
    ///
    /// // ── 进入初始场景 ──────────────────────────────────────────
    /// SceneSystem.SwitchTo("MainMenu");
    ///
    /// // ── 携带参数切换场景 ──────────────────────────────────────
    /// var args = new SceneArgs().Set("level", 3);
    /// SceneSystem.SwitchTo("GameScene", args);
    ///
    /// // ── 按类型切换 ────────────────────────────────────────────
    /// SceneSystem.SwitchTo&lt;GameScene&gt;();
    ///
    /// // ── 查询当前场景 ──────────────────────────────────────────
    /// var current = SceneSystem.CurrentScene;
    /// var name    = SceneSystem.CurrentSceneName;
    ///
    /// // ── 获取指定场景 ──────────────────────────────────────────
    /// var scene = SceneSystem.GetScene&lt;GameScene&gt;("GameScene");
    ///
    /// // ── 注销场景 ──────────────────────────────────────────────
    /// SceneSystem.Unregister("LoadingScene");
    ///
    /// // ── Debug 模式 ────────────────────────────────────────────
    /// SceneSystem.DebugMode = true;
    /// </code>
    /// </summary>
    public static class SceneSystem
    {
        private static SceneManager _default;

        /// <summary>全局默认 SceneManager 实例（懒初始化）</summary>
        public static SceneManager Default
        {
            get
            {
                if (_default == null) _default = new SceneManager();
                return _default;
            }
        }

        /// <summary>是否开启 Debug 模式（透传给 Default manager）</summary>
        public static bool DebugMode
        {
            get => Default.DebugMode;
            set => Default.DebugMode = value;
        }

        /// <summary>当前激活的场景，未切换前为 null</summary>
        public static IScene CurrentScene => Default.CurrentScene;

        /// <summary>当前激活场景的名称，未切换前为 null</summary>
        public static string CurrentSceneName => Default.CurrentSceneName;

        /// <summary>已注册的场景数量</summary>
        public static int Count => Default.Count;

        // ── Tick ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 推进当前场景逻辑。应在 MonoBehaviour.Update 中每帧调用。
        /// </summary>
        /// <param name="deltaTime">帧间隔（秒），传入 <c>Time.deltaTime</c></param>
        public static void Tick(float deltaTime)
            => Default.Tick(deltaTime);

        // ── 注册 / 注销 ──────────────────────────────────────────────────────────

        /// <inheritdoc cref="SceneManager.Register"/>
        public static void Register(IScene scene)
            => Default.Register(scene);

        /// <inheritdoc cref="SceneManager.Unregister"/>
        public static void Unregister(string name)
            => Default.Unregister(name);

        // ── 切换 ─────────────────────────────────────────────────────────────────

        /// <inheritdoc cref="SceneManager.SwitchTo(string, SceneArgs)"/>
        public static void SwitchTo(string name, SceneArgs args = null)
            => Default.SwitchTo(name, args);

        /// <inheritdoc cref="SceneManager.SwitchTo{TScene}(SceneArgs)"/>
        public static void SwitchTo<TScene>(SceneArgs args = null) where TScene : class, IScene
            => Default.SwitchTo<TScene>(args);

        // ── 查询 ─────────────────────────────────────────────────────────────────

        /// <inheritdoc cref="SceneManager.HasScene"/>
        public static bool HasScene(string name)
            => Default.HasScene(name);

        /// <inheritdoc cref="SceneManager.GetScene(string)"/>
        public static IScene GetScene(string name)
            => Default.GetScene(name);

        /// <inheritdoc cref="SceneManager.GetScene{TScene}(string)"/>
        public static TScene GetScene<TScene>(string name) where TScene : class, IScene
            => Default.GetScene<TScene>(name);

        /// <inheritdoc cref="SceneManager.GetAllScenes"/>
        public static IScene[] GetAllScenes()
            => Default.GetAllScenes();

        // ── 销毁全部 ─────────────────────────────────────────────────────────────

        /// <inheritdoc cref="SceneManager.DestroyAll"/>
        public static void DestroyAll()
            => Default.DestroyAll();

        // ── 重置 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 完全重置全局场景系统（测试用，游戏运行时慎用）。
        /// </summary>
        public static void Reset()
        {
            _default?.DestroyAll();
            _default = null;
        }
    }
}
