using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 固定尺寸的居中网格。使用 Inspector 模板创建 cell，布局由属性变更或显式接口驱动。
    /// Horizontal 每行从左到右，Vertical 每列从上到下；不足一行/列时仍居中。
    /// 此组件独占 Content 和 cell 的尺寸、锚点及位置，不要同时挂其他布局组件。
    /// </summary>
    [AddComponentMenu("UI/UIGrid")]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class UIGrid : MonoBehaviour
    {
        public enum GridAxis
        {
            Horizontal,
            Vertical
        }

        [SerializeField] private GridAxis _axis;
        [SerializeField] private bool _reverse;
        [SerializeField, Min(1)] private int _cellsPerLine = 1;
        [SerializeField] private Vector2 _cellSize = new Vector2(100f, 100f);
        [SerializeField] private Vector2 _spacing = new Vector2(10f, 10f);
        [SerializeField] private RectTransform _cellPrefab = null;

        // 当前使用的子节点，布局不依赖其激活状态。
        protected readonly List<RectTransform> _children = new List<RectTransform>();
        // 仅保存已回收且隐藏的子节点。
        protected readonly List<RectTransform> _cachedChildren = new List<RectTransform>();
        private RectTransform _content;
        protected int _itemCount;

        /// <summary>SetItemCount 为每个有效 cell 绑定一次；循环网格还会在滚动复用时绑定。</summary>
        /// <remarks>回调异常直接向上传递，不支持回调中重入 SetItemCount。</remarks>
        public event Action<RectTransform, int> BindCell;

        public RectTransform CellPrefab => _cellPrefab;
        public virtual int ItemCount => _itemCount;

        protected virtual void Awake()
        {
            if (CellPrefab == null)
            {
                throw new InvalidOperationException("CellPrefab 不能为空。");
            }
            if (CellPrefab)
            {
                CellPrefab.gameObject.SetActive(false);
            }
        }

        /// <summary>回收已有 cell，优先复用，不足时实例化。相同数量也重新绑定，多余实例停用缓存。</summary>
        public virtual void SetItemCount(int itemCount)
        {
            if (itemCount < 0)
            {
                return;
            }
            _itemCount = itemCount;
            RecycleAllCells();
            ResizeContent(ItemCount, 0f);
            for (int i = 0; i < ItemCount; i++)
            {
                RectTransform cell = GetCell();
                PositionCell(cell, i, ItemCount);
                cell.SetAsLastSibling();
                cell.gameObject.SetActive(true);
                BindCell?.Invoke(cell, i);
            }
        }

        /// <summary>优先取隐藏缓存，不足时实例化；返回的 cell 保持隐藏，由调用方定位和显示。</summary>
        protected RectTransform GetCell()
        {
            RectTransform cell = null;
            while (_cachedChildren.Count > 0 && cell == null)
            {
                int index = _cachedChildren.Count - 1;
                cell = _cachedChildren[index];
                _cachedChildren.RemoveAt(index);
            }
            if (cell == null)
            {
                cell = Instantiate(CellPrefab, Content, false);
                cell.gameObject.SetActive(false);
            }
            _children.Add(cell);
            return cell;
        }

        /// <summary>回收使用中的 cell，隐藏后放回缓存，不销毁实例。</summary>
        protected void RecycleCell(RectTransform cell)
        {
            if (!_children.Remove(cell) || cell == null)
            {
                return;
            }
            _cachedChildren.Add(cell);
            cell.gameObject.SetActive(false);
        }

        /// <summary>回收当前全部 cell，供重新设置数量时复用。</summary>
        protected void RecycleAllCells()
        {
            while (_children.Count > 0)
            {
                RecycleCell(_children[_children.Count - 1]);
            }
        }

        /// <summary>派生循环网格在索引变化时使用同一绑定事件。</summary>
        protected void InvokeBinding(RectTransform cell, int index)
        {
            BindCell?.Invoke(cell, index);
        }

        /// <summary>Horizontal 按行扩展，Vertical 按列扩展。</summary>
        public GridAxis Axis
        {
            get => _axis;
            set
            {
                if (_axis != value)
                {
                    _axis = value;
                    SetDirty();
                }
            }
        }

        /// <summary>Horizontal 为从下往上，Vertical 为从右往左；默认从上往下/从左往右。</summary>
        public bool Reverse
        {
            get => _reverse;
            set
            {
                if (_reverse != value)
                {
                    _reverse = value;
                    SetDirty();
                }
            }
        }

        /// <summary>每行的列数或每列的行数。</summary>
        public int CellsPerLine
        {
            get => Mathf.Max(1, _cellsPerLine);
            set
            {
                value = Mathf.Max(1, value);
                if (_cellsPerLine != value)
                {
                    _cellsPerLine = value;
                    SetDirty();
                }
            }
        }

        /// <summary>单个 cell 的宽、高。</summary>
        public Vector2 CellSize
        {
            get => new Vector2(Mathf.Max(0.01f, _cellSize.x), Mathf.Max(0.01f, _cellSize.y));
            set
            {
                value = new Vector2(Mathf.Max(0.01f, value.x), Mathf.Max(0.01f, value.y));
                if (_cellSize != value)
                {
                    _cellSize = value;
                    SetDirty();
                }
            }
        }

        /// <summary>X 为左右间距，Y 为上下间距，不包含外边距。</summary>
        public Vector2 Spacing
        {
            get => new Vector2(Mathf.Max(0f, _spacing.x), Mathf.Max(0f, _spacing.y));
            set
            {
                value = new Vector2(Mathf.Max(0f, value.x), Mathf.Max(0f, value.y));
                if (_spacing != value)
                {
                    _spacing = value;
                    SetDirty();
                }
            }
        }

        protected RectTransform Content
        {
            get
            {
                if (_content == null)
                {
                    _content = (RectTransform)transform;
                }
                return _content;
            }
        }

        protected bool IsHorizontal => Axis == GridAxis.Horizontal;
        protected float LineSize => IsHorizontal ? CellSize.y : CellSize.x;
        protected float LineStride => LineSize + (IsHorizontal ? Spacing.y : Spacing.x);

        protected virtual void OnValidate()
        {
            _cellsPerLine = Mathf.Max(1, _cellsPerLine);
            _itemCount = Mathf.Max(0, _itemCount);
            _cellSize = CellSize;
            _spacing = Spacing;
            // OnValidate 不执行实例化或销毁，Inspector 修改后显式调用 RefreshPositions。
        }

        /// <summary>立即通知布局变化；无逐帧检测。</summary>
        public virtual void SetDirty()
        {
            RefreshPositions();
        }

        /// <summary>立即更新 Content 尺寸和 cell 位置，不重复绑定已有 cell。</summary>
        public virtual void RefreshPositions()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }
            ResizeContent(ItemCount, 0f);
            for (int i = 0; i < _children.Count; i++)
            {
                PositionCell(_children[i], i, ItemCount);
            }
        }

        protected int GetLineCount(int itemCount)
        {
            return itemCount == 0 ? 0 : (itemCount - 1) / CellsPerLine + 1;
        }

        protected void ResizeContent(int itemCount, float minimumCrossSize)
        {
            Vector2 origin = IsHorizontal
                ? new Vector2(0.5f, Reverse ? 0f : 1f)
                : new Vector2(Reverse ? 1f : 0f, 0.5f);
            Content.anchorMin = origin;
            Content.anchorMax = origin;
            Content.pivot = origin;
            int lines = GetLineCount(itemCount);
            int crossCount = Mathf.Min(itemCount, CellsPerLine);
            float length = lines == 0 ? 0f : lines * LineStride - (LineStride - LineSize);
            float crossCell = IsHorizontal ? CellSize.x : CellSize.y;
            float crossSpacing = IsHorizontal ? Spacing.x : Spacing.y;
            float cross = crossCount == 0 ? 0f : crossCount * (crossCell + crossSpacing) - crossSpacing;
            cross = Mathf.Max(cross, minimumCrossSize);
            Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, IsHorizontal ? cross : length);
            Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, IsHorizontal ? length : cross);
        }

        /// <summary>获得以 Content 起始边中点为原点的 cell 中心坐标。</summary>
        public Vector2 GetCellPosition(int index, int itemCount)
        {
            if (index < 0 || index >= itemCount)
            {
                return Vector2.zero;
            }
            int line = index / CellsPerLine;
            int offset = index % CellsPerLine;
            int count = Mathf.Min(CellsPerLine, itemCount - line * CellsPerLine);
            float crossStride = IsHorizontal ? CellSize.x + Spacing.x : CellSize.y + Spacing.y;
            float cross = (offset - (count - 1) * 0.5f) * crossStride;
            float along = LineSize * 0.5f + line * LineStride;
            return IsHorizontal
                ? new Vector2(cross, Reverse ? along : -along)
                : new Vector2(Reverse ? -along : along, -cross);
        }

        protected void PositionCell(RectTransform cell, int index, int itemCount)
        {
            if (!cell)
            {
                return;
            }
            cell.anchorMin = Content.pivot;
            cell.anchorMax = Content.pivot;
            cell.pivot = new Vector2(0.5f, 0.5f);
            cell.sizeDelta = CellSize;
            cell.anchoredPosition3D = GetCellPosition(index, itemCount);
        }
    }
}
