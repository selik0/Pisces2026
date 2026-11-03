using GameEngine;
using GameNative;
using UnityEngine;

namespace GameClient
{
    /// <summary>Unity 客户端启动装配入口。</summary>
    public static class GameLauncher
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Platform.SetService(new PlatformImp());
            ManagerHub.Instance.Initialize();
#if GAME_ASSET_AB
            AssetManager.Instance.SetProvider(new AssetBundleAssetProvider("AssetBundles"));
#else
            AssetManager.Instance.SetProvider(new EditorAssetProvider());
            UIManager.Instance.SetLoader(new EditorUIViewLoader());
#endif
        }
    }
}
