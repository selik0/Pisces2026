// 自动生成，请勿手动修改
// 生成时间：2026-10-19 18:02:48
using GameEngine;

namespace GameLogic
{
    public partial class EnterGameUI
    {
        public UnityEngine.UI.Image imgBackground;
        public TMPro.TextMeshProUGUI txtVersion;
        public UnityEngine.UI.Button btnLogin;
        public TMPro.TextMeshProUGUI txtLogin;
        public UnityEngine.UI.Button btnNotice;
        public TMPro.TextMeshProUGUI txtNotice;
        public UnityEngine.UI.Image imgLogo;
        public TMPro.TextMeshProUGUI txtAntiAddiction;
        public TMPro.TextMeshProUGUI txtServer;
        public UnityEngine.UI.Button btnStart;
        public TMPro.TextMeshProUGUI txtStart;
        public TMPro.TextMeshProUGUI txtLogo;

        protected override void OnBind()
        {
            base.OnBind();
            imgBackground = Entity.GetCollectedComponent<UnityEngine.UI.Image>("imgBackground");
            txtVersion = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("txtVersion");
            btnLogin = Entity.GetCollectedComponent<UnityEngine.UI.Button>("btnLogin");
            txtLogin = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("txtLogin");
            btnNotice = Entity.GetCollectedComponent<UnityEngine.UI.Button>("btnNotice");
            txtNotice = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("txtNotice");
            imgLogo = Entity.GetCollectedComponent<UnityEngine.UI.Image>("imgLogo");
            txtAntiAddiction = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("txtAntiAddiction");
            txtServer = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("txtServer");
            btnStart = Entity.GetCollectedComponent<UnityEngine.UI.Button>("btnStart");
            txtStart = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("txtStart");
            txtLogo = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("txtLogo");

            btnLogin.onClick.AddListener(OnLoginClick);
            btnNotice.onClick.AddListener(OnNoticeClick);
            btnStart.onClick.AddListener(OnStartClick);
        }

        protected override void OnUnbind()
        {
            btnLogin.onClick.RemoveListener(OnLoginClick);
            btnNotice.onClick.RemoveListener(OnNoticeClick);
            btnStart.onClick.RemoveListener(OnStartClick);

            imgBackground = null;
            txtVersion = null;
            btnLogin = null;
            txtLogin = null;
            btnNotice = null;
            txtNotice = null;
            imgLogo = null;
            txtAntiAddiction = null;
            txtServer = null;
            btnStart = null;
            txtStart = null;
            txtLogo = null;
            base.OnUnbind();
        }

        partial void OnLoginClick();
        partial void OnNoticeClick();
        partial void OnStartClick();


    }
}
