using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EFramework.Extensions.UI.VirtualList
{
    public enum QVirtualListDirection
    {
        Vertical,
        Horizontal
    }

    public enum QVirtualListAlign
    {
        Start,
        Center,
        End
    }

    /// <summary>
    /// Virtualized single-axis list with pooled item views and variable item sizes.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScrollRect))]
    public class QVirtualListView : MonoBehaviour
    {
        [SerializeField] private QVirtualListDirection m_direction = QVirtualListDirection.Vertical;
        [SerializeField] private bool m_useScrollRectDirection = true;
        [SerializeField] private bool m_stretchCrossAxis = true;
        [SerializeField] private bool m_resetPositionOnReload = true;
        [SerializeField] private float m_spacing;
        [SerializeField] private float m_overscan = 100f;
        [SerializeField] private RectOffset m_padding = new();

        private readonly Dictionary<int, QVirtualListItem> m_visibleItems = new();
        private readonly List<float> m_itemStarts = new();
        private readonly List<float> m_itemSizes = new();

        private ScrollRect m_scrollRect;
        private RectTransform m_content;
        private RectTransform m_viewport;
        private QVirtualItemPool m_pool;
        private IQVirtualListAdapter m_adapter;
        private float m_contentLength;
        private bool m_initialized;
        private bool m_ignoreScrollEvent;
        private bool m_deferredRefreshQueued;
        private int m_deferredRefreshAttempts;

        public ScrollRect ScrollRect
        {
            get
            {
                EnsureInitialized();
                return m_scrollRect;
            }
        }

        public int Count => m_adapter != null ? Mathf.Max(0, m_adapter.Count) : 0;

        public QVirtualListDirection Direction
        {
            get
            {
                if (m_useScrollRectDirection && m_scrollRect != null)
                {
                    if (m_scrollRect.horizontal && !m_scrollRect.vertical)
                    {
                        return QVirtualListDirection.Horizontal;
                    }

                    if (m_scrollRect.vertical && !m_scrollRect.horizontal)
                    {
                        return QVirtualListDirection.Vertical;
                    }
                }

                return m_direction;
            }
        }

        private bool IsVertical => Direction == QVirtualListDirection.Vertical;

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
            m_deferredRefreshQueued = false;

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
                RefreshLayout();
            }
        }

        public void SetAdapter(IQVirtualListAdapter adapter, bool reload = true)
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
            RebuildLayoutCache();
            SetContentLength(m_contentLength);
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

            var viewportLength = GetViewportLength();
            if (viewportLength <= 0.01f)
            {
                RequestDeferredRefresh();
                return;
            }

            m_deferredRefreshAttempts = 0;
            var scrollOffset = GetScrollOffset();
            var min = Mathf.Max(0f, scrollOffset - Mathf.Max(0f, m_overscan));
            var max = Mathf.Min(m_contentLength, scrollOffset + viewportLength + Mathf.Max(0f, m_overscan));
            var firstIndex = FindFirstVisibleIndex(min);
            var lastIndex = FindLastVisibleIndex(max);

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

        public void RefreshLayout()
        {
            EnsureInitialized();
            if (!m_initialized)
            {
                return;
            }

            var offset = GetScrollOffset();
            SetContentLength(m_contentLength);
            SetScrollOffset(offset);
            RefreshVisibleItems();
        }

        public void ScrollToIndex(int index, QVirtualListAlign align = QVirtualListAlign.Start)
        {
            if (!IsValidIndex(index))
            {
                return;
            }

            var itemStart = m_itemStarts[index];
            var itemSize = m_itemSizes[index];
            var viewportLength = GetViewportLength();
            var targetOffset = align switch
            {
                QVirtualListAlign.Center => itemStart - (viewportLength - itemSize) * 0.5f,
                QVirtualListAlign.End => itemStart + itemSize - viewportLength,
                _ => itemStart
            };

            SetScrollOffset(targetOffset);
            RefreshVisibleItems();
        }

        public void Clear(bool destroyPooledItems = false)
        {
            RecycleAllVisibleItems();
            m_itemStarts.Clear();
            m_itemSizes.Clear();
            m_contentLength = 0f;

            if (m_content != null)
            {
                SetContentLength(0f);
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
                Debug.LogError($"{nameof(QVirtualListView)} requires ScrollRect.content.");
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
                m_content.anchorMax = new Vector2(1f, 1f);
                m_content.pivot = new Vector2(0.5f, 1f);
            }
            else
            {
                m_scrollRect.vertical = false;
                m_scrollRect.horizontal = true;
                m_content.anchorMin = new Vector2(0f, 0f);
                m_content.anchorMax = new Vector2(0f, 1f);
                m_content.pivot = new Vector2(0f, 0.5f);
            }
        }

        private void RebuildLayoutCache()
        {
            m_itemStarts.Clear();
            m_itemSizes.Clear();
            m_contentLength = GetStartPadding();

            if (m_adapter == null)
            {
                m_contentLength += GetEndPadding();
                return;
            }

            var count = Count;
            for (var i = 0; i < count; i++)
            {
                var itemSize = m_adapter.GetItemSize(i);
                var axisSize = Mathf.Max(0f, IsVertical ? itemSize.y : itemSize.x);
                m_itemStarts.Add(m_contentLength);
                m_itemSizes.Add(axisSize);
                m_contentLength += axisSize;
                if (i < count - 1)
                {
                    m_contentLength += Mathf.Max(0f, m_spacing);
                }
            }

            m_contentLength += GetEndPadding();
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
            var adapterSize = m_adapter.GetItemSize(index);
            var start = m_itemStarts[index];

            if (IsVertical)
            {
                rectTransform.pivot = new Vector2(0.5f, 1f);
                if (m_stretchCrossAxis)
                {
                    rectTransform.anchorMin = new Vector2(0f, 1f);
                    rectTransform.anchorMax = new Vector2(1f, 1f);
                    rectTransform.sizeDelta = new Vector2(0f, m_itemSizes[index]);
                }
                else
                {
                    rectTransform.anchorMin = new Vector2(0.5f, 1f);
                    rectTransform.anchorMax = new Vector2(0.5f, 1f);
                    rectTransform.sizeDelta = new Vector2(adapterSize.x, m_itemSizes[index]);
                }

                rectTransform.anchoredPosition = new Vector2(0f, -start);
            }
            else
            {
                rectTransform.pivot = new Vector2(0f, 0.5f);
                if (m_stretchCrossAxis)
                {
                    rectTransform.anchorMin = new Vector2(0f, 0f);
                    rectTransform.anchorMax = new Vector2(0f, 1f);
                    rectTransform.sizeDelta = new Vector2(m_itemSizes[index], 0f);
                }
                else
                {
                    rectTransform.anchorMin = new Vector2(0f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0f, 0.5f);
                    rectTransform.sizeDelta = new Vector2(m_itemSizes[index], adapterSize.y);
                }

                rectTransform.anchoredPosition = new Vector2(start, 0f);
            }
        }

        private void SetContentLength(float length)
        {
            if (IsVertical)
            {
                m_content.sizeDelta = new Vector2(m_content.sizeDelta.x, Mathf.Max(0f, length));
            }
            else
            {
                m_content.sizeDelta = new Vector2(Mathf.Max(0f, length), m_content.sizeDelta.y);
            }
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

        private void RequestDeferredRefresh()
        {
            if (m_deferredRefreshQueued || !isActiveAndEnabled || m_deferredRefreshAttempts >= 8)
            {
                return;
            }

            m_deferredRefreshQueued = true;
            m_deferredRefreshAttempts++;
            StartCoroutine(RefreshAfterLayoutPass());
        }

        private IEnumerator RefreshAfterLayoutPass()
        {
            yield return null;

            if (!isActiveAndEnabled)
            {
                yield break;
            }

            Canvas.ForceUpdateCanvases();
            m_deferredRefreshQueued = false;
            RefreshLayout();
        }

        private float GetStartPadding()
        {
            return IsVertical ? m_padding.top : m_padding.left;
        }

        private float GetEndPadding()
        {
            return IsVertical ? m_padding.bottom : m_padding.right;
        }

        private int FindFirstVisibleIndex(float visibleStart)
        {
            var low = 0;
            var high = m_itemStarts.Count - 1;
            var result = -1;

            while (low <= high)
            {
                var mid = (low + high) / 2;
                var itemEnd = m_itemStarts[mid] + m_itemSizes[mid];
                if (itemEnd >= visibleStart)
                {
                    result = mid;
                    high = mid - 1;
                }
                else
                {
                    low = mid + 1;
                }
            }

            return result;
        }

        private int FindLastVisibleIndex(float visibleEnd)
        {
            var low = 0;
            var high = m_itemStarts.Count - 1;
            var result = -1;

            while (low <= high)
            {
                var mid = (low + high) / 2;
                if (m_itemStarts[mid] <= visibleEnd)
                {
                    result = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return result;
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < Count && index < m_itemStarts.Count;
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
