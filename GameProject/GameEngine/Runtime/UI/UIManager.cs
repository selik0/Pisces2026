using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// UI 管理器。
    /// 按 <see cref="UILayer"/> 分层管理与显示界面，支持：
    /// <list type="bullet">
    ///   <item>分层显示：高层级遮盖低层级，同层内只显示最上层，<see cref="UILayer.Guide"/>、<see cref="UILayer.Toast"/> 默认不遮盖下层。</item>
    ///   <item>界面组合：界面可挂载 <see cref="UIWidget"/> 组件和 <see cref="UIView"/> 子界面。</item>
    ///   <item>栈式关闭：按打开顺序反向逐级关闭，也可直接关闭较早打开的界面。</item>
    ///   <item>数据驱动：一个界面对应一个数据类，通过数据打开界面。</item>
    ///   <item>同层队列：同一层的数据和界面可入队后逐个打开。</item>
    ///   <item>获取途径跳转：支持跳转深度配置（默认 2），达到上限跳转后触发 <see cref="OnJumpDepthLimitReached"/>。</item>
    /// </list>
    /// </summary>
    public sealed class UIManager
    {
        private sealed class NavigationNode
        {
            public UIView View;
            public bool IsJump;
            public string Source;
        }

        private readonly Dictionary<UILayer, UILayerStack> _layerStacks = new Dictionary<UILayer, UILayerStack>();
        private readonly List<NavigationNode> _navigationStack = new List<NavigationNode>();
        private readonly Dictionary<Type, Type> _viewTypeByDataType = new Dictionary<Type, Type>();
        private readonly Dictionary<UIView, List<UIBrick>> _childrenByView = new Dictionary<UIView, List<UIBrick>>();
        private readonly Dictionary<UIBrick, UIView> _parentOfBrick = new Dictionary<UIBrick, UIView>();
        private readonly Dictionary<UILayer, Transform> _layerRoots = new Dictionary<UILayer, Transform>();
        private readonly HashSet<UILayer> _nonCoveringLayers = new HashSet<UILayer>();

        private IUIViewLoader _loader;
        private Transform _uiRoot;
        private bool _initialized;

        /// <summary>跳转深度上限。达到上限后跳转仍会执行，但会触发 <see cref="OnJumpDepthLimitReached"/>。</summary>
        public int MaxJumpDepth { get; set; } = 2;

        /// <summary>当前跳转深度（仍在导航栈中的跳转节点数量）。</summary>
        public int CurrentJumpDepth { get; private set; }

        /// <summary>达到跳转深度上限时触发，订阅方应弹出“已达到跳转上限”的提示。</summary>
        public event Action OnJumpDepthLimitReached;

        /// <summary>当前已打开（顶层）界面的数量。</summary>
        public int OpenViewCount => _navigationStack.Count;

        public UIManager()
        {
            _loader = new DefaultUIViewLoader();
            _nonCoveringLayers.Add(UILayer.Guide);
            _nonCoveringLayers.Add(UILayer.Toast);
        }

        /// <summary>设置自定义 UI 预制体加载器。</summary>
        public void SetLoader(IUIViewLoader loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        /// <summary>配置某层是否遮盖下层界面。默认除 <see cref="UILayer.Guide"/>、<see cref="UILayer.Toast"/> 外均遮盖。</summary>
        public void SetLayerCovering(UILayer layer, bool cover)
        {
            if (cover)
            {
                _nonCoveringLayers.Remove(layer);
            }
            else
            {
                _nonCoveringLayers.Add(layer);
            }

            RefreshLayers();
        }

        /// <summary>初始化 UI 根节点，所有层级容器将挂在其下。</summary>
        public void Initialize(Transform uiRoot)
        {
            if (uiRoot == null)
            {
                Log.Error("[UIManager] Initialize 失败：uiRoot 为 null。");
                return;
            }

            _uiRoot = uiRoot;
            _initialized = true;

            Log.Debug("[UIManager] 初始化完成。");
        }

        // ── 注册 ────────────────────────────────────────────────────────────────

        /// <summary>注册界面与数据类的映射，之后可通过 <c>OpenView&lt;TData&gt;</c> 用数据打开界面。</summary>
        public void RegisterView<TView, TData>() where TView : UIView, new() where TData : UIViewData
        {
            _viewTypeByDataType[typeof(TData)] = typeof(TView);
        }

        // ── 打开 ────────────────────────────────────────────────────────────────

        /// <summary>按指定界面类型打开界面，压入对应层级。每次调用创建一个新实例。</summary>
        public TView OpenView<TView>(UIViewData data = null) where TView : UIView, new()
        {
            return OpenInternal(typeof(TView), data, false, null) as TView;
        }

        /// <summary>通过数据类打开对应界面，需先调用 <see cref="RegisterView{TView,TData}"/> 注册映射。</summary>
        public UIView OpenView<TData>(TData data = null) where TData : UIViewData
        {
            Type viewType = ResolveViewType(typeof(TData));
            return viewType != null ? OpenInternal(viewType, data, false, null) : null;
        }

        // ── 同层队列 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 将界面加入对应层级的队列。该层为空时立即打开，否则等待；当该层最上层界面关闭且层内清空后，逐个打开队列中的下一个。
        /// </summary>
        public void EnqueueOpen<TView>(UIViewData data = null) where TView : UIView, new()
        {
            EnqueueInternal(typeof(TView), data, false, null);
        }

        /// <summary>按数据类型将界面加入对应层级队列，需先注册映射。</summary>
        public void EnqueueOpen<TData>(TData data = null) where TData : UIViewData
        {
            Type viewType = ResolveViewType(typeof(TData));
            if (viewType != null)
            {
                EnqueueInternal(viewType, data, false, null);
            }
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

            CloseInternal(_navigationStack[_navigationStack.Count - 1].View);
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

            if (!_navigationStack.Exists(node => node.View == view))
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
                if (_navigationStack[i].View is TView)
                {
                    CloseInternal(_navigationStack[i].View);
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

            UIView[] views = stack.GetAllViews();
            for (int i = views.Length - 1; i >= 0; i--)
            {
                CloseInternal(views[i]);
            }

            stack.ClearPending();
        }

        /// <summary>关闭全部已打开界面并清空所有队列。</summary>
        public void CloseAll()
        {
            UIView[] views = new UIView[_navigationStack.Count];
            for (int i = 0; i < views.Length; i++)
            {
                views[i] = _navigationStack[i].View;
            }

            for (int i = views.Length - 1; i >= 0; i--)
            {
                if (_navigationStack.Exists(node => node.View == views[i]))
                {
                    CloseInternal(views[i]);
                }
            }

            foreach (UILayerStack stack in _layerStacks.Values)
            {
                stack.ClearPending();
            }
        }

        // ── 获取途径跳转 ────────────────────────────────────────────────────────

        /// <summary>通过获取途径跳转到指定界面。每次跳转推进 <see cref="CurrentJumpDepth"/>，达到 <see cref="MaxJumpDepth"/> 后触发 <see cref="OnJumpDepthLimitReached"/>。</summary>
        public void NavigateTo<TView>(UIViewData data = null) where TView : UIView, new()
        {
            NavigateInternal(typeof(TView), data, null);
        }

        /// <summary>按数据类型跳转到对应界面。</summary>
        public void NavigateTo<TData>(TData data = null) where TData : UIViewData
        {
            Type viewType = ResolveViewType(typeof(TData));
            if (viewType != null)
            {
                NavigateInternal(viewType, data, null);
            }
        }

        // ── 界面组合 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 在父界面下创建并打开一个 UI 组件。组件生命周期随父界面关闭而结束。
        /// </summary>
        /// <param name="parent">父界面。</param>
        /// <param name="containerPath">父界面下的挂载容器子节点路径，为 null 时直接挂到父界面节点。</param>
        public TWidget CreateWidget<TWidget>(UIView parent, string containerPath = null) where TWidget : UIWidget, new()
        {
            if (parent == null)
            {
                Log.Error("[UIManager] CreateWidget 失败：parent 为 null。");
                return null;
            }

            TWidget widget = new TWidget();
            string prefabPath = widget.PrefabPath;
            if (string.IsNullOrEmpty(prefabPath))
            {
                Log.Error($"[UIManager] 组件 {typeof(TWidget).Name} 未配置 PrefabPath，无法实例化。");
                return null;
            }

            UIEntity entity = _loader.Instantiate(prefabPath, ResolveAttachPoint(parent, containerPath));
            if (entity == null)
            {
                Log.Error($"[UIManager] 组件 {typeof(TWidget).Name} 实例化失败，预制体: {prefabPath}");
                return null;
            }

            widget.Create(entity);
            if (!widget.IsBound)
            {
                UnityEngine.Object.Destroy(entity.gameObject);
                return null;
            }

            widget.Open();
            AddChild(parent, widget);
            return widget;
        }

        /// <summary>
        /// 在父界面下打开一个子界面。子界面不进入层级栈，随父界面一同显示与关闭。
        /// </summary>
        /// <param name="parent">父界面。</param>
        /// <param name="data">子界面数据。</param>
        /// <param name="containerPath">父界面下的挂载容器子节点路径，为 null 时直接挂到父界面节点。</param>
        public TView OpenSubView<TView>(UIView parent, UIViewData data = null, string containerPath = null) where TView : UIView, new()
        {
            if (parent == null)
            {
                Log.Error("[UIManager] OpenSubView 失败：parent 为 null。");
                return null;
            }

            UIView view = InstantiateView(typeof(TView), data, ResolveAttachPoint(parent, containerPath), true);
            if (view == null)
            {
                return null;
            }

            view.Open();
            AddChild(parent, view);
            return (TView)view;
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
                if (_navigationStack[i].View is TView)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>指定界面是否已打开。</summary>
        public bool IsOpen(UIView view)
        {
            return view != null && _navigationStack.Exists(node => node.View == view);
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

        /// <summary>关闭全部界面并清空所有层级容器，保留初始化状态以便继续打开新界面。</summary>
        public void DestroyAll()
        {
            CloseAll();

            foreach (UILayerStack stack in _layerStacks.Values)
            {
                stack.ClearPending();
            }

            foreach (Transform root in _layerRoots.Values)
            {
                if (root != null)
                {
                    UnityEngine.Object.Destroy(root.gameObject);
                }
            }

            _layerRoots.Clear();
            _layerStacks.Clear();
            _navigationStack.Clear();
            _childrenByView.Clear();
            _parentOfBrick.Clear();
            CurrentJumpDepth = 0;
        }

        // ── 内部实现 ────────────────────────────────────────────────────────────

        private void NavigateInternal(Type viewType, UIViewData data, string source)
        {
            UIView view = OpenInternal(viewType, data, true, source);
            if (view == null)
            {
                return;
            }

            CurrentJumpDepth++;
            if (CurrentJumpDepth >= MaxJumpDepth)
            {
                OnJumpDepthLimitReached?.Invoke();
            }
        }

        private UIView OpenInternal(Type viewType, UIViewData data, bool isJump, string source)
        {
            if (!_initialized)
            {
                Log.Error("[UIManager] 尚未初始化，无法打开界面。请先调用 Initialize。");
                return null;
            }

            UIView view = InstantiateView(viewType, data, null, false);
            if (view == null)
            {
                return null;
            }

            view.Open();

            UILayerStack stack = GetLayerStack(view.Layer);
            stack.PushView(view);
            _navigationStack.Add(new NavigationNode { View = view, IsJump = isJump, Source = source });

            RefreshLayers();

            Log.Debug($"[UIManager] 打开界面 type={viewType.Name} layer={view.Layer} jump={isJump}");

            return view;
        }

        private void EnqueueInternal(Type viewType, UIViewData data, bool isJump, string source)
        {
            if (!_initialized)
            {
                Log.Error("[UIManager] 尚未初始化，无法入队。请先调用 Initialize。");
                return;
            }

            UILayerStack stack = GetLayerStack(ResolveLayer(viewType));
            if (stack.IsEmpty)
            {
                OpenInternal(viewType, data, isJump, source);
            }
            else
            {
                stack.EnqueuePending(new UIOpenInfo(viewType, data, isJump, source));
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
                UIView view = OpenInternal(info.ViewType, info.Data, info.IsJump, info.Source);
                if (view != null)
                {
                    return;
                }
            }
        }

        private void CloseInternal(UIView view)
        {
            // 先从父界面解除绑定
            if (_parentOfBrick.TryGetValue(view, out UIView parentView))
            {
                if (_childrenByView.TryGetValue(parentView, out List<UIBrick> siblings))
                {
                    siblings.Remove(view);
                }

                _parentOfBrick.Remove(view);
            }

            // 先关闭子界面/组件（最深层优先）
            CloseChildren(view);

            // 从层级栈移除
            UILayerStack stack = GetLayerStackOrNull(view.Layer);
            bool removedFromLayer = stack != null && stack.RemoveView(view);

            // 从导航栈移除
            RemoveNavigationNode(view);

            // 生命周期关闭并销毁实体
            CloseBrickSafely(view);

            RefreshLayers();

            // 层内队列逐个打开
            if (removedFromLayer && stack.IsEmpty)
            {
                TryOpenNextPending(view.Layer);
            }
        }

        private void CloseChildren(UIView view)
        {
            if (!_childrenByView.TryGetValue(view, out List<UIBrick> children))
            {
                return;
            }

            for (int i = children.Count - 1; i >= 0; i--)
            {
                UIBrick child = children[i];
                children.RemoveAt(i);
                _parentOfBrick.Remove(child);

                if (child is UIView subView)
                {
                    CloseInternal(subView);
                }
                else
                {
                    CloseBrickSafely(child);
                }
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

        private UIView InstantiateView(Type viewType, UIViewData data, Transform parent, bool parentExplicit)
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

            if (!parentExplicit)
            {
                parent = GetLayerRoot(view.Layer);
            }

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

            view.Data = data;
            view.Create(entity);
            if (!view.IsBound)
            {
                UnityEngine.Object.Destroy(entity.gameObject);
                Log.Error($"[UIManager] 界面 {viewType.Name} 绑定失败。");
                return null;
            }

            return view;
        }

        private void AddChild(UIView parent, UIBrick child)
        {
            if (!_childrenByView.TryGetValue(parent, out List<UIBrick> children))
            {
                children = new List<UIBrick>();
                _childrenByView[parent] = children;
            }

            children.Add(child);
            _parentOfBrick[child] = parent;
        }

        private void RemoveNavigationNode(UIView view)
        {
            for (int i = 0; i < _navigationStack.Count; i++)
            {
                if (_navigationStack[i].View == view)
                {
                    if (_navigationStack[i].IsJump)
                    {
                        CurrentJumpDepth = Math.Max(0, CurrentJumpDepth - 1);
                    }

                    _navigationStack.RemoveAt(i);
                    return;
                }
            }
        }

        private Type ResolveViewType(Type dataType)
        {
            if (_viewTypeByDataType.TryGetValue(dataType, out Type viewType))
            {
                return viewType;
            }

            Log.Error($"[UIManager] 未注册数据类型 {dataType.Name} 对应的界面，请先调用 RegisterView。");
            return null;
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

        private Transform GetLayerRoot(UILayer layer)
        {
            if (_layerRoots.TryGetValue(layer, out Transform root))
            {
                return root;
            }

            GameObject go = new GameObject($"Layer_{layer}");
            root = go.transform;
            if (_uiRoot != null)
            {
                root.SetParent(_uiRoot, false);
            }

            _layerRoots[layer] = root;
            SortLayerRoots();
            return root;
        }

        private void SortLayerRoots()
        {
            List<UILayer> layers = new List<UILayer>(_layerRoots.Keys);
            layers.Sort((a, b) => a.CompareTo(b));
            for (int i = 0; i < layers.Count; i++)
            {
                _layerRoots[layers[i]].SetSiblingIndex(i);
            }
        }

        private Transform ResolveAttachPoint(UIView parent, string containerPath)
        {
            if (string.IsNullOrEmpty(containerPath))
            {
                return parent.Transform;
            }

            Transform container = parent.Transform.Find(containerPath);
            if (container == null)
            {
                Log.Warning($"[UIManager] 未找到父界面 {parent.GetType().Name} 下的容器 {containerPath}，改用父节点挂载。");
                return parent.Transform;
            }

            return container;
        }

        private void RefreshLayers()
        {
            PruneDestroyedViews();

            foreach (KeyValuePair<UILayer, UILayerStack> kv in _layerStacks)
            {
                UILayerStack stack = kv.Value;
                bool layerVisible = IsLayerVisible(kv.Key);
                int count = stack.ViewCount;

                for (int i = 0; i < count; i++)
                {
                    UIView view = stack.GetView(i);
                    bool shouldShow = layerVisible && i == count - 1;
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

        private bool IsLayerVisible(UILayer layer)
        {
            UILayerStack stack = GetLayerStackOrNull(layer);
            if (stack == null || stack.IsEmpty)
            {
                return false;
            }

            // 不遮盖层始终可见
            if (_nonCoveringLayers.Contains(layer))
            {
                return true;
            }

            // 上方存在非空且遮盖的层级时，本层被遮盖
            foreach (KeyValuePair<UILayer, UILayerStack> kv in _layerStacks)
            {
                if (kv.Key > layer && !kv.Value.IsEmpty && !_nonCoveringLayers.Contains(kv.Key))
                {
                    return false;
                }
            }

            return true;
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
