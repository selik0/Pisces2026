using GameEngine;

namespace GameLogic
{
    /// <summary>
    /// 游戏启动更新流程。FsmManager 会在主循环中自动驱动该 FSM。
    /// </summary>
    public sealed class GameUpdateFlow
    {
        private const string FsmName = "GameUpdateFlow";
        private IFsm<GameUpdateFlow> _fsm;

        /// <summary>服务器是否要求强制更新，接入版本接口后由检查状态写入。</summary>
        public bool ForceUpdateRequired { get; set; }

        /// <summary>服务器是否存在热更新，接入资源版本接口后由检查状态写入。</summary>
        public bool HotUpdateRequired { get; set; }

        /// <summary>启动更新流程。</summary>
        public void Start()
        {
            if (_fsm != null && !_fsm.IsDestroyed)
            {
                return;
            }

            _fsm = FsmManager.Instance.CreateFsm(
                FsmName,
                this,
                new CheckForceUpdateState(),
                new ForceUpdateState(),
                new CheckHotUpdateState(),
                new HotUpdateState(),
                new UpdateCompleteState());

            if (_fsm != null)
            {
                _fsm.Start<CheckForceUpdateState>();
            }
        }

        /// <summary>停止并销毁更新流程。</summary>
        public void Destroy()
        {
            if (_fsm != null)
            {
                FsmManager.Instance.DestroyFsm(_fsm);
                _fsm = null;
            }
        }

        private sealed class CheckForceUpdateState : FsmState<GameUpdateFlow>
        {
            protected override void OnEnter(IFsm<GameUpdateFlow> fsm)
            {
                // 这里请求服务器强更版本，并比较客户端版本与最低可用版本。
                fsm.Owner.ForceUpdateRequired = false;
                ChangeState<ForceUpdateState>(fsm);
            }
        }

        private sealed class ForceUpdateState : FsmState<GameUpdateFlow>
        {
            protected override void OnEnter(IFsm<GameUpdateFlow> fsm)
            {
                if (!fsm.Owner.ForceUpdateRequired)
                {
                    ChangeState<CheckHotUpdateState>(fsm);
                    return;
                }

                // 这里下载并安装强更包，完成后再进入热更检查。
                ChangeState<CheckHotUpdateState>(fsm);
            }
        }

        private sealed class CheckHotUpdateState : FsmState<GameUpdateFlow>
        {
            protected override void OnEnter(IFsm<GameUpdateFlow> fsm)
            {
                // 这里请求服务器热更版本，并比较本地资源版本。
                fsm.Owner.HotUpdateRequired = false;
                ChangeState<HotUpdateState>(fsm);
            }
        }

        private sealed class HotUpdateState : FsmState<GameUpdateFlow>
        {
            protected override void OnEnter(IFsm<GameUpdateFlow> fsm)
            {
                if (fsm.Owner.HotUpdateRequired)
                {
                    // 这里下载热更资源、校验文件并更新本地资源版本。
                }

                ChangeState<UpdateCompleteState>(fsm);
            }
        }

        private sealed class UpdateCompleteState : FsmState<GameUpdateFlow>
        {
            protected override void OnEnter(IFsm<GameUpdateFlow> fsm)
            {
                // 这里通知启动器进入登录或主场景。
                Log.Debug("[GameUpdateFlow] 更新流程完成");
            }
        }
    }
}
