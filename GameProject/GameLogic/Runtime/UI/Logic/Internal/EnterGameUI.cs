using GameEngine;
using UnityEngine;

namespace GameLogic
{
    public partial class EnterGameUI : UIView
    {
        private const string Version = "1.0.0";
        private int _autoLoginTimerId = -1;
        private bool _loginSucceeded;

        public override string PrefabPath => @"Assets/GameAssets/UIPrefab/Internal/EnterGameUI.prefab";

        protected override void OnOpen()
        {
            base.OnOpen();
            txtVersion.text = $"版本 {Version}";
            txtServer.text = "当前服务器  1区 · 曙光之城";
            txtAntiAddiction.text = "健康游戏，合理安排时间。未成年人请在家长陪同下游戏。";
            txtStatus.text = "正在自动登录...";
            btnStart.interactable = false;
            _loginSucceeded = false;
            StartAutoLogin();
        }

        protected override void OnClose()
        {
            StopAutoLogin();
            base.OnClose();
        }

        partial void OnLoginClick()
        {
            if (State != UIBrickState.Opened)
            {
                return;
            }

#if UNITY_EDITOR
            UIManager.Instance.OpenView<LoginUI>(null, this);
#else
            txtStatus.text = "SDK 登录暂未接入";
#endif
        }

        partial void OnNoticeClick()
        {
            txtStatus.text = "暂无公告";
        }

        partial void OnStartClick()
        {
            if (!_loginSucceeded)
            {
                txtStatus.text = "请先完成登录";
                return;
            }

            txtStatus.text = $"正在进入 {txtServer.text}...";
            btnStart.interactable = false;
        }

        private void StartAutoLogin()
        {
            StopAutoLogin();
            _autoLoginTimerId = TimerManager.Instance.Schedule(0.8f, CompleteAutoLogin);
        }

        private void StopAutoLogin()
        {
            if (_autoLoginTimerId < 0)
            {
                return;
            }

            TimerManager.Instance.Stop(_autoLoginTimerId);
            _autoLoginTimerId = -1;
        }

        private void CompleteAutoLogin()
        {
            _autoLoginTimerId = -1;
            _loginSucceeded = Random.Range(0, 2) == 1;
            if (_loginSucceeded)
            {
                txtStatus.text = "自动登录成功，可以开始游戏";
                btnStart.interactable = true;
                return;
            }

            txtStatus.text = "自动登录失败，请登录账号";
            UIManager.Instance.OpenView<LoginUI>(null, this);
        }
    }
}
