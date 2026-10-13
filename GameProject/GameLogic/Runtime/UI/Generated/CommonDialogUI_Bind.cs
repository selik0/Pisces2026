// 自动生成，请勿手动修改
// 生成时间：2026-10-13 11:27:39
using GameEngine;

namespace GameLogic
{
    public partial class CommonDialogUI
    {
        public TMPro.TextMeshProUGUI tmpMsg;
        public UnityEngine.UI.Button btnClose;
        public UnityEngine.UI.Button btnSure;
        public UnityEngine.UI.Button btnCancel;

        protected override void OnBind()
        {
            base.OnBind();
            tmpMsg = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("tmpMsg");
            btnClose = Entity.GetCollectedComponent<UnityEngine.UI.Button>("btnClose");
            btnSure = Entity.GetCollectedComponent<UnityEngine.UI.Button>("btnSure");
            btnCancel = Entity.GetCollectedComponent<UnityEngine.UI.Button>("btnCancel");

            btnClose.onClick.AddListener(OnCloseClick);
            btnSure.onClick.AddListener(OnSureClick);
            btnCancel.onClick.AddListener(OnCancelClick);
        }

        protected override void OnUnbind()
        {
            btnClose.onClick.RemoveListener(OnCloseClick);
            btnSure.onClick.RemoveListener(OnSureClick);
            btnCancel.onClick.RemoveListener(OnCancelClick);

            tmpMsg = null;
            btnClose = null;
            btnSure = null;
            btnCancel = null;
            base.OnUnbind();
        }

        partial void OnCloseClick();
        partial void OnSureClick();
        partial void OnCancelClick();


    }
}
