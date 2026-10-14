using System;
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

        /// <summary>流程完成通知；参数表示是否成功，成功时本地凭据已保存。</summary>
        public event Action<bool> Completed;

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
            else
            {
                ErrorMessage = "无法创建登录流程";
                Completed?.Invoke(false);
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
                if (string.IsNullOrWhiteSpace(fsm.Owner.Account))
                {
                    fsm.Owner.ErrorMessage = "登录账号为空";
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
                // 模拟登录：任意非空账号均成功，不校验密码。
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
                try
                {
                    LocalDataManager.Instance.Save(new LoginSaveData
                    {
                        Account = fsm.Owner.Account,
                        Password = fsm.Owner.Credential ?? string.Empty
                    });
                }
                catch (Exception exception)
                {
                    fsm.Owner.LoginSucceeded = false;
                    fsm.Owner.ErrorMessage = "保存登录信息失败";
                    Log.Error("[GameLoginFlow] 保存登录信息失败", exception);
                    ChangeState<LoginFailedState>(fsm);
                    return;
                }

                Log.Debug("[GameLoginFlow] 登录流程完成");
                fsm.Owner.Completed?.Invoke(true);
            }
        }

        private sealed class LoginFailedState : FsmState<GameLoginFlow>
        {
            protected override void OnEnter(IFsm<GameLoginFlow> fsm)
            {
                Log.Error($"[GameLoginFlow] 登录失败：{fsm.Owner.ErrorMessage ?? "未知错误"}");
                fsm.Owner.Completed?.Invoke(false);
            }
        }
    }
}
