// 自动生成，请勿手动修改
// 生成时间：2026-10-13 15:27:08
using GameEngine;

namespace GameLogic
{
    public partial class ConfirmDialogUI
    {
        public TMPro.TextMeshProUGUI txtMsg;
        public TMPro.TextMeshProUGUI txtTitle;
        public UnityEngine.UI.Button btnClose;
        public UnityEngine.UI.Button btnSure;
        public TMPro.TextMeshProUGUI txtSure;
        public UnityEngine.UI.Button btnCancel;
        public TMPro.TextMeshProUGUI txtCancel;

        protected override void OnBind()
        {
            base.OnBind();
            txtMsg = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("txtMsg");
            txtTitle = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("txtTitle");
            btnClose = Entity.GetCollectedComponent<UnityEngine.UI.Button>("btnClose");
            btnSure = Entity.GetCollectedComponent<UnityEngine.UI.Button>("btnSure");
            txtSure = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("txtSure");
            btnCancel = Entity.GetCollectedComponent<UnityEngine.UI.Button>("btnCancel");
            txtCancel = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("txtCancel");

            btnClose.onClick.AddListener(OnCloseClick);
            btnSure.onClick.AddListener(OnSureClick);
            btnCancel.onClick.AddListener(OnCancelClick);
        }

        protected override void OnUnbind()
        {
            btnClose.onClick.RemoveListener(OnCloseClick);
            btnSure.onClick.RemoveListener(OnSureClick);
            btnCancel.onClick.RemoveListener(OnCancelClick);

            txtMsg = null;
            txtTitle = null;
            btnClose = null;
            btnSure = null;
            txtSure = null;
            btnCancel = null;
            txtCancel = null;
            base.OnUnbind();
        }

        partial void OnCloseClick();
        partial void OnSureClick();
        partial void OnCancelClick();


    }
}
