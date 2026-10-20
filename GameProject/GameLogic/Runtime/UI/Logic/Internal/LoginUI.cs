using GameEngine;
using System;

namespace GameLogic
{
    public partial class LoginUI : UIView
    {
        // 设置为当前 UI 加载器使用的预制体地址。
        public override string PrefabPath => @"Internal/LoginUI.prefab";
        public override UILayer Layer => UILayer.Dialog;

        private GameLoginFlow _loginFlow;
        protected override void OnOpen()
        {
            base.OnOpen();
            btnLogin.interactable = true;
            try
            {
                LoginSaveData savedData = LocalDataManager.Instance.Load<LoginSaveData>();
                inputAccount.SetTextWithoutNotify(savedData.Account ?? string.Empty);
                inputPassword.SetTextWithoutNotify(savedData.Password ?? string.Empty);
            }
            catch (Exception exception)
            {
                Log.Error("[LoginUI] 读取登录信息失败", exception);
            }
        }

        protected override void OnClose()
        {
            ReleaseLoginFlow();
            base.OnClose();
        }

        partial void OnAccountValueChanged(string value)
        {
        }

        partial void OnAccountEndEdit(string value)
        {
        }

        partial void OnPasswordValueChanged(string value)
        {
        }

        partial void OnPasswordEndEdit(string value)
        {
        }

        partial void OnLoginClick()
        {
            if (State != UIBrickState.Opened || _loginFlow != null)
            {
                return;
            }

            string account = inputAccount.text.Trim();
            if (string.IsNullOrEmpty(account))
            {
                Log.Warning("[LoginUI] 请输入账号");
                inputAccount.ActivateInputField();
                return;
            }

            _loginFlow = new GameLoginFlow
            {
                Account = account,
                Credential = inputPassword.text
            };
            _loginFlow.Completed += OnLoginCompleted;
            btnLogin.interactable = false;
            try
            {
                _loginFlow.Start();
            }
            catch (Exception exception)
            {
                ReleaseLoginFlow();
                btnLogin.interactable = true;
                Log.Error("[LoginUI] 启动登录流程失败", exception);
            }
        }

        private void OnLoginCompleted(bool succeeded)
        {
            ReleaseLoginFlow();
            btnLogin.interactable = true;
            if (succeeded)
            {
                UIManager.Instance.CloseView(this);
            }
        }

        private void ReleaseLoginFlow()
        {
            if (_loginFlow == null)
            {
                return;
            }

            GameLoginFlow loginFlow = _loginFlow;
            _loginFlow = null;
            loginFlow.Completed -= OnLoginCompleted;
            loginFlow.Destroy();
        }
    }
}
