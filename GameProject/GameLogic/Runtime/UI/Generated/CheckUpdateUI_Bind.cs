// 自动生成，请勿手动修改
// 生成时间：2026-10-13 15:50:38
using GameEngine;

namespace GameLogic
{
    public partial class CheckUpdateUI
    {
        public UnityEngine.UI.Image imgBg;
        public TMPro.TextMeshProUGUI txtMsg;
        public UnityEngine.UI.Slider sldProgress;

        protected override void OnBind()
        {
            base.OnBind();
            imgBg = Entity.GetCollectedComponent<UnityEngine.UI.Image>("imgBg");
            txtMsg = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("txtMsg");
            sldProgress = Entity.GetCollectedComponent<UnityEngine.UI.Slider>("sldProgress");

            sldProgress.onValueChanged.AddListener(OnProgressValueChanged);
        }

        protected override void OnUnbind()
        {
            sldProgress.onValueChanged.RemoveListener(OnProgressValueChanged);

            imgBg = null;
            txtMsg = null;
            sldProgress = null;
            base.OnUnbind();
        }

        partial void OnProgressValueChanged(float value);


    }
}
