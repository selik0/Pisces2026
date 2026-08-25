using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// UI 管理器。
    /// 按 <see cref="UILayer"/> 分层管理与显示界面，支持：
    /// <list type="bullet">
    ///   <item>分层显示：UILayer 只决定渲染顺序，不影响其他层的可见性；普通层仅显示同层最上层，Tips 允许同层多个界面同时显示。</item>
    ///   <item>界面组合：界面可挂载 <see cref="UIWidget"/> 组件和 <see cref="UIView"/> 子界面。</item>
    ///   <item>栈式关闭：按打开顺序反向逐级关闭，也可直接关闭较早打开的界面。</item>
    ///   <item>显式类型：必须指定界面类型，可同时传入对应的打开数据。</item>
    ///   <item>同层队列：同一层的界面打开请求及数据可入队后逐个处理。</item>
    /// </list>
    /// </summary>
    public sealed class UIManager : Singleton<UIManager>, ILogin
    {
        private readonly Dictionary<UILayer, UILayerStack> _layerStacks = new Dictionary<UILayer, UILayerStack>();
        private readonly List<UIView> _navigationStack = new List<UIView>();
        private readonly Dictionary<UIView, List<UIView>> _ownedViewsByWindow = new Dictionary<UIView, List<UIView>>();
        private readonly HashSet<UILayer> _multiVisibleLayers = new HashSet<UILayer>();

        private IUIViewLoader _loader;
        private UIRoot _uiRoot;
        private bool _initialized;
        private bool _suspendPendingOpen;

        /// <summary>界面关闭并从 UIManager 移除后触发。</summary>
        public event Action<UIView> ViewClosed;

        /// <summary>当前已打开（顶层）界面的数量。</summary>
        public int OpenViewCount => _navigationStack.Count;

        public UIManager()
        {
            _loader = new DefaultUIViewLoader();
            _multiVisibleLayers.Add(UILayer.Tips);
        }

        /// <summary>设置自定义 UI 预制体加载器。</summary>
        public void SetLoader(IUIViewLoader loader)
        {
            if (loader == null)
            {
                Log.Error("[UIManager] SetLoader 失败：loader 为 null。");
                return;
            }

            _loader = loader;
        }

        /// <summary>配置某层是否允许同层多个界面同时显示。Tips 默认允许。</summary>
        public void SetLayerMultiVisible(UILayer layer, bool multiVisible)
        {
            if (multiVisible)
            {
                _multiVisibleLayers.Add(layer);
            }
            else
            {
                _multiVisibleLayers.Remove(layer);
            }

            RefreshLayers();
        }

        /// <summary>使用场景中的 UI 根组件初始化。</summary>
        public void Initialize(UIRoot uiRoot)
        {
            _uiRoot = uiRoot;
            _initialized = true;

            Log.Debug("[UIManager] 初始化完成。");
        }

        // ── 打开 ────────────────────────────────────────────────────────────────

        /// <summary>
        /// 按指定界面类型打开界面。有效的 ownerWindow 必须是已打开的 <see cref="UILayer.Window"/> 界面；
        /// 无效 ownerWindow 会被视为 null，不影响界面打开。附属界面随 ownerWindow 隐藏、恢复和关闭。
        /// </summary>
        public TView OpenView<TView>(UIViewData data = null, UIView ownerWindow = null) where TView : UIView, new()
        {
            ownerWindow = NormalizeOwnerWindow(ownerWindow);
            return OpenInternal(typeof(TView), data, ownerWindow) as TView;
        }

        // ── 同层队列 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 将界面加入对应层级的队列。该层为空时立即打开，否则等待；当该层最上层界面关闭且层内清空后，逐个打开队列中的下一个。
        /// </summary>
        public void EnqueueOpen<TView>(UIViewData data = null, UIView ownerWindow = null) where TView : UIView, new()
        {
            ownerWindow = NormalizeOwnerWindow(ownerWindow);
            EnqueueInternal(typeof(TView), data, ownerWindow);
        }

        // ── 关闭 ────────────────────────────────────────────────────────────────

        /// <summary>按打开顺序反向关闭最近打开的界面。</summary>
        public bool Back()
        {
            if (_navigationStack.Count == 0)
            {
                Log.Warning("[UIManager] 当前没有可关闭的界面。");
                return false;
            }

            CloseInternal(_navigationStack[_navigationStack.Count - 1]);
            return true;
        }

        /// <summary>直接关闭指定界面，无需逐级关闭其上层界面。子界面随父界面一并关闭。</summary>
        public bool CloseView(UIView view)
        {
            if (view == null)
            {
                Log.Warning("[UIManager] CloseView 失败：view 为 null。");
                return false;
            }

            if (!_navigationStack.Contains(view))
            {
                Log.Warning($"[UIManager] 界面 {view.GetType().Name} 未打开，忽略关闭请求。");
                return false;
            }

            CloseInternal(view);
            return true;
        }

        /// <summary>直接关闭最晚打开的同类型界面。</summary>
        public bool CloseView<TView>() where TView : UIView
        {
            for (int i = _navigationStack.Count - 1; i >= 0; i--)
            {
                if (_navigationStack[i] is TView)
                {
                    CloseInternal(_navigationStack[i]);
                    return true;
                }
            }

            Log.Warning($"[UIManager] 未找到打开中的界面 {typeof(TView).Name}，忽略关闭请求。");
            return false;
        }

        /// <summary>关闭指定层级的全部界面并清空其队列。</summary>
        public void CloseLayer(UILayer layer)
        {
            UILayerStack stack = GetLayerStackOrNull(layer);
            if (stack == null)
            {
                return;
            }

            bool previousSuspendState = _suspendPendingOpen;
            _suspendPendingOpen = true;
            try
            {
                stack.ClearPending();
                UIView[] views = stack.GetAllViews();
                for (int i = views.Length - 1; i >= 0; i--)
                {
                    CloseInternal(views[i]);
                }
            }
            finally
            {
                _suspendPendingOpen = previousSuspendState;
            }
        }

        /// <summary>关闭全部已打开界面并清空所有队列。</summary>
        public void CloseAll()
        {
            bool previousSuspendState = _suspendPendingOpen;
            _suspendPendingOpen = true;
            try
            {
                foreach (UILayerStack stack in _layerStacks.Values)
                {
                    stack.ClearPending();
                }

                UIView[] views = new UIView[_navigationStack.Count];
                for (int i = 0; i < views.Length; i++)
                {
                    views[i] = _navigationStack[i];
                }

                for (int i = views.Length - 1; i >= 0; i--)
                {
                    if (_navigationStack.Contains(views[i]))
                    {
                        CloseInternal(views[i]);
                    }
                }
            }
            finally
            {
                _suspendPendingOpen = previousSuspendState;
            }
        }

        // ── 界面组合 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 通过父界面持有的 <see cref="ChildUIManager"/> 打开子界面。
        /// 打开新子界面时会先关闭旧子界面，保证同一父界面只显示一个子界面。
        /// </summary>
        public TSubView OpenSubView<TSubView>(UIView parent, UIViewData data = null, string containerPath = null)
            where TSubView : UISubView, new()
        {
            if (parent == null || parent.ChildUIManager == null)
            {
                Log.Error("[UIManager] OpenSubView 失败：父界面为空或未由 UIManager 创建。");
                return null;
            }

            return parent.ChildUIManager.Open<TSubView>(data, containerPath);
        }

        /// <summary>关闭父界面当前显示的子界面。</summary>
        public bool CloseSubView(UIView parent)
        {
            return parent != null && parent.ChildUIManager != null && parent.ChildUIManager.Close();
        }

        // ── 查询 ────────────────────────────────────────────────────────────────

        /// <summary>获取指定层级当前最上层界面，空层返回 null。</summary>
        public UIView GetTopView(UILayer layer)
        {
            return GetLayerStackOrNull(layer)?.Top;
        }

        /// <summary>获取指定层级当前最上层界面（强类型）。</summary>
        public TView GetTopView<TView>(UILayer layer) where TView : UIView
        {
            return GetTopView(layer) as TView;
        }

        /// <summary>当前是否已打开指定类型的界面。</summary>
        public bool HasView<TView>() where TView : UIView
        {
            for (int i = 0; i < _navigationStack.Count; i++)
            {
                if (_navigationStack[i] is TView)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>指定界面是否已打开。</summary>
        public bool IsOpen(UIView view)
        {
            return view != null && _navigationStack.Contains(view);
        }

        // ── Tick ────────────────────────────────────────────────────────────────

        /// <summary>
        /// 推进队列处理。通常无需每帧调用（打开/关闭均为同步完成）；
        /// 若替换为异步加载器，需在游戏主循环中每帧调用以消费队列。
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_initialized)
            {
                return;
            }

            foreach (UILayerStack stack in _layerStacks.Values)
            {
                TryOpenNextPending(stack.Layer);
            }
        }

        // ── 销毁全部 ────────────────────────────────────────────────────────────

        /// <summary>关闭全部界面并清空层级状态，保留初始化状态以便继续打开新界面。</summary>
        public void DestroyAll()
        {
            CloseAll();

            foreach (UILayerStack stack in _layerStacks.Values)
            {
                stack.ClearPending();
            }

            _layerStacks.Clear();
            _navigationStack.Clear();
            _ownedViewsByWindow.Clear();
        }

        public void Login()
        {
            DestroyAll();
        }

        public void Logout()
        {
            DestroyAll();
        }

        // ── 内部实现 ────────────────────────────────────────────────────────────

        private UIView OpenInternal(Type viewType, UIViewData data, UIView ownerWindow)
        {
            if (!_initialized)
            {
                Log.Error("[UIManager] 尚未初始化，无法打开界面。请先调用 Initialize。");
                return null;
            }

            ownerWindow = NormalizeOwnerWindow(ownerWindow);

            UIView view = InstantiateView(viewType, data);
            if (view == null)
            {
                return null;
            }

            view.Open();

            view.OwnerWindow = ownerWindow;
            UILayerStack stack = GetLayerStack(view.Layer);
            stack.PushView(view);
            _navigationStack.Add(view);
            AddOwnedView(ownerWindow, view);

            RefreshLayers();

            Log.Debug($"[UIManager] 打开界面 type={viewType.Name} layer={view.Layer}");

            return view;
        }

        private void EnqueueInternal(Type viewType, UIViewData data, UIView ownerWindow)
        {
            if (!_initialized)
            {
                Log.Error("[UIManager] 尚未初始化，无法入队。请先调用 Initialize。");
                return;
            }

            UILayerStack stack = GetLayerStack(ResolveLayer(viewType));
            if (stack.IsEmpty)
            {
                OpenInternal(viewType, data, ownerWindow);
            }
            else
            {
                stack.EnqueuePending(new UIOpenInfo(viewType, data, ownerWindow));
            }
        }

        private void TryOpenNextPending(UILayer layer)
        {
            UILayerStack stack = GetLayerStackOrNull(layer);
            if (stack == null || !stack.IsEmpty)
            {
                return;
            }

            while (stack.PendingCount > 0)
            {
                UIOpenInfo info = stack.DequeuePending();
                UIView view = OpenInternal(info.ViewType, info.Data, info.OwnerWindow);
                if (view != null)
                {
                    return;
                }
            }
        }

        private void CloseInternal(UIView view)
        {
            bool previousSuspendState = _suspendPendingOpen;
            if (_ownedViewsByWindow.ContainsKey(view))
            {
                _suspendPendingOpen = true;
            }

            try
            {
                CloseOwnedViews(view);
            }
            finally
            {
                _suspendPendingOpen = previousSuspendState;
            }

            RemoveOwnedView(view);

            // 子界面与 Widget 必须先于父界面关闭；Widget 的池化由独立管理器负责。
            view.ChildUIManager?.Close();
            UIWidgetManager.Instance.CloseByParent(view);

            // 从层级栈移除
            UILayerStack stack = GetLayerStackOrNull(view.Layer);
            bool removedFromLayer = stack != null && stack.RemoveView(view);

            // 从导航栈移除
            RemoveNavigationNode(view);

            // 生命周期关闭并销毁实体
            CloseBrickSafely(view);
            InvokeViewClosed(view);

            RefreshLayers();

            // 层内队列逐个打开
            if (!_suspendPendingOpen && removedFromLayer && stack.IsEmpty)
            {
                TryOpenNextPending(view.Layer);
            }
        }

        private void CloseBrickSafely(UIBrick brick)
        {
            if (brick.State == UIBrickState.Created ||
                brick.State == UIBrickState.Opened ||
                brick.State == UIBrickState.Hiding)
            {
                brick.Close();
            }

            if (brick.GameObject != null)
            {
                UnityEngine.Object.Destroy(brick.GameObject);
            }
        }

        private UIView InstantiateView(Type viewType, UIViewData data)
        {
            UIView view;
            try
            {
                view = (UIView)Activator.CreateInstance(viewType);
            }
            catch (Exception exception)
            {
                Log.Error($"[UIManager] 创建界面实例失败 type={viewType.Name}。", exception);
                return null;
            }

            Transform parent = _uiRoot.UICanvas.transform;
            string prefabPath = view.PrefabPath;
            if (string.IsNullOrEmpty(prefabPath))
            {
                Log.Error($"[UIManager] 界面 {viewType.Name} 未配置 PrefabPath，无法实例化。");
                return null;
            }

            UIEntity entity = _loader.Instantiate(prefabPath, parent);
            if (entity == null)
            {
                Log.Error($"[UIManager] 界面 {viewType.Name} 实例化失败，预制体: {prefabPath}");
                return null;
            }

            UILayerStack stack = GetLayerStack(view.Layer);
            int sortOrder = stack.IsEmpty ? (int)view.Layer : stack.Top.SortOrder + 100;

            view.Data = data;
            view.ChildUIManager = new ChildUIManager(view, () => _loader);
            view.Create(entity);
            if (!view.IsBound)
            {
                UnityEngine.Object.Destroy(entity.gameObject);
                Log.Error($"[UIManager] 界面 {viewType.Name} 绑定失败。");
                return null;
            }

            view.SetSortOrder(sortOrder);
            return view;
        }

        private UIView NormalizeOwnerWindow(UIView ownerWindow)
        {
            if (ownerWindow == null)
            {
                return null;
            }

            if (ownerWindow.Layer != UILayer.Window)
            {
                Log.Warning($"[UIManager] ownerWindow {ownerWindow.GetType().Name} 不在 UILayer.Window，按非附属界面打开。");
                return null;
            }

            if (!IsOpen(ownerWindow))
            {
                Log.Warning($"[UIManager] ownerWindow {ownerWindow.GetType().Name} 未打开，按非附属界面打开。");
                return null;
            }

            return ownerWindow;
        }

        private void AddOwnedView(UIView ownerWindow, UIView view)
        {
            if (ownerWindow == null)
            {
                return;
            }

            if (!_ownedViewsByWindow.TryGetValue(ownerWindow, out List<UIView> ownedViews))
            {
                ownedViews = new List<UIView>();
                _ownedViewsByWindow[ownerWindow] = ownedViews;
            }

            ownedViews.Add(view);
        }

        private void CloseOwnedViews(UIView ownerWindow)
        {
            if (!_ownedViewsByWindow.TryGetValue(ownerWindow, out List<UIView> ownedViews))
            {
                return;
            }

            _ownedViewsByWindow.Remove(ownerWindow);
            UIView[] views = ownedViews.ToArray();
            for (int i = views.Length - 1; i >= 0; i--)
            {
                if (IsOpen(views[i]))
                {
                    CloseInternal(views[i]);
                }
            }
        }

        private void RemoveOwnedView(UIView view)
        {
            UIView ownerWindow = view.OwnerWindow;
            view.OwnerWindow = null;
            if (ownerWindow == null || !_ownedViewsByWindow.TryGetValue(ownerWindow, out List<UIView> ownedViews))
            {
                return;
            }

            ownedViews.Remove(view);
            if (ownedViews.Count == 0)
            {
                _ownedViewsByWindow.Remove(ownerWindow);
            }
        }

        private bool IsOwnerWindowActive(UIView view)
        {
            UIView ownerWindow = view.OwnerWindow;
            return ownerWindow == null || IsTopView(ownerWindow);
        }

        private int FindTopEligibleViewIndex(UILayerStack stack)
        {
            for (int i = stack.ViewCount - 1; i >= 0; i--)
            {
                if (IsOwnerWindowActive(stack.GetView(i)))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsTopView(UIView view)
        {
            UILayerStack stack = GetLayerStackOrNull(view.Layer);
            return stack != null && stack.Top == view;
        }

        private void RemoveNavigationNode(UIView view)
        {
            _navigationStack.Remove(view);
        }

        private void InvokeViewClosed(UIView view)
        {
            try
            {
                ViewClosed?.Invoke(view);
            }
            catch (Exception exception)
            {
                Log.Error($"[UIManager] ViewClosed 回调异常，界面: {view.GetType().Name}。", exception);
            }
        }

        private UILayer ResolveLayer(Type viewType)
        {
            try
            {
                UIView prototype = (UIView)Activator.CreateInstance(viewType);
                return prototype.Layer;
            }
            catch (Exception exception)
            {
                Log.Error($"[UIManager] 无法创建 {viewType.Name} 实例以解析层级。", exception);
                return UILayer.Window;
            }
        }

        private UILayerStack GetLayerStack(UILayer layer)
        {
            if (!_layerStacks.TryGetValue(layer, out UILayerStack stack))
            {
                stack = new UILayerStack(layer);
                _layerStacks[layer] = stack;
            }

            return stack;
        }

        private UILayerStack GetLayerStackOrNull(UILayer layer)
        {
            _layerStacks.TryGetValue(layer, out UILayerStack stack);
            return stack;
        }

        private void RefreshLayers()
        {
            PruneDestroyedViews();

            foreach (KeyValuePair<UILayer, UILayerStack> kv in _layerStacks)
            {
                UILayerStack stack = kv.Value;
                bool multiVisible = _multiVisibleLayers.Contains(kv.Key);
                int topEligibleIndex = multiVisible ? -1 : FindTopEligibleViewIndex(stack);
                int count = stack.ViewCount;

                for (int i = 0; i < count; i++)
                {
                    UIView view = stack.GetView(i);
                    bool canShowInLayer = multiVisible || i == topEligibleIndex;
                    bool shouldShow = canShowInLayer && IsOwnerWindowActive(view);
                    if (shouldShow && !view.IsVisible)
                    {
                        view.Show();
                    }
                    else if (!shouldShow && view.IsVisible)
                    {
                        view.Hide();
                    }
                }
            }
        }

        private void PruneDestroyedViews()
        {
            foreach (UILayerStack stack in _layerStacks.Values)
            {
                for (int i = stack.ViewCount - 1; i >= 0; i--)
                {
                    UIView view = stack.GetView(i);
                    if (view.GameObject == null)
                    {
                        stack.RemoveView(view);
                        RemoveNavigationNode(view);
                    }
                }
            }
        }
    }
}
