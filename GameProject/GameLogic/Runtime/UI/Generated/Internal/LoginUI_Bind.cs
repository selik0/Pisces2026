// 自动生成，请勿手动修改
// 生成时间：2026-10-14 16:44:13
using GameEngine;

namespace GameLogic
{
    public partial class LoginUI
    {
        public UnityEngine.UI.Image imgBackground;
        public UnityEngine.UI.Image imgCard;
        public TMPro.TextMeshProUGUI txtTitle;
        public TMPro.TMP_InputField inputAccount;
        public TMPro.TextMeshProUGUI txtAccountPlaceholder;
        public TMPro.TextMeshProUGUI txtAccount;
        public TMPro.TMP_InputField inputPassword;
        public TMPro.TextMeshProUGUI txtPasswordPlaceholder;
        public TMPro.TextMeshProUGUI txtPassword;
        public UnityEngine.UI.Button btnLogin;
        public TMPro.TextMeshProUGUI txtLogin;

        protected override void OnBind()
        {
            base.OnBind();
            imgBackground = Entity.GetCollectedComponent<UnityEngine.UI.Image>("imgBackground");
            imgCard = Entity.GetCollectedComponent<UnityEngine.UI.Image>("imgCard");
            txtTitle = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("txtTitle");
            inputAccount = Entity.GetCollectedComponent<TMPro.TMP_InputField>("inputAccount");
            txtAccountPlaceholder = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("txtAccountPlaceholder");
            txtAccount = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("txtAccount");
            inputPassword = Entity.GetCollectedComponent<TMPro.TMP_InputField>("inputPassword");
            txtPasswordPlaceholder = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("txtPasswordPlaceholder");
            txtPassword = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("txtPassword");
            btnLogin = Entity.GetCollectedComponent<UnityEngine.UI.Button>("btnLogin");
            txtLogin = Entity.GetCollectedComponent<TMPro.TextMeshProUGUI>("txtLogin");

            inputAccount.onValueChanged.AddListener(OnAccountValueChanged);
            inputAccount.onEndEdit.AddListener(OnAccountEndEdit);
            inputPassword.onValueChanged.AddListener(OnPasswordValueChanged);
            inputPassword.onEndEdit.AddListener(OnPasswordEndEdit);
            btnLogin.onClick.AddListener(OnLoginClick);
        }

        protected override void OnUnbind()
        {
            inputAccount.onValueChanged.RemoveListener(OnAccountValueChanged);
            inputAccount.onEndEdit.RemoveListener(OnAccountEndEdit);
            inputPassword.onValueChanged.RemoveListener(OnPasswordValueChanged);
            inputPassword.onEndEdit.RemoveListener(OnPasswordEndEdit);
            btnLogin.onClick.RemoveListener(OnLoginClick);

            imgBackground = null;
            imgCard = null;
            txtTitle = null;
            inputAccount = null;
            txtAccountPlaceholder = null;
            txtAccount = null;
            inputPassword = null;
            txtPasswordPlaceholder = null;
            txtPassword = null;
            btnLogin = null;
            txtLogin = null;
            base.OnUnbind();
        }

        partial void OnAccountValueChanged(string value);
        partial void OnAccountEndEdit(string value);
        partial void OnPasswordValueChanged(string value);
        partial void OnPasswordEndEdit(string value);
        partial void OnLoginClick();


    }
}
