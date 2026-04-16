using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 场景管理器。
    /// <para>
    /// 统一注册、切换和驱动所有 <see cref="IScene"/> 实例。
    /// 需要在游戏主循环中每帧调用 <see cref="Tick"/>。
    /// </para>
    /// <remarks>
    /// 场景切换流程：
    /// <list type="number">
    ///   <item>调用当前场景的 <see cref="IScene.OnExit"/></item>
    ///   <item>将当前场景标记为非激活</item>
    ///   <item>将目标场景标记为激活</item>
    ///   <item>调用目标场景的 <see cref="IScene.OnEnter"/></item>
    /// </list>
    /// </remarks>
    /// </summary>
    public sealed class SceneManager
    {
        private readonly Dictionary<string, IScene> _scenes = new Dictionary<string, IScene>();

        /// <summary>当前激活的场景，未切换前为 null</summary>
        public IScene CurrentScene { get; private set; }

        /// <summary>当前激活场景的名称，未切换前为 null</summary>
        public string CurrentSceneName => CurrentScene?.Name;

        /// <summary>已注册的场景数量</summary>
        public int Count => _scenes.Count;

        /// <summary>是否开启 Debug 日志</summary>
        public bool DebugMode { get; set; }

        // ── 注册 / 注销 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 注册一个场景。场景名称须唯一。
        /// </summary>
        /// <param name="scene">要注册的场景实例，不可为 null</param>
        /// <exception cref="ArgumentNullException">scene 为 null 时抛出</exception>
        /// <exception cref="InvalidOperationException">同名场景已存在时抛出</exception>
        public void Register(IScene scene)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));
            if (string.IsNullOrEmpty(scene.Name))
                throw new ArgumentException("[SceneManager] 场景名称不能为空。");
            if (_scenes.ContainsKey(scene.Name))
                throw new InvalidOperationException(
                    $"[SceneManager] 已存在名称为 '{scene.Name}' 的场景。");

            _scenes[scene.Name] = scene;

            if (DebugMode)
                Log.Debug($"[SceneManager] 注册场景  name={scene.Name}");
        }

        /// <summary>
        /// 注销并销毁一个场景。若该场景为当前激活场景，会先调用 <see cref="IScene.OnExit"/>。
        /// </summary>
        /// <param name="name">场景名称</param>
        public void Unregister(string name)
        {
            if (!_scenes.TryGetValue(name, out var scene))
            {
                Log.Warning($"[SceneManager] 未找到名称为 '{name}' 的场景，忽略注销请求。");
                return;
            }

            // 若是当前场景，先退出
            if (CurrentScene == scene)
            {
                ExitCurrentScene();
                CurrentScene = null;
            }

            scene.OnDestroy();
            _scenes.Remove(name);

            if (DebugMode)
                Log.Debug($"[SceneManager] 注销场景  name={name}");
        }

        // ── 查询 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 是否已注册指定名称的场景。
        /// </summary>
        public bool HasScene(string name) => _scenes.ContainsKey(name);

        /// <summary>
        /// 获取指定名称的场景，不存在则返回 null。
        /// </summary>
        public IScene GetScene(string name)
        {
            _scenes.TryGetValue(name, out var scene);
            return scene;
        }

        /// <summary>
        /// 获取指定名称的场景（强类型版本），不存在则返回 null。
        /// </summary>
        public TScene GetScene<TScene>(string name) where TScene : class, IScene
            => GetScene(name) as TScene;

        /// <summary>
        /// 获取所有已注册的场景。
        /// </summary>
        public IScene[] GetAllScenes()
        {
            var result = new IScene[_scenes.Count];
            _scenes.Values.CopyTo(result, 0);
            return result;
        }

        // ── 切换 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 切换到指定名称的场景。
        /// </summary>
        /// <param name="name">目标场景名称</param>
        /// <param name="args">传递给目标场景的参数，可为 null</param>
        /// <exception cref="InvalidOperationException">目标场景未注册时抛出</exception>
        public void SwitchTo(string name, SceneArgs args = null)
        {
            if (!_scenes.TryGetValue(name, out var next))
                throw new InvalidOperationException(
                    $"[SceneManager] 未找到名称为 '{name}' 的场景，无法切换。");

            if (CurrentScene == next)
            {
                Log.Warning($"[SceneManager] 目标场景 '{name}' 已是当前场景，忽略切换请求。");
                return;
            }

            if (DebugMode)
                Log.Debug($"[SceneManager] 切换场景  '{CurrentSceneName ?? "null"}' → '{name}'");

            // 退出当前场景
            ExitCurrentScene();

            // 进入目标场景
            CurrentScene = next;
            SetSceneActive(next, true);
            next.OnEnter(args);

            if (DebugMode)
                Log.Debug($"[SceneManager] 场景切换完成  current='{name}'");
        }

        /// <summary>
        /// 切换到指定类型的场景（类型须唯一）。
        /// </summary>
        /// <typeparam name="TScene">目标场景类型</typeparam>
        /// <param name="args">传递给目标场景的参数，可为 null</param>
        /// <exception cref="InvalidOperationException">找不到对应类型的已注册场景时抛出</exception>
        public void SwitchTo<TScene>(SceneArgs args = null) where TScene : class, IScene
        {
            foreach (var scene in _scenes.Values)
            {
                if (scene is TScene)
                {
                    SwitchTo(scene.Name, args);
                    return;
                }
            }
            throw new InvalidOperationException(
                $"[SceneManager] 未找到类型为 '{typeof(TScene).Name}' 的已注册场景。");
        }

        // ── Tick ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 推进当前场景逻辑。应在 MonoBehaviour.Update 中每帧调用。
        /// </summary>
        /// <param name="deltaTime">帧间隔（秒），传入 <c>Time.deltaTime</c></param>
        public void Tick(float deltaTime)
        {
            CurrentScene?.OnUpdate(deltaTime);
        }

        // ── 销毁全部 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 退出当前场景并销毁所有已注册场景。
        /// </summary>
        public void DestroyAll()
        {
            ExitCurrentScene();
            CurrentScene = null;

            foreach (var scene in _scenes.Values)
                scene.OnDestroy();

            _scenes.Clear();

            if (DebugMode)
                Log.Debug("[SceneManager] 已销毁所有场景");
        }

        // ── 内部辅助 ─────────────────────────────────────────────────────────────

        private void ExitCurrentScene()
        {
            if (CurrentScene == null) return;
            CurrentScene.OnExit();
            SetSceneActive(CurrentScene, false);
        }

        private static void SetSceneActive(IScene scene, bool active)
        {
            if (scene is SceneBase sceneBase)
                sceneBase.SetActive(active);
        }
    }
}
