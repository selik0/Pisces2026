using GameEngine;

namespace GameLogic
{
    public partial class CheckUpdateUI : UIView<CheckUpdateUIData>
    {
        // 设置为当前 UI 加载器使用的预制体地址。
        public override string PrefabPath => @"Assets/GameAssets/UIPrefab/Internal/CheckUpdateUI.prefab";

        private GameUpdateFlow _updateFlow;

        protected override void OnOpen()
        {
            base.OnOpen();
            _updateFlow = Data == null ? null : Data.UpdateFlow;
            if (_updateFlow == null)
            {
                txtMsg.text = "更新流程未配置";
                return;
            }

            _updateFlow.StageChanged += OnStageChanged;
            _updateFlow.ProgressChanged += OnProgressChanged;
            _updateFlow.Completed += OnUpdateCompleted;
            _updateFlow.Failed += OnUpdateFailed;
            SetProgressVisible(false);
            txtMsg.text = "正在检查版本，请稍候...";
            sldProgress.value = 0f;
            _updateFlow.Start();
        }

        protected override void OnClose()
        {
            if (_updateFlow != null)
            {
                _updateFlow.StageChanged -= OnStageChanged;
                _updateFlow.ProgressChanged -= OnProgressChanged;
                _updateFlow.Completed -= OnUpdateCompleted;
                _updateFlow.Failed -= OnUpdateFailed;
            }

            base.OnClose();
        }

        private void OnStageChanged()
        {
            if (_updateFlow.Stage == GameUpdateStage.WaitingForHotUpdate)
            {
                ShowUpdateConfirm(false);
            }
            else if (_updateFlow.Stage == GameUpdateStage.WaitingForForceUpdate)
            {
                ShowUpdateConfirm(true);
            }
            else if (_updateFlow.Stage == GameUpdateStage.Downloading)
            {
                SetProgressVisible(true);
                txtMsg.text = "正在下载更新文件...";
            }
        }

        private void ShowUpdateConfirm(bool forceUpdate)
        {
            txtMsg.text = forceUpdate ? "检测到强制更新" : "检测到新版本";
            string content = forceUpdate
                ? "当前版本过低，必须更新客户端后才能继续游戏。"
                : "发现新版本，是否立即下载更新？";
            string confirmText = forceUpdate ? "前往更新" : "立即更新";
            System.Action confirmAction = forceUpdate ? (System.Action)OpenForceUpdate : AcceptHotUpdate;
            ConfirmDialogButtonData confirmButton = new ConfirmDialogButtonData(confirmText, confirmAction);
            ConfirmDialogButtonData cancelButton = forceUpdate
                ? null
                : new ConfirmDialogButtonData("稍后再说", null);
            UIManager.Instance.OpenView<ConfirmDialogUI>(
                new ConfirmDialogUIData(forceUpdate ? "强制更新" : "热更新", content,
                    confirmButton, cancelButton));
        }

        private void AcceptHotUpdate()
        {
            _updateFlow.ConfirmHotUpdate();
        }

        private void OnProgressChanged()
        {
            sldProgress.value = _updateFlow.DownloadProgress;
        }

        private void OnUpdateCompleted()
        {
            if (State != UIBrickState.Opened)
            {
                return;
            }

            txtMsg.text = "更新完成";
            CloseSelf();
            UIManager.Instance.OpenView<LoginUI>();
        }

        private void OnUpdateFailed()
        {
            txtMsg.text = _updateFlow.ErrorMessage ?? "更新失败";
        }

        private void OpenForceUpdate()
        {
            _updateFlow.OpenForceUpdateUrl();
        }

        private void SetProgressVisible(bool visible)
        {
            if (sldProgress != null)
            {
                sldProgress.gameObject.SetActive(visible);
            }
        }

        private void CloseSelf()
        {
            if (State == UIBrickState.Opened)
            {
                UIManager.Instance.CloseView(this);
            }
        }

        partial void OnProgressValueChanged(float value)
        {
        }


    }
}
