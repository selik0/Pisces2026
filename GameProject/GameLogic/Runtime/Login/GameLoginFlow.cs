using GameEngine;

namespace GameLogic
{
    /// <summary>
    /// 游戏登录流程。网络请求完成后，由对应状态推进 FSM。
    /// </summary>
    public sealed class GameLoginFlow
    {
        private const string FsmName = "GameLoginFlow";
        private IFsm<GameLoginFlow> _fsm;

        /// <summary>登录账号或平台用户标识。</summary>
        public string Account { get; set; }

        /// <summary>登录凭据，例如密码、平台票据或 SDK Token。</summary>
        public string Credential { get; set; }

        /// <summary>登录请求是否成功，由网络回调写入。</summary>
        public bool LoginSucceeded { get; set; }

        /// <summary>登录失败原因。</summary>
        public string ErrorMessage { get; private set; }

        /// <summary>启动登录流程。</summary>
        public void Start()
        {
            if (_fsm != null && !_fsm.IsDestroyed)
            {
                return;
            }

            ErrorMessage = null;
            LoginSucceeded = false;
            _fsm = FsmManager.Instance.CreateFsm(
                FsmName,
                this,
                new PrepareLoginState(),
                new RequestLoginState(),
                new SyncLoginDataState(),
                new LoginCompleteState(),
                new LoginFailedState());

            if (_fsm != null)
            {
                _fsm.Start<PrepareLoginState>();
            }
        }

        /// <summary>停止并销毁登录流程。</summary>
        public void Destroy()
        {
            if (_fsm != null)
            {
                FsmManager.Instance.DestroyFsm(_fsm);
                _fsm = null;
            }
        }

        private sealed class PrepareLoginState : FsmState<GameLoginFlow>
        {
            protected override void OnEnter(IFsm<GameLoginFlow> fsm)
            {
                if (string.IsNullOrEmpty(fsm.Owner.Account) || string.IsNullOrEmpty(fsm.Owner.Credential))
                {
                    fsm.Owner.ErrorMessage = "登录账号或凭据为空";
                    ChangeState<LoginFailedState>(fsm);
                    return;
                }

                // 这里读取平台 SDK 登录凭据、检查网络状态并组装登录请求。
                ChangeState<RequestLoginState>(fsm);
            }
        }

        private sealed class RequestLoginState : FsmState<GameLoginFlow>
        {
            protected override void OnEnter(IFsm<GameLoginFlow> fsm)
            {
                // 这里请求登录服务器，网络回调中设置 LoginSucceeded 和 ErrorMessage。
                fsm.Owner.LoginSucceeded = true;
                ChangeState(fsm, fsm.Owner.LoginSucceeded
                    ? typeof(SyncLoginDataState)
                    : typeof(LoginFailedState));
            }
        }

        private sealed class SyncLoginDataState : FsmState<GameLoginFlow>
        {
            protected override void OnEnter(IFsm<GameLoginFlow> fsm)
            {
                // 这里请求并缓存玩家基础信息、背包、邮件和其他登录后必要数据。
                ChangeState<LoginCompleteState>(fsm);
            }
        }

        private sealed class LoginCompleteState : FsmState<GameLoginFlow>
        {
            protected override void OnEnter(IFsm<GameLoginFlow> fsm)
            {
                // 这里通知启动器进入主界面或游戏主场景。
                Log.Debug("[GameLoginFlow] 登录流程完成");
            }
        }

        private sealed class LoginFailedState : FsmState<GameLoginFlow>
        {
            protected override void OnEnter(IFsm<GameLoginFlow> fsm)
            {
                Log.Error($"[GameLoginFlow] 登录失败：{fsm.Owner.ErrorMessage ?? "未知错误"}");
                // 这里显示登录失败提示，并允许用户重试或返回登录页。
            }
        }
    }
}
