using GameEngine;

namespace GameLogic
{
    public partial class ConfirmDialogUI : UIView<ConfirmDialogUIData>
    {
        // 设置为当前 UI 加载器使用的预制体地址。
        public override string PrefabPath => @"Internal/ConfirmDialogUI.prefab";
        public override UILayer Layer => UILayer.Dialog;

        private bool _handlingClick;

        protected override void OnOpen()
        {
            base.OnOpen();
            _handlingClick = false;
            UpdateUI();
        }

        protected override void OnClose()
        {
            base.OnClose();
            Data.OnClose?.Invoke();
        }

        /// <summary>根据当前弹窗数据更新标题、内容和按钮显示。</summary>
        public void UpdateUI()
        {
            txtTitle.text = Data.Title;
            txtMsg.text = Data.Content;
            txtSure.text = Data.ConfirmButton.Text;

            var isShowCancel = Data.CancelButton != null;
            btnCancel.gameObject.SetActive(isShowCancel);
            if (isShowCancel)
            {
                txtCancel.text = Data.CancelButton.Text;
            }
        }

        partial void OnCloseClick()
        {
            UIManager.Instance.CloseView(this);
        }

        partial void OnSureClick()
        {
            ButtonClickHandler(Data.ConfirmButton);
        }

        partial void OnCancelClick()
        {
            ButtonClickHandler(Data.CancelButton);
        }

        private void ButtonClickHandler(ConfirmDialogButtonData buttonData)
        {
            if (_handlingClick || buttonData == null || State != UIBrickState.Opened)
            {
                return;
            }

            _handlingClick = true;
            try
            {
                buttonData.OnClick?.Invoke();
                if (buttonData.CloseOnClick)
                {
                    UIManager.Instance.CloseView(this);
                }
            }
            finally
            {
                _handlingClick = false;
            }
        }
    }
}
