namespace GameEngine
{
    /// <summary>
    /// UI 层级枚举，值越大显示层级越高。
    /// 每个层级预留 1000 的间隔，便于后续插入新层级。
    /// </summary>
    public enum UILayer
    {
        /// <summary>场景 HUD，例如血条、名字、战斗飘字</summary>
        SceneHud = 0,

        /// <summary>常驻 HUD，例如主界面资源栏</summary>
        Hud = 1000,

        /// <summary>全屏页面，打开后看不到下层界面，例如主界面</summary>
        Window = 2000,

        /// <summary>局部功能面板</summary>
        Page = 3000,

        /// <summary>普通窗口，例如设置、邮件</summary>
        Popup = 4000,

        /// <summary>高优先级弹窗，例如确认框、奖励弹窗</summary>
        Dialog = 5000,

        /// <summary>新手引导层</summary>
        Guide = 6000,

        /// <summary>飘字提示层</summary>
        Toast = 7000,

        /// <summary>加载遮罩层</summary>
        Loading = 8000,

        /// <summary>调试层</summary>
        Debug = 9000,
    }
}