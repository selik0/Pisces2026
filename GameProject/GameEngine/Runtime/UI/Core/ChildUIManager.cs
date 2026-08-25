using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 父界面的子界面管理器。同一时刻只允许一个子界面处于显示状态；
    /// 打开另一子界面时会先完整关闭并销毁当前子界面。
    /// </summary>
    public sealed class ChildUIManager
    {
        private readonly UIView _owner;
        private readonly Func<IUIViewLoader> _loaderProvider;

        internal ChildUIManager(UIView owner, Func<IUIViewLoader> loaderProvider)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _loaderProvider = loaderProvider ?? throw new ArgumentNullException(nameof(loaderProvider));
        }

        /// <summary>当前正在显示的子界面。</summary>
        public UISubView Current { get; private set; }

        /// <summary>是否存在已打开的子界面。</summary>
        public bool HasCurrent => Current != null;

        /// <summary>
        /// 打开指定子界面。如果当前已经是该类型，则返回现有实例；否则先关闭当前实例再打开新实例。
        /// </summary>
        public TSubView Open<TSubView>(UIViewData data = null, string containerPath = null)
            where TSubView : UISubView, new()
        {
            if (Current is TSubView existing)
            {
                return existing;
            }

            Close();

            if (!_owner.IsBound || _owner.IsDestroyed)
            {
                Log.Error($"[ChildUIManager] 父界面 {_owner.GetType().Name} 未绑定或已销毁，无法打开子界面。");
                return null;
            }

            TSubView subView = new TSubView();
            if (string.IsNullOrEmpty(subView.PrefabPath))
            {
                Log.Error($"[ChildUIManager] 子界面 {typeof(TSubView).Name} 未配置 PrefabPath。");
                return null;
            }

            IUIViewLoader loader = _loaderProvider();
            if (loader == null)
            {
                Log.Error("[ChildUIManager] UI 加载器为空，无法打开子界面。");
                return null;
            }

            UIEntity entity = loader.Instantiate(subView.PrefabPath, ResolveAttachPoint(containerPath));
            if (entity == null)
            {
                Log.Error($"[ChildUIManager] 子界面 {typeof(TSubView).Name} 实例化失败，预制体: {subView.PrefabPath}");
                return null;
            }

            subView.ParentView = _owner;
            subView.Data = data;
            subView.Create(entity);
            if (!subView.IsBound)
            {
                UnityEngine.Object.Destroy(entity.gameObject);
                return null;
            }

            Current = subView;
            subView.Open();
            return subView;
        }

        /// <summary>强制切换到指定子界面，即使类型相同也会使用新数据重新创建。</summary>
        public TSubView Switch<TSubView>(UIViewData data = null, string containerPath = null)
            where TSubView : UISubView, new()
        {
            Close();
            return Open<TSubView>(data, containerPath);
        }

        /// <summary>关闭并销毁当前子界面。</summary>
        public bool Close()
        {
            UISubView subView = Current;
            if (subView == null)
            {
                return false;
            }

            Current = null;
            if (subView.State == UIBrickState.Created ||
                subView.State == UIBrickState.Opened ||
                subView.State == UIBrickState.Hiding)
            {
                subView.Close();
            }

            if (subView.GameObject != null)
            {
                UnityEngine.Object.Destroy(subView.GameObject);
            }

            return true;
        }

        /// <summary>当前子界面是否为指定类型。</summary>
        public bool IsCurrent<TSubView>() where TSubView : UISubView
        {
            return Current is TSubView;
        }

        private Transform ResolveAttachPoint(string containerPath)
        {
            if (string.IsNullOrEmpty(containerPath))
            {
                return _owner.Transform;
            }

            Transform container = _owner.Transform.Find(containerPath);
            if (container != null)
            {
                return container;
            }

            Log.Warning($"[ChildUIManager] 未找到父界面 {_owner.GetType().Name} 下的容器 {containerPath}，改用父节点挂载。");
            return _owner.Transform;
        }
    }
}
