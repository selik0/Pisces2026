using GameEngine;
using GameLogic;
using UnityEngine;

/// <summary>场景加载后的游戏启动流程入口。</summary>
public sealed class GameStartup : MonoBehaviour
{
    private bool _started;
    private GameUpdateFlow _updateFlow;

    private void Update()
    {
        if (_started)
        {
            return;
        }

        UIRoot uiRoot = FindObjectOfType<UIRoot>();
        if (uiRoot == null)
        {
            return;
        }

        _started = true;
        UIManager.Instance.Initialize(uiRoot);
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
    }
}
