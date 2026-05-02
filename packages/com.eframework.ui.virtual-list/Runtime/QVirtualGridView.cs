using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFramework.Extensions.UI.VirtualList
{
    public enum QVirtualGridDirection
    {
        Vertical,
        Horizontal
    }

    /// <summary>
    /// Virtualized fixed-size grid with pooled item views.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScrollRect))]
    public class QVirtualGridView : MonoBehaviour
    {
        [SerializeField] private QVirtualGridDirection m_direction = QVirtualGridDirection.Vertical;
        [SerializeField] private bool m_useScrollRectDirection = true;
        [SerializeField] private bool m_resetPositionOnReload = true;
        [SerializeField] private Vector2 m_cellSize = new(100f, 100f);
        [SerializeField] private Vector2 m_spacing;
        [SerializeField] private int m_constraintCount = 1;
        [SerializeField] private float m_overscan = 100f;
        [SerializeField] private RectOffset m_padding = new();

        private readonly Dictionary<int, QVirtualListItem> m_visibleItems = new();

        private ScrollRect m_scrollRect;
        private RectTransform m_content;
        private RectTransform m_viewport;
        private QVirtualItemPool m_pool;
        private IQVirtualItemAdapter m_adapter;
        private float m_contentLength;
        private bool m_initialized;
        private bool m_ignoreScrollEvent;

        public ScrollRect ScrollRect
        {
            get
            {
                EnsureInitialized();
                return m_scrollRect;
            }
        }

        public int Count => m_adapter != null ? Mathf.Max(0, m_adapter.Count) : 0;

        public QVirtualGridDirection Direction
        {
            get
            {
                if (m_useScrollRectDirection && m_scrollRect != null)
                {
                    if (m_scrollRect.horizontal && !m_scrollRect.vertical)
                    {
                        return QVirtualGridDirection.Horizontal;
                    }

                    if (m_scrollRect.vertical && !m_scrollRect.horizontal)
                    {
                        return QVirtualGridDirection.Vertical;
                    }
                }

                return m_direction;
            }
        }

        private bool IsVertical => Direction == QVirtualGridDirection.Vertical;
        private int ConstraintCount => Mathf.Max(1, m_constraintCount);
        private float MainCellSize => IsVertical ? Mathf.Max(0f, m_cellSize.y) : Mathf.Max(0f, m_cellSize.x);
        private float MainSpacing => IsVertical ? Mathf.Max(0f, m_spacing.y) : Mathf.Max(0f, m_spacing.x);
        private float CrossCellSize => IsVertical ? Mathf.Max(0f, m_cellSize.x) : Mathf.Max(0f, m_cellSize.y);
        private float CrossSpacing => IsVertical ? Mathf.Max(0f, m_spacing.x) : Mathf.Max(0f, m_spacing.y);

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            if (!m_initialized)
            {
                return;
            }

            m_scrollRect.onValueChanged.AddListener(OnScrollChanged);
            RefreshVisibleItems();
        }

        private void OnDisable()
        {
            if (m_scrollRect != null)
            {
                m_scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
            }
        }

        private void OnDestroy()
        {
            Clear(true);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (m_initialized && isActiveAndEnabled)
            {
                RefreshVisibleItems();
            }
        }

        public void SetAdapter(IQVirtualItemAdapter adapter, bool reload = true)
        {
            EnsureInitialized();
            m_adapter = adapter;
            if (reload)
            {
                Reload();
            }
        }

        public QVirtualListItem GetVisibleItem(int index)
        {
            return m_visibleItems.TryGetValue(index, out var item) ? item : null;
        }

        public bool IsItemVisible(int index)
        {
            return m_visibleItems.ContainsKey(index);
        }

        public Vector2Int GetVisibleRange()
        {
            if (m_visibleItems.Count == 0)
            {
                return new Vector2Int(-1, -1);
            }

            var min = int.MaxValue;
            var max = int.MinValue;
            foreach (var index in m_visibleItems.Keys)
            {
                if (index < min) min = index;
                if (index > max) max = index;
            }

            return new Vector2Int(min, max);
        }

        public void Reload()
        {
            EnsureInitialized();
            var offset = m_resetPositionOnReload ? 0f : GetScrollOffset();
            RecycleAllVisibleItems();
            RebuildContentSize();
            SetScrollOffset(offset);
            RefreshVisibleItems();
        }

        public void RefreshItem(int index)
        {
            if (m_adapter == null || !IsValidIndex(index))
            {
                return;
            }

            if (!m_visibleItems.TryGetValue(index, out var item))
            {
                return;
            }

            m_adapter.Unbind(item, index);
            item.MarkRecycled();
            item.MarkBound(index);
            m_adapter.Bind(item, index);
            ApplyItemTransform(item, index);
        }

        public void RefreshVisibleItems()
        {
            if (!m_initialized || m_adapter == null || Count == 0)
            {
                RecycleAllVisibleItems();
                return;
            }

            var firstLine = GetFirstVisibleLine();
            var lastLine = GetLastVisibleLine();
            var firstIndex = GetFirstIndexOnLine(firstLine);
            var lastIndex = Mathf.Min(Count - 1, GetLastIndexOnLine(lastLine));

            if (firstIndex < 0 || lastIndex < firstIndex)
            {
                RecycleAllVisibleItems();
                return;
            }

            RecycleOutOfRange(firstIndex, lastIndex);

            for (var i = firstIndex; i <= lastIndex; i++)
            {
                if (!m_visibleItems.ContainsKey(i))
                {
                    ShowItem(i);
                }
                else
                {
                    ApplyItemTransform(m_visibleItems[i], i);
                }
            }
        }

        public void ScrollToIndex(int index, QVirtualListAlign align = QVirtualListAlign.Start)
        {
            if (!IsValidIndex(index))
            {
                return;
            }

            var line = GetLine(index);
            var lineStart = GetStartPadding() + line * (MainCellSize + MainSpacing);
            var viewportLength = GetViewportLength();
            var targetOffset = align switch
            {
                QVirtualListAlign.Center => lineStart - (viewportLength - MainCellSize) * 0.5f,
                QVirtualListAlign.End => lineStart + MainCellSize - viewportLength,
                _ => lineStart
            };

            SetScrollOffset(targetOffset);
            RefreshVisibleItems();
        }

        public void Clear(bool destroyPooledItems = false)
        {
            RecycleAllVisibleItems();
            m_contentLength = 0f;
            if (m_content != null)
            {
                SetContentSize(0f, 0f);
            }

            if (destroyPooledItems && m_pool != null)
            {
                m_pool.DestroyAll();
                m_pool = null;
                m_initialized = false;
            }
        }

        private void EnsureInitialized()
        {
            if (m_initialized)
            {
                return;
            }

            m_scrollRect = GetComponent<ScrollRect>();
            m_content = m_scrollRect.content;
            m_viewport = m_scrollRect.viewport != null ? m_scrollRect.viewport : (RectTransform)m_scrollRect.transform;

            if (m_content == null)
            {
                Debug.LogError($"{nameof(QVirtualGridView)} requires ScrollRect.content.");
                return;
            }

            ConfigureContentTransform();
            m_pool = new QVirtualItemPool(transform, m_content);
            m_initialized = true;
        }

        private void ConfigureContentTransform()
        {
            if (IsVertical)
            {
                m_scrollRect.vertical = true;
                m_scrollRect.horizontal = false;
                m_content.anchorMin = new Vector2(0f, 1f);
                m_content.anchorMax = new Vector2(0f, 1f);
                m_content.pivot = new Vector2(0f, 1f);
            }
            else
            {
                m_scrollRect.vertical = false;
                m_scrollRect.horizontal = true;
                m_content.anchorMin = new Vector2(0f, 1f);
                m_content.anchorMax = new Vector2(0f, 1f);
                m_content.pivot = new Vector2(0f, 1f);
            }
        }

        private void RebuildContentSize()
        {
            var lineCount = GetLineCount();
            var crossCount = ConstraintCount;
            var mainLength = GetStartPadding() + GetEndPadding();
            if (lineCount > 0)
            {
                mainLength += lineCount * MainCellSize + (lineCount - 1) * MainSpacing;
            }

            var crossLength = GetCrossStartPadding() + GetCrossEndPadding();
            if (crossCount > 0)
            {
                crossLength += crossCount * CrossCellSize + (crossCount - 1) * CrossSpacing;
            }

            m_contentLength = Mathf.Max(0f, mainLength);
            if (IsVertical)
            {
                SetContentSize(crossLength, m_contentLength);
            }
            else
            {
                SetContentSize(m_contentLength, crossLength);
            }
        }

        private void ShowItem(int index)
        {
            var prefab = m_adapter.GetItemPrefab(index);
            var item = m_pool.Get(prefab);
            if (item == null)
            {
                return;
            }

            item.MarkBound(index);
            ApplyItemTransform(item, index);
            m_adapter.Bind(item, index);
            m_visibleItems[index] = item;
        }

        private void RecycleOutOfRange(int firstIndex, int lastIndex)
        {
            s_recycleBuffer.Clear();
            foreach (var pair in m_visibleItems)
            {
                if (pair.Key < firstIndex || pair.Key > lastIndex)
                {
                    s_recycleBuffer.Add(pair.Key);
                }
            }

            foreach (var index in s_recycleBuffer)
            {
                RecycleVisibleItem(index);
            }
        }

        private void RecycleAllVisibleItems()
        {
            if (m_visibleItems.Count == 0)
            {
                return;
            }

            s_recycleBuffer.Clear();
            foreach (var index in m_visibleItems.Keys)
            {
                s_recycleBuffer.Add(index);
            }

            foreach (var index in s_recycleBuffer)
            {
                RecycleVisibleItem(index);
            }
        }

        private void RecycleVisibleItem(int index)
        {
            if (!m_visibleItems.TryGetValue(index, out var item))
            {
                return;
            }

            if (m_adapter != null && index >= 0)
            {
                m_adapter.Unbind(item, index);
            }

            item.MarkRecycled();
            m_visibleItems.Remove(index);
            m_pool.Release(item);
        }

        private void ApplyItemTransform(QVirtualListItem item, int index)
        {
            var rectTransform = item.RectTransform;
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.sizeDelta = m_cellSize;

            var line = GetLine(index);
            var cross = GetCrossIndex(index);
            var mainPos = GetStartPadding() + line * (MainCellSize + MainSpacing);
            var crossPos = GetCrossStartPadding() + cross * (CrossCellSize + CrossSpacing);

            if (IsVertical)
            {
                rectTransform.anchoredPosition = new Vector2(crossPos, -mainPos);
            }
            else
            {
                rectTransform.anchoredPosition = new Vector2(mainPos, -crossPos);
            }
        }

        private int GetLineCount()
        {
            if (Count == 0)
            {
                return 0;
            }

            return Mathf.CeilToInt(Count / (float)ConstraintCount);
        }

        private int GetLine(int index)
        {
            return index / ConstraintCount;
        }

        private int GetCrossIndex(int index)
        {
            return index % ConstraintCount;
        }

        private int GetFirstVisibleLine()
        {
            var step = MainCellSize + MainSpacing;
            if (step <= 0f)
            {
                return 0;
            }

            var raw = (GetScrollOffset() - Mathf.Max(0f, m_overscan) - GetStartPadding()) / step;
            return Mathf.Clamp(Mathf.FloorToInt(raw), 0, Mathf.Max(0, GetLineCount() - 1));
        }

        private int GetLastVisibleLine()
        {
            var step = MainCellSize + MainSpacing;
            if (step <= 0f)
            {
                return Mathf.Max(0, GetLineCount() - 1);
            }

            var raw = (GetScrollOffset() + GetViewportLength() + Mathf.Max(0f, m_overscan) - GetStartPadding()) / step;
            return Mathf.Clamp(Mathf.FloorToInt(raw), 0, Mathf.Max(0, GetLineCount() - 1));
        }

        private int GetFirstIndexOnLine(int line)
        {
            return Mathf.Clamp(line * ConstraintCount, 0, Mathf.Max(0, Count - 1));
        }

        private int GetLastIndexOnLine(int line)
        {
            return Mathf.Clamp(line * ConstraintCount + ConstraintCount - 1, 0, Mathf.Max(0, Count - 1));
        }

        private void SetContentSize(float width, float height)
        {
            m_content.sizeDelta = new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height));
        }

        private float GetScrollOffset()
        {
            if (m_content == null)
            {
                return 0f;
            }

            var offset = IsVertical ? m_content.anchoredPosition.y : -m_content.anchoredPosition.x;
            return Mathf.Clamp(offset, 0f, GetMaxScrollOffset());
        }

        private void SetScrollOffset(float offset)
        {
            if (m_content == null)
            {
                return;
            }

            offset = Mathf.Clamp(offset, 0f, GetMaxScrollOffset());
            m_ignoreScrollEvent = true;
            if (IsVertical)
            {
                m_content.anchoredPosition = new Vector2(m_content.anchoredPosition.x, offset);
            }
            else
            {
                m_content.anchoredPosition = new Vector2(-offset, m_content.anchoredPosition.y);
            }

            m_ignoreScrollEvent = false;
        }

        private float GetMaxScrollOffset()
        {
            return Mathf.Max(0f, m_contentLength - GetViewportLength());
        }

        private float GetViewportLength()
        {
            if (m_viewport == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, IsVertical ? m_viewport.rect.height : m_viewport.rect.width);
        }

        private float GetStartPadding()
        {
            return IsVertical ? m_padding.top : m_padding.left;
        }

        private float GetEndPadding()
        {
            return IsVertical ? m_padding.bottom : m_padding.right;
        }

        private float GetCrossStartPadding()
        {
            return IsVertical ? m_padding.left : m_padding.top;
        }

        private float GetCrossEndPadding()
        {
            return IsVertical ? m_padding.right : m_padding.bottom;
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < Count;
        }

        private void OnScrollChanged(Vector2 position)
        {
            if (!m_ignoreScrollEvent)
            {
                RefreshVisibleItems();
            }
        }

        private static readonly List<int> s_recycleBuffer = new();
    }
}
