using System;
using GameEngine;

namespace GameLogic
{
    /// <summary>通用确认弹窗的显示数据。</summary>
    public sealed class ConfirmDialogUIData : UIViewData
    {
        /// <summary>弹窗标题。</summary>
        public readonly string Title;

        /// <summary>弹窗内容。</summary>
        public readonly string Content;

        /// <summary>确认按钮的数据。</summary>
        public readonly ConfirmDialogButtonData ConfirmButton;

        /// <summary>取消按钮的数据，为 null 时不显示取消按钮。</summary>
        public readonly ConfirmDialogButtonData CancelButton;

        public Action OnClose { get; set; }

        public ConfirmDialogUIData()
        {
        }

        /// <summary>构造弹窗数据；省略取消按钮时显示单按钮弹窗。</summary>
        public ConfirmDialogUIData(uint titleId, uint contentId, ConfirmDialogButtonData confirmButton,
            ConfirmDialogButtonData cancelButton = null)
        {
            Title = titleId.ToString();
            Content = contentId.ToString();
            ConfirmButton = confirmButton;
            CancelButton = cancelButton;
        }

        /// <summary>构造弹窗数据；省略取消按钮时显示单按钮弹窗。</summary>
        public ConfirmDialogUIData(string title, string content, ConfirmDialogButtonData confirmButton,
            ConfirmDialogButtonData cancelButton = null)
        {
            Title = title;
            Content = content;
            ConfirmButton = confirmButton;
            CancelButton = cancelButton;
        }
    }

    /// <summary>确认弹窗的按钮显示和点击行为数据。</summary>
    public sealed class ConfirmDialogButtonData
    {
        public static readonly ConfirmDialogButtonData DefaultConfirmButton = new ConfirmDialogButtonData("确定", null);
        public static readonly ConfirmDialogButtonData DefaultCancelButton = new ConfirmDialogButtonData("取消", null);

        /// <summary>按钮显示文本。</summary>
        public string Text { get; set; }

        /// <summary>按钮点击事件；需要关闭时在界面关闭后执行。</summary>
        public Action OnClick { get; set; }

        /// <summary>点击按钮后是否关闭界面，默认关闭。</summary>
        public bool CloseOnClick { get; set; }

        public ConfirmDialogButtonData(uint textId, Action onClick, bool closeOnClick = true)
        {
            Text = textId.ToString();
            OnClick = onClick;
            CloseOnClick = closeOnClick;
        }

        /// <summary>构造按钮数据。</summary>
        public ConfirmDialogButtonData(string text, Action onClick, bool closeOnClick = true)
        {
            Text = text;
            OnClick = onClick;
            CloseOnClick = closeOnClick;
        }
    }
}
