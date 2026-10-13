using GameEngine;

namespace GameLogic
{
    public partial class CommonDialogUI : UIView
    {
        // 设置为当前 UI 加载器使用的预制体地址。
        public override string PrefabPath => @"UIPrefab/Internal/CommonDialogUI.prefab";

        protected override void OnOpen()
        {
            base.OnOpen();
        }

        partial void OnCloseClick()
        {
        }

        partial void OnSureClick()
        {
        }

        partial void OnCancelClick()
        {
        }


    }
}
