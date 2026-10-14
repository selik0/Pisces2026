using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameEngine
{
    /// <summary>
    /// UIGrid 的虚拟化版本。挂在 ScrollRect.content 上，Viewport 必须是 Content 的祖先。
    /// 仅创建可见行/列加一行/列缓存，离屏对象停用并复用；Viewport 使用 RectMask2D 裁剪。
    /// cell 的数据绑定必须覆盖上一次数据，不应在绑定回调中持续添加事件监听器。
    /// 设置数量及滚动复用时绑定；多余实例停用缓存，保留历史所需的最大池容量。
    /// </summary>
    [AddComponentMenu("UI/UILoopGrid")]
    public class UILoopGrid : UIGrid
    {
        [SerializeField] private RectTransform _viewport = null;
        [SerializeField] private ScrollRect _scrollRect = null;

        private readonly Dictionary<int, RectTransform> _cells = new Dictionary<int, RectTransform>();
        private readonly List<int> _recycleIndices = new List<int>();
        private readonly Vector3[] _corners = new Vector3[4];
        private bool _layoutDirty = true;
        private bool _hasData;
        private int _firstVisibleLine = int.MinValue;
        private int _lastVisibleLine = int.MinValue;

        public RectTransform Viewport => _viewport;

        protected override void Awake()
        {
            base.Awake();
            if (_viewport == null)
            {
                throw new InvalidOperationException("Viewport 不能为空。");
            }
            if (_scrollRect == null)
            {
                throw new InvalidOperationException("ScrollRect 不能为空。");
            }
            ConfigureViewport();
            _scrollRect.onValueChanged.AddListener(OnScrollChanged);
        }

        /// <summary>
        /// 回收全部映射，复用已有实例并绑定当前可见区和缓存区的数据，不足时才实例化。
        /// </summary>
        public override void SetItemCount(int itemCount)
        {
            if (itemCount < 0)
            {
                return;
            }
            _itemCount = itemCount;
            _hasData = true;
            _layoutDirty = true;
            _cells.Clear();
            RecycleAllCells();
            UpdateCells();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _layoutDirty = true;
        }

        protected virtual void OnEnable()
        {
            _firstVisibleLine = int.MinValue;
            _lastVisibleLine = int.MinValue;
            RefreshVisibleCells();
        }

        protected virtual void OnDisable()
        {
            _firstVisibleLine = int.MinValue;
            _lastVisibleLine = int.MinValue;
            foreach (RectTransform cell in _children)
            {
                if (cell != null)
                {
                    cell.gameObject.SetActive(false);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            if (_scrollRect != null)
            {
                _scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
            }
        }

        private void OnScrollChanged(Vector2 position)
        {
            RefreshVisibleCells();
        }

        /// <summary>显式更新全部已有 cell 位置和 Content 尺寸。</summary>
        public override void RefreshPositions()
        {
            _layoutDirty = true;
            RefreshVisibleCells();
        }

        /// <summary>更新可见区。手动移动 Content 后调用；滚动事件自动调用，仅定位新建或复用的 cell。</summary>
        public void RefreshVisibleCells()
        {
            UpdateVisibleCells();
        }

        private void UpdateVisibleCells()
        {
            if (!_hasData || !isActiveAndEnabled)
            {
                return;
            }
            UpdateCells();
        }

        private void ConfigureViewport()
        {
            _scrollRect.content = Content;
            _scrollRect.viewport = _viewport;
            _scrollRect.horizontal = !IsHorizontal;
            _scrollRect.vertical = IsHorizontal;
        }

        private void UpdateCells()
        {
            bool layoutChanged = _layoutDirty;
            if (layoutChanged)
            {
                _layoutDirty = false;
                ConfigureViewport();
                ResizeContent(ItemCount, IsHorizontal ? _viewport.rect.width : _viewport.rect.height);
            }
            _viewport.GetWorldCorners(_corners);
            float minimum = float.PositiveInfinity;
            float maximum = float.NegativeInfinity;
            for (int i = 0; i < _corners.Length; i++)
            {
                Vector3 point = Content.InverseTransformPoint(_corners[i]);
                float distance = IsHorizontal ? (Reverse ? point.y : -point.y) : (Reverse ? -point.x : point.x);
                minimum = Mathf.Min(minimum, distance);
                maximum = Mathf.Max(maximum, distance);
            }

            int lineCount = GetLineCount(ItemCount);
            int firstVisible = Mathf.Max(0, Mathf.FloorToInt((minimum - LineSize) / LineStride) + 1);
            int lastVisible = Mathf.Min(lineCount - 1, Mathf.CeilToInt(maximum / LineStride) - 1);
            bool hasVisible = firstVisible <= lastVisible && _viewport.rect.width > 0f && _viewport.rect.height > 0f;
            if (!hasVisible)
            {
                firstVisible = -1;
                lastVisible = -1;
            }
            // 滚动未跨行时无需重建索引映射、扫描对象池或重复设置激活状态。
            if (!layoutChanged && firstVisible == _firstVisibleLine && lastVisible == _lastVisibleLine)
            {
                return;
            }
            _firstVisibleLine = int.MinValue;
            _lastVisibleLine = int.MinValue;
            int first = hasVisible ? firstVisible : 0;
            int last = hasVisible ? Mathf.Min(lineCount - 1, lastVisible + 1) : -1;
            if (hasVisible && last == lastVisible && first > 0)
            {
                first--;
            }
            int firstIndex = first * CellsPerLine;
            int endIndex = last < first ? 0 : (int)Math.Min((long)ItemCount, ((long)last + 1) * CellsPerLine);
            _recycleIndices.Clear();
            foreach (KeyValuePair<int, RectTransform> entry in _cells)
            {
                if (entry.Key < firstIndex || entry.Key >= endIndex || entry.Value == null)
                {
                    _recycleIndices.Add(entry.Key);
                }
            }
            foreach (int index in _recycleIndices)
            {
                RectTransform cell = _cells[index];
                _cells.Remove(index);
                RecycleCell(cell);
            }
            // 保留仍处于当前区间的 cell，只把离屏 cell 分配给新进入的索引。
            for (int index = firstIndex; index < endIndex; index++)
            {
                bool changed = !_cells.TryGetValue(index, out RectTransform cell);
                if (changed)
                {
                    cell = GetCell();
                }
                if (layoutChanged || changed)
                {
                    PositionCell(cell, index, ItemCount);
                }
                if (changed)
                {
                    InvokeBinding(cell, index);
                    // 绑定成功后才提交映射，异常后下次滚动可重试该索引。
                    _cells.Add(index, cell);
                }
                if (!isActiveAndEnabled)
                {
                    return;
                }
                int line = index / CellsPerLine;
                cell.gameObject.SetActive(line >= firstVisible && line <= lastVisible);
                if (!isActiveAndEnabled)
                {
                    return;
                }
            }
            _firstVisibleLine = firstVisible;
            _lastVisibleLine = lastVisible;
        }

    }
}
