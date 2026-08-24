using GameClient;
using GameEngine;
using GameNative;
using UnityEngine;

/// <summary>Unity 客户端启动装配入口。</summary>
public static class GameLauncher
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        Platform.SetService(new PlatformImp());
        ManagerHub.Instance.Initialize();
    }
}
