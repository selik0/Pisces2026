using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// UIWidget 独立管理器。
    /// 通过类型和预制体打开的 Widget 在关闭后缓存实体；通过已有界面节点打开的 Widget 只绑定该节点，不进入缓存。
    /// </summary>
    public sealed class UIWidgetManager : Singleton<UIWidgetManager>, ILogin
    {
        private sealed class PooledEntity
        {
            public UIEntity Entity;
            public float IdleSeconds;
        }

        private readonly Dictionary<Type, Stack<PooledEntity>> _poolByType = new Dictionary<Type, Stack<PooledEntity>>();
        private readonly HashSet<UIWidget> _openedWidgets = new HashSet<UIWidget>();
        private IUIViewLoader _loader = new DefaultUIViewLoader();
        private Transform _poolRoot;

        /// <summary>缓存实体空闲多少秒后释放，默认 60 秒。</summary>
        public float CacheLifetimeSeconds { get; set; } = 60f;

        /// <summary>设置 Widget 预制体加载器。</summary>
        public void SetLoader(IUIViewLoader loader)
        {
            if (loader == null)
            {
                Log.Error("[UIWidgetManager] SetLoader 失败：loader 为 null。");
                return;
            }

            _loader = loader;
        }

        /// <summary>
        /// 按类型在父界面指定节点下打开 Widget。关闭后实体进入对应类型的对象池。
        /// </summary>
        public TWidget Open<TWidget>(UIView parentView, string containerPath = null) where TWidget : UIWidget, new()
        {
            if (parentView == null || !parentView.IsBound)
            {
                Log.Error("[UIWidgetManager] Open 失败：父界面为空或未绑定。");
                return null;
            }

            Transform parent = parentView.Transform;
            if (!string.IsNullOrEmpty(containerPath))
            {
                Transform container = parent.Find(containerPath);
                if (container != null)
                {
                    parent = container;
                }
                else
                {
                    Log.Warning($"[UIWidgetManager] 未找到父界面 {parentView.GetType().Name} 下的节点 {containerPath}，改用父界面根节点。");
                }
            }

            return OpenInternal<TWidget>(parent, parentView);
        }

        /// <summary>按类型在父 Widget 指定节点下打开子 Widget。</summary>
        public TWidget Open<TWidget>(UIWidget parentWidget, string containerPath = null) where TWidget : UIWidget, new()
        {
            if (parentWidget == null || !parentWidget.IsBound || !_openedWidgets.Contains(parentWidget))
            {
                Log.Error("[UIWidgetManager] Open 失败：父 Widget 为空、未绑定或未由当前管理器打开。");
                return null;
            }

            Transform parent = ResolveContainer(parentWidget.Transform, containerPath, parentWidget.GetType().Name);
            return OpenInternal<TWidget>(parent, parentWidget);
        }

        /// <summary>
        /// 按类型实例化并打开 Widget。关闭后实体进入对应类型的对象池，并在空闲 60 秒后释放。
        /// </summary>
        public TWidget Open<TWidget>(Transform parent) where TWidget : UIWidget, new()
        {
            return OpenInternal<TWidget>(parent, null);
        }

        private TWidget OpenInternal<TWidget>(Transform parent, UIBrick logicalParent) where TWidget : UIWidget, new()
        {
            if (parent == null)
            {
                Log.Error("[UIWidgetManager] Open 失败：parent 为 null。");
                return null;
            }

            TWidget widget = new TWidget();
            if (string.IsNullOrEmpty(widget.PrefabPath))
            {
                Log.Error($"[UIWidgetManager] Widget {typeof(TWidget).Name} 未配置 PrefabPath。");
                return null;
            }

            UIEntity entity = TakeFromPool(typeof(TWidget), parent);
            if (entity == null)
            {
                entity = _loader.Instantiate(widget.PrefabPath, parent);
            }

            if (entity == null)
            {
                Log.Error($"[UIWidgetManager] Widget {typeof(TWidget).Name} 实例化失败，预制体: {widget.PrefabPath}");
                return null;
            }

            widget.Parent = logicalParent;
            widget.IsPooled = true;
            if (!OpenWidget(widget, entity))
            {
                UnityEngine.Object.Destroy(entity.gameObject);
                return null;
            }

            return widget;
        }

        /// <summary>
        /// 使用已有界面节点打开 Widget。该模式不实例化预制体，关闭时也不会缓存或销毁传入节点。
        /// </summary>
        public TWidget Open<TWidget>(UIEntity entity, UIBrick logicalParent = null) where TWidget : UIWidget, new()
        {
            if (entity == null)
            {
                Log.Error("[UIWidgetManager] Open 失败：entity 为 null。");
                return null;
            }

            if (logicalParent != null && !(logicalParent is UIView) && !(logicalParent is UIWidget))
            {
                Log.Error("[UIWidgetManager] Open 失败：Widget 父级只能是 UIView 或 UIWidget。");
                return null;
            }

            if (logicalParent is UIWidget parentWidget && !_openedWidgets.Contains(parentWidget))
            {
                Log.Error("[UIWidgetManager] Open 失败：父 Widget 未由当前管理器打开。");
                return null;
            }

            TWidget widget = new TWidget
            {
                Parent = logicalParent,
                IsPooled = false
            };

            return OpenWidget(widget, entity) ? widget : null;
        }

        /// <summary>
        /// 关闭 Widget。池化实例归还对象池；节点实例仅解绑逻辑，不销毁节点。
        /// </summary>
        public bool Close(UIWidget widget)
        {
            if (widget == null || !_openedWidgets.Remove(widget))
            {
                return false;
            }

            CloseChildren(widget);

            Type widgetType = widget.GetType();
            UIEntity entity = widget.Entity;
            bool pooled = widget.IsPooled;
            widget.Closed -= OnWidgetClosed;
            widget.Parent = null;
            widget.ReleaseBinding();

            if (entity == null)
            {
                return true;
            }

            if (!pooled)
            {
                return true;
            }

            Transform poolRoot = GetPoolRoot();
            entity.transform.SetParent(poolRoot, false);
            entity.gameObject.SetActive(false);

            if (!_poolByType.TryGetValue(widgetType, out Stack<PooledEntity> pool))
            {
                pool = new Stack<PooledEntity>();
                _poolByType[widgetType] = pool;
            }

            pool.Push(new PooledEntity
            {
                Entity = entity,
                IdleSeconds = 0f
            });
            return true;
        }

        /// <summary>关闭属于指定父界面的全部 Widget，包括嵌套子 Widget。</summary>
        public void CloseByParent(UIView parentView)
        {
            if (parentView == null || _openedWidgets.Count == 0)
            {
                return;
            }

            List<UIWidget> widgets = new List<UIWidget>();
            foreach (UIWidget widget in _openedWidgets)
            {
                if (widget.Parent == parentView)
                {
                    widgets.Add(widget);
                }
            }

            for (int i = 0; i < widgets.Count; i++)
            {
                Close(widgets[i]);
            }
        }

        private void CloseChildren(UIWidget parentWidget)
        {
            List<UIWidget> children = new List<UIWidget>();
            foreach (UIWidget widget in _openedWidgets)
            {
                if (widget.Parent == parentWidget)
                {
                    children.Add(widget);
                }
            }

            for (int i = 0; i < children.Count; i++)
            {
                Close(children[i]);
            }
        }

        /// <summary>推进缓存过期计时，应使用不受 Time.timeScale 影响的 deltaTime。</summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || _poolByType.Count == 0)
            {
                return;
            }

            List<Type> emptyTypes = null;
            foreach (KeyValuePair<Type, Stack<PooledEntity>> pair in _poolByType)
            {
                Stack<PooledEntity> source = pair.Value;
                Stack<PooledEntity> retained = new Stack<PooledEntity>(source.Count);
                while (source.Count > 0)
                {
                    PooledEntity item = source.Pop();
                    item.IdleSeconds += deltaTime;
                    if (item.Entity == null || item.IdleSeconds >= CacheLifetimeSeconds)
                    {
                        if (item.Entity != null)
                        {
                            UnityEngine.Object.Destroy(item.Entity.gameObject);
                        }
                    }
                    else
                    {
                        retained.Push(item);
                    }
                }

                while (retained.Count > 0)
                {
                    source.Push(retained.Pop());
                }

                if (source.Count == 0)
                {
                    if (emptyTypes == null)
                    {
                        emptyTypes = new List<Type>();
                    }

                    emptyTypes.Add(pair.Key);
                }
            }

            if (emptyTypes != null)
            {
                for (int i = 0; i < emptyTypes.Count; i++)
                {
                    _poolByType.Remove(emptyTypes[i]);
                }
            }
        }

        /// <summary>关闭全部打开中的 Widget，并释放全部缓存实体。</summary>
        public void DestroyAll()
        {
            UIWidget[] widgets = new UIWidget[_openedWidgets.Count];
            _openedWidgets.CopyTo(widgets);
            for (int i = 0; i < widgets.Length; i++)
            {
                Close(widgets[i]);
            }

            foreach (Stack<PooledEntity> pool in _poolByType.Values)
            {
                while (pool.Count > 0)
                {
                    UIEntity entity = pool.Pop().Entity;
                    if (entity != null)
                    {
                        UnityEngine.Object.Destroy(entity.gameObject);
                    }
                }
            }

            _poolByType.Clear();
            if (_poolRoot != null)
            {
                UnityEngine.Object.Destroy(_poolRoot.gameObject);
                _poolRoot = null;
            }
        }

        public void Login()
        {
            DestroyAll();
        }

        public void Logout()
        {
            DestroyAll();
        }

        private bool OpenWidget(UIWidget widget, UIEntity entity)
        {
            widget.Create(entity);
            if (!widget.IsBound)
            {
                return false;
            }

            _openedWidgets.Add(widget);
            widget.Closed += OnWidgetClosed;
            widget.Open();
            return true;
        }

        private void OnWidgetClosed()
        {
            UIWidget closedWidget = null;
            foreach (UIWidget widget in _openedWidgets)
            {
                if (widget.State == UIBrickState.Closed)
                {
                    closedWidget = widget;
                    break;
                }
            }

            if (closedWidget != null)
            {
                Close(closedWidget);
            }
        }

        private Transform ResolveContainer(Transform parent, string containerPath, string ownerName)
        {
            if (string.IsNullOrEmpty(containerPath))
            {
                return parent;
            }

            Transform container = parent.Find(containerPath);
            if (container != null)
            {
                return container;
            }

            Log.Warning($"[UIWidgetManager] 未找到 {ownerName} 下的节点 {containerPath}，改用父级根节点。");
            return parent;
        }

        private UIEntity TakeFromPool(Type widgetType, Transform parent)
        {
            if (!_poolByType.TryGetValue(widgetType, out Stack<PooledEntity> pool))
            {
                return null;
            }

            while (pool.Count > 0)
            {
                UIEntity entity = pool.Pop().Entity;
                if (entity == null)
                {
                    continue;
                }

                entity.transform.SetParent(parent, false);
                entity.gameObject.SetActive(true);
                if (pool.Count == 0)
                {
                    _poolByType.Remove(widgetType);
                }

                return entity;
            }

            _poolByType.Remove(widgetType);
            return null;
        }

        private Transform GetPoolRoot()
        {
            if (_poolRoot != null)
            {
                return _poolRoot;
            }

            GameObject poolObject = new GameObject("UIWidgetPool");
            UnityEngine.Object.DontDestroyOnLoad(poolObject);
            poolObject.SetActive(false);
            _poolRoot = poolObject.transform;
            return _poolRoot;
        }
    }
}
