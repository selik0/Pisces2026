using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 全局 UI 系统静态入口，内部持有默认的 <see cref="UIManager"/> 单例。
    ///
    /// <para>
    /// 打开/关闭均为同步完成，一般无需每帧调用 <see cref="Tick"/>；
    /// 若替换为异步加载器，需在游戏主循环中每帧调用。
    /// </para>
    ///
    /// <code>
    /// // ── 初始化 ────────────────────────────────────────────────────
    /// UISystem.Initialize(canvasRoot);
    /// UISystem.MaxJumpDepth = 2;
    /// UISystem.OnJumpDepthLimitReached += () => Toast.Show("已达到跳转上限");
    ///
    /// // ── 注册界面与数据类的映射 ────────────────────────────────────
    /// UISystem.RegisterView&lt;ActivityView, ActivityViewData&gt;();
    /// UISystem.RegisterView&lt;BagView, BagViewData&gt;();
    ///
    /// // ── 用数据打开界面（数据类决定打开哪个界面）───────────────────
    /// UISystem.OpenView(new ActivityViewData { ActivityId = 1001 });
    ///
    /// // ── 界面组合：活动大界面里挂页签组件和子界面 ──────────────────
    /// // （在 ActivityView.OnCreate 中）
    /// UISystem.CreateWidget&lt;ActivityTabWidget&gt;(this, "Tabs");
    /// UISystem.OpenSubView&lt;ActivityRewardView&gt;(this, new ActivityRewardViewData(), "Content");
    ///
    /// // ── 反向逐级关闭 / 直接关闭 ───────────────────────────────────
    /// UISystem.Back();                         // 关闭最近打开的界面
    /// UISystem.CloseView&lt;ActivityView&gt;();      // 直接关闭较早打开的界面
    ///
    /// // ── 同层队列逐个打开 ──────────────────────────────────────────
    /// UISystem.EnqueueOpen&lt;RewardView&gt;(new RewardViewData());
    /// UISystem.EnqueueOpen&lt;NoticeView&gt;(new NoticeViewData());
    ///
    /// // ── 获取途径跳转 ──────────────────────────────────────────────
    /// UISystem.NavigateTo&lt;ShopView&gt;(new ShopViewData());
    /// </code>
    /// </summary>
    public static class UISystem
    {
        private static UIManager _default;

        /// <summary>全局默认 UIManager 实例（懒初始化）</summary>
        public static UIManager Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new UIManager();
                }

                return _default;
            }
        }

        /// <summary>跳转深度上限，默认 2。</summary>
        public static int MaxJumpDepth
        {
            get => Default.MaxJumpDepth;
            set => Default.MaxJumpDepth = value;
        }

        /// <summary>当前跳转深度。</summary>
        public static int CurrentJumpDepth => Default.CurrentJumpDepth;

        /// <summary>当前已打开（顶层）界面的数量。</summary>
        public static int OpenViewCount => Default.OpenViewCount;

        /// <summary>达到跳转深度上限时触发。</summary>
        public static event Action OnJumpDepthLimitReached
        {
            add => Default.OnJumpDepthLimitReached += value;
            remove => Default.OnJumpDepthLimitReached -= value;
        }

        // ── 初始化 ─────────────────────────────────────────────────────────────────

        /// <inheritdoc cref="UIManager.Initialize"/>
        public static void Initialize(Transform uiRoot)
            => Default.Initialize(uiRoot);

        /// <inheritdoc cref="UIManager.SetLoader"/>
        public static void SetLoader(IUIViewLoader loader)
            => Default.SetLoader(loader);

        /// <inheritdoc cref="UIManager.SetLayerCovering"/>
        public static void SetLayerCovering(UILayer layer, bool cover)
            => Default.SetLayerCovering(layer, cover);

        // ── 注册 ─────────────────────────────────────────────────────────────────

        /// <inheritdoc cref="UIManager.RegisterView{TView,TData}"/>
        public static void RegisterView<TView, TData>() where TView : UIView, new() where TData : UIViewData
            => Default.RegisterView<TView, TData>();

        // ── 打开 ─────────────────────────────────────────────────────────────────

        /// <inheritdoc cref="UIManager.OpenView{TView}(UIViewData)"/>
        public static TView OpenView<TView>(UIViewData data = null) where TView : UIView, new()
            => Default.OpenView<TView>(data);

        /// <inheritdoc cref="UIManager.OpenView{TData}(TData)"/>
        public static UIView OpenView<TData>(TData data = null) where TData : UIViewData
            => Default.OpenView(data);

        // ── 同层队列 ─────────────────────────────────────────────────────────────

        /// <inheritdoc cref="UIManager.EnqueueOpen{TView}(UIViewData)"/>
        public static void EnqueueOpen<TView>(UIViewData data = null) where TView : UIView, new()
            => Default.EnqueueOpen<TView>(data);

        /// <inheritdoc cref="UIManager.EnqueueOpen{TData}(TData)"/>
        public static void EnqueueOpen<TData>(TData data = null) where TData : UIViewData
            => Default.EnqueueOpen(data);

        // ── 关闭 ─────────────────────────────────────────────────────────────────

        /// <inheritdoc cref="UIManager.Back"/>
        public static bool Back()
            => Default.Back();

        /// <inheritdoc cref="UIManager.CloseView(UIView)"/>
        public static bool CloseView(UIView view)
            => Default.CloseView(view);

        /// <inheritdoc cref="UIManager.CloseView{TView}"/>
        public static bool CloseView<TView>() where TView : UIView
            => Default.CloseView<TView>();

        /// <inheritdoc cref="UIManager.CloseLayer"/>
        public static void CloseLayer(UILayer layer)
            => Default.CloseLayer(layer);

        /// <inheritdoc cref="UIManager.CloseAll"/>
        public static void CloseAll()
            => Default.CloseAll();

        // ── 获取途径跳转 ─────────────────────────────────────────────────────────

        /// <inheritdoc cref="UIManager.NavigateTo{TView}(UIViewData)"/>
        public static void NavigateTo<TView>(UIViewData data = null) where TView : UIView, new()
            => Default.NavigateTo<TView>(data);

        /// <inheritdoc cref="UIManager.NavigateTo{TData}(TData)"/>
        public static void NavigateTo<TData>(TData data = null) where TData : UIViewData
            => Default.NavigateTo(data);

        // ── 界面组合 ─────────────────────────────────────────────────────────────

        /// <inheritdoc cref="UIManager.CreateWidget{TWidget}(UIView,string)"/>
        public static TWidget CreateWidget<TWidget>(UIView parent, string containerPath = null) where TWidget : UIWidget, new()
            => Default.CreateWidget<TWidget>(parent, containerPath);

        /// <inheritdoc cref="UIManager.OpenSubView{TView}(UIView,UIViewData,string)"/>
        public static TView OpenSubView<TView>(UIView parent, UIViewData data = null, string containerPath = null) where TView : UIView, new()
            => Default.OpenSubView<TView>(parent, data, containerPath);

        // ── 查询 ─────────────────────────────────────────────────────────────────

        /// <inheritdoc cref="UIManager.GetTopView(UILayer)"/>
        public static UIView GetTopView(UILayer layer)
            => Default.GetTopView(layer);

        /// <inheritdoc cref="UIManager.GetTopView{TView}(UILayer)"/>
        public static TView GetTopView<TView>(UILayer layer) where TView : UIView
            => Default.GetTopView<TView>(layer);

        /// <inheritdoc cref="UIManager.HasView{TView}"/>
        public static bool HasView<TView>() where TView : UIView
            => Default.HasView<TView>();

        /// <inheritdoc cref="UIManager.IsOpen"/>
        public static bool IsOpen(UIView view)
            => Default.IsOpen(view);

        // ── Tick ─────────────────────────────────────────────────────────────────

        /// <inheritdoc cref="UIManager.Tick"/>
        public static void Tick(float deltaTime)
            => Default.Tick(deltaTime);

        // ── 销毁全部 ─────────────────────────────────────────────────────────────

        /// <inheritdoc cref="UIManager.DestroyAll"/>
        public static void DestroyAll()
            => Default.DestroyAll();

        // ── 重置 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 完全重置全局 UI 系统（测试用，游戏运行时慎用）。
        /// </summary>
        public static void Reset()
        {
            _default?.DestroyAll();
            _default = null;
        }
    }
}
