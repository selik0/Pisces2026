using GameEngine;

namespace GameLogic
{
    /// <summary>更新检查界面使用的更新流程数据。</summary>
    public sealed class CheckUpdateUIData : UIViewData
    {
        public readonly GameUpdateFlow UpdateFlow;

        public CheckUpdateUIData(GameUpdateFlow updateFlow)
        {
            UpdateFlow = updateFlow;
        }
    }
}
