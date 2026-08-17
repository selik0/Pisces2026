using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 场景管理器。
    /// <para>
    /// 继承 <see cref="Singleton{T}"/>（实现 <see cref="ILogin"/>），
    /// 登录/登出时销毁所有场景，避免跨会话残留。
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
    public sealed class SceneManager : Singleton<SceneManager>, ILogin
    {
        public SceneManager()
        {
        }

        private readonly Dictionary<string, IScene> _scenes = new Dictionary<string, IScene>();

        /// <summary>当前激活的场景，未切换前为 null</summary>
        public IScene CurrentScene { get; private set; }

        /// <summary>当前激活场景的名称，未切换前为 null</summary>
        public string CurrentSceneName => CurrentScene?.Name;

        /// <summary>已注册的场景数量</summary>
        public int Count => _scenes.Count;

        // ── 注册 / 注销 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 注册一个场景。场景名称须唯一。
        /// </summary>
        /// <param name="scene">要注册的场景实例，不可为 null</param>
        public void Register(IScene scene)
        {
            if (scene == null)
            {
                Log.Error("[SceneManager] 注册失败：scene 为 null。");
                return;
            }

            if (string.IsNullOrEmpty(scene.Name))
            {
                Log.Error("[SceneManager] 注册失败：场景名称不能为空。");
                return;
            }

            if (_scenes.ContainsKey(scene.Name))
            {
                Log.Error($"[SceneManager] 注册失败：已存在名称为 '{scene.Name}' 的场景。");
                return;
            }

            _scenes[scene.Name] = scene;

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
            }

            _scenes.Remove(name);

            try
            {
                scene.OnDestroy();
            }
            catch (Exception ex)
            {
                Log.Error($"[SceneManager] 场景 OnDestroy 异常  name='{scene.Name}'", ex);
            }

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
        public void SwitchTo(string name, SceneArgs args = null)
        {
            if (!_scenes.TryGetValue(name, out var next))
            {
                Log.Error($"[SceneManager] 切换失败：未找到名称为 '{name}' 的场景。");
                return;
            }

            if (CurrentScene == next)
            {
                Log.Warning($"[SceneManager] 目标场景 '{name}' 已是当前场景，忽略切换请求。");
                return;
            }

            Log.Debug($"[SceneManager] 切换场景  '{CurrentSceneName ?? "null"}' → '{name}'");

            // 退出当前场景
            ExitCurrentScene();

            // 进入目标场景
            CurrentScene = next;
            SetSceneActive(next, true);
            next.OnEnter(args);

            Log.Debug($"[SceneManager] 场景切换完成  current='{name}'");
        }

        /// <summary>
        /// 切换到指定类型的场景（类型须唯一）。
        /// </summary>
        /// <typeparam name="TScene">目标场景类型</typeparam>
        /// <param name="args">传递给目标场景的参数，可为 null</param>
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

            Log.Error($"[SceneManager] 切换失败：未找到类型为 '{typeof(TScene).Name}' 的已注册场景。");
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
        /// 先快照再清理，单个场景销毁异常不会中断其余场景的清理。
        /// </summary>
        public void DestroyAll()
        {
            ExitCurrentScene();

            var snapshot = new List<IScene>(_scenes.Values);
            _scenes.Clear();

            foreach (var scene in snapshot)
            {
                try
                {
                    scene.OnDestroy();
                }
                catch (Exception ex)
                {
                    Log.Error($"[SceneManager] 场景 OnDestroy 异常  name='{scene.Name}'", ex);
                }
            }

            Log.Debug("[SceneManager] 已销毁所有场景");
        }

        // ── ILogin ───────────────────────────────────────────────────────────────

        /// <summary>登录时清理残留场景，保持初始状态。</summary>
        public override void Login()
        {
            DestroyAll();
        }

        /// <summary>登出时销毁所有场景，避免跨会话残留。</summary>
        public override void Logout()
        {
            DestroyAll();
        }

        // ── 内部辅助 ─────────────────────────────────────────────────────────────

        private void ExitCurrentScene()
        {
            var scene = CurrentScene;
            if (scene == null)
            {
                return;
            }

            CurrentScene = null;

            try
            {
                scene.OnExit();
            }
            catch (Exception ex)
            {
                Log.Error($"[SceneManager] 场景 OnExit 异常  name='{scene.Name}'", ex);
            }

            SetSceneActive(scene, false);
        }

        private static void SetSceneActive(IScene scene, bool active)
        {
            if (scene is SceneBase sceneBase)
            {
                sceneBase.SetActive(active);
            }
        }
    }
}
