using GameEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>场景加载后的游戏启动流程入口。</summary>
    public sealed class GameStartup : MonoBehaviour
    {
        [SerializeField] private CameraRoot _cameraRoot = null;
        [SerializeField] private UIRoot _uiRoot = null;

        private static GameStartup _instance;
        private GameUpdateFlow _updateFlow;

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
            _updateFlow = new GameUpdateFlow(new GameUpdateSettings
            {
                SimulateUpdate = true
            });
            UIManager.Instance.OpenView<CheckUpdateUI>(new CheckUpdateUIData(_updateFlow));
        }

        private void OnDestroy()
        {
            if (_updateFlow != null)
            {
                _updateFlow.Destroy();
                _updateFlow = null;
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
