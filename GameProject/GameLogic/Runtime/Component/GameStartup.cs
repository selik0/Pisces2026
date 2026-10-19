using GameEngine;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace GameLogic
{
    /// <summary>场景加载后的游戏启动流程入口。</summary>
    public sealed class GameStartup : MonoBehaviour
    {
        private const string EditorMainSceneName = "MainScene";
        private const string PackageUpdateSceneName = "UpdateScene";

        [SerializeField] private CameraRoot _cameraRoot = null;
        [SerializeField] private UIRoot _uiRoot = null;

        private static GameStartup _instance;
        /// <summary>启动时配置的相机根组件。</summary>
        public CameraRoot CameraRoot => _cameraRoot;

        /// <summary>启动时配置的 UI 根组件。</summary>
        public UIRoot UIRoot => _uiRoot;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (!EnsureStartupScene())
            {
                return;
            }

            if (_cameraRoot == null || _uiRoot == null ||
                _cameraRoot.MainCamera == null || _cameraRoot.UICamera == null || _uiRoot.UICanvas == null)
            {
                Log.Error("[GameStartup] 请配置 CameraRoot、UIRoot、主相机、UI 相机和 UI Canvas。");
                return;
            }

            _instance = this;
            // DontDestroyOnLoad 只接受根对象，保留启动层级及其相机、UI 子节点。
            DontDestroyOnLoad(transform.root.gameObject);
            DontDestroyOnLoad(_cameraRoot.transform.root.gameObject);
            DontDestroyOnLoad(_uiRoot.transform.root.gameObject);

            UIManager.Instance.Initialize(_uiRoot);
            StartStartupFlow();
        }

        private void StartStartupFlow()
        {
            if (GameNative.Platform.IsPCOrEditor)
            {
                UIManager.Instance.OpenView<EnterGameUI>();
            }
            else
            {
                UIManager.Instance.OpenView<CheckUpdateUI>();
            }
        }

        private bool EnsureStartupScene()
        {
            string currentSceneName = UnitySceneManager.GetActiveScene().name;
            if (currentSceneName != EditorMainSceneName && currentSceneName != PackageUpdateSceneName)
            {
                Log.Error("[GameStartup] 当前场景不正确。");
                return false;
            }
            string expectedSceneName = GameNative.Platform.IsPCOrEditor
                ? EditorMainSceneName
                : PackageUpdateSceneName;
            if (currentSceneName == expectedSceneName)
            {
                return true;
            }

            Log.Warning($"[GameStartup] 当前场景为 {currentSceneName}，将切换到 {expectedSceneName}。");
            UnitySceneManager.LoadScene(expectedSceneName);
            return false;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
