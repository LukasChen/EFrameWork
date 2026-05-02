// Created by lorin
// 2024/08/08 7:03 PM

using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace EFramework.Runtime.UI.Components.XListView
{
    [RequireComponent(typeof(ScrollRect))]
    public class XListView : MonoBehaviour
    {
        private XListViewItemPool m_Pool;
        private ScrollRect m_ScrollRect;
        private Transform m_Content;
        private RectTransform m_ContentRectTransform;
        private float m_ViewPortSize;
        private Vector2 m_ContentSize;

        private int m_ItemCount;
        private Action<XListItem> m_OnGetItemByIndex;
        private Func<int, int> m_OnGetItemPrefabIndex;
        private readonly Dictionary<int, XListItem> m_ItemMap = new();
        private readonly Dictionary<int, int> m_ItemPrefabIndexMap = new();
        private readonly Dictionary<int, Vector2> m_ItemPosMap = new();
        private bool m_IsVertical;
        private float m_MaxItemSize = 0f;
        private Vector2 m_CurContentPos;
        private int m_RangeMinIndex, m_RangeMaxIndex; // 列表可见范围
        private Vector2Int m_VisibleRange = Vector2Int.zero; // 当前可见范围

        private Action<Vector2> m_OnScrolling;
        public event Action<Vector2> OnScrolling
        {
            add => m_OnScrolling += value;
            remove => m_OnScrolling -= value;
        }

        public bool BigVisibleArea;
        public ScrollRect ScrollRect => m_ScrollRect;
        public Transform Viewport => m_ScrollRect.viewport;
        public float ViewPortSize => m_ViewPortSize;

        public int ContentStartOffset;
        public int ContentEndOffset;
        [SerializeField] private int m_ItemPadding;
        [SerializeField] private XListViewFillType m_FillOrigin;
        [SerializeField] private List<GameObject> m_ItemPrefabs;

        private void Awake()
        {
            m_ScrollRect = GetComponent<ScrollRect>();
            m_Content = m_ScrollRect.content;
            m_ContentRectTransform = m_Content.GetComponent<RectTransform>();
            m_Pool = new XListViewItemPool(transform, m_Content);
            // !!! 只支持单方向滚动
            if (m_ScrollRect.vertical && !m_ScrollRect.horizontal)
            {
                // 垂直列表(默认从顶部开始)
                m_IsVertical = true;
                m_ScrollRect.content.pivot = new Vector2(0.5f, 1f);
                m_ScrollRect.content.anchorMin = new Vector2(0f, 1f);
                m_ScrollRect.content.anchorMax = new Vector2(1f, 1f);
                m_ScrollRect.content.offsetMin = Vector2.zero;
                m_ScrollRect.content.offsetMax = Vector2.zero;
                m_ScrollRect.content.anchoredPosition = Vector2.zero;
            }
            else if (!m_ScrollRect.vertical && m_ScrollRect.horizontal)
            {
                // 水平列表
                m_IsVertical = false;
            }
        
            m_ScrollRect.onValueChanged.AddListener(pos =>
            {
                UpdateListContent(pos);
                m_OnScrolling?.Invoke(pos);
            });
        }

        public void InitListView(int itemCount, Func<int, int> onGetItemPrefabIndex, Action<XListItem> onGetItemByIndex)
        {
            m_OnGetItemPrefabIndex = onGetItemPrefabIndex;
            m_OnGetItemByIndex = onGetItemByIndex;
            ResetItemCount(itemCount);
            if (itemCount > 0) ScrollToItem(0);
        }

        public void ScrollToItem(int index, float time = 0f, Vector2 offset = default)
        {
            var itemRealPos = m_FillOrigin is XListViewFillType.TopToBottom or XListViewFillType.LeftToRight
                ? m_ItemPosMap[index] + m_ContentSize / 2f
                : m_ItemPosMap[index] - m_ContentSize / 2f;
            var itemSize = m_ItemPrefabs[m_OnGetItemPrefabIndex(index)].GetComponent<RectTransform>().sizeDelta;
            if (m_IsVertical) itemSize.Set(0, itemSize.y);
            else itemSize.Set(itemSize.x, 0);
            var pos = m_ContentSize/2f - itemRealPos - itemSize/2f - offset;
            m_ContentRectTransform.DOAnchorPos(pos, time);
            Vector2 contentPos;
            if (m_IsVertical)
            {
                contentPos = m_FillOrigin == XListViewFillType.TopToBottom
                    ? new Vector2(0, (m_ItemCount-index) / (float)m_ItemCount)
                    : new Vector2(0, index / (float)m_ItemCount);
            }
            else
            {
                contentPos = m_FillOrigin == XListViewFillType.LeftToRight
                    ? new Vector2((m_ItemCount-index) / (float)m_ItemCount, 0)
                    : new Vector2(index / (float)m_ItemCount, 0);
            }
            UpdateListContent(contentPos);
        }

        public void ScrollToItem(int index, float time, Action callback)
        {
            var itemRealPos = m_FillOrigin is XListViewFillType.TopToBottom or XListViewFillType.LeftToRight
                ? m_ItemPosMap[index] + m_ContentSize / 2f
                : m_ItemPosMap[index] - m_ContentSize / 2f;
            var itemSize = m_ItemPrefabs[m_OnGetItemPrefabIndex(index)].GetComponent<RectTransform>().sizeDelta;
            if (m_IsVertical) itemSize.Set(0, itemSize.y);
            else itemSize.Set(itemSize.x, 0);
            var pos = m_ContentSize/2f - itemRealPos - itemSize/2f;
            m_ContentRectTransform.DOAnchorPos(pos, time).OnComplete(() =>
            {
                callback?.Invoke();
            });
            Vector2 contentPos;
            if (m_IsVertical)
            {
                contentPos = m_FillOrigin == XListViewFillType.TopToBottom
                    ? new Vector2(0, (m_ItemCount-index) / (float)m_ItemCount)
                    : new Vector2(0, index / (float)m_ItemCount);
            }
            else
            {
                contentPos = m_FillOrigin == XListViewFillType.LeftToRight
                    ? new Vector2((m_ItemCount-index) / (float)m_ItemCount, 0)
                    : new Vector2(index / (float)m_ItemCount, 0);
            }
            UpdateListContent(contentPos);
        }

        public void ResetItemCount(int itemCount) // 重置Item数量
        {
            RecycleAllItem();
            m_ItemCount = itemCount;
            m_ItemMap.Clear();
            m_ItemPrefabIndexMap.Clear();
            m_ItemPosMap.Clear();
            UpdateContentSizeAndItemsPos();
        }

        public XListItem GetItem(int index)
        {
            return m_ItemMap.ContainsKey(index) ? m_ItemMap[index] : null;
        }
    
        public bool IsItemVisible(int index) // 判断指定索引的Item是否可见
        {
            bool isVisible = false;
            if (m_ItemPosMap.ContainsKey(index) && m_ItemPrefabIndexMap.ContainsKey(index))
            {
                var pos = m_FillOrigin is XListViewFillType.TopToBottom or XListViewFillType.LeftToRight ? 
                    m_ItemPosMap[index] + m_ContentSize / 2f : m_ItemPosMap[index] - m_ContentSize / 2f; // 坐标须加上内容尺寸的一半,才能使所有内容居中排布
                var size = m_ItemPrefabs[m_ItemPrefabIndexMap[index]].GetComponent<RectTransform>().sizeDelta;
                isVisible = IsItemVisible(pos, size, m_CurContentPos);
            }
            return isVisible;
        }

        public Vector2Int GetVisibleItemRange() // 获取可见的索引范围
        {
            m_VisibleRange.Set(m_RangeMinIndex, m_RangeMaxIndex);
            return m_VisibleRange;
        }

        private XListItem NewItem(int prefabIndex = 0)
        {
            var item = m_Pool.TryGetItemFromPool(prefabIndex);
            if (item == null)
            {
                var prefab = m_ItemPrefabs[prefabIndex];
                item = Instantiate(prefab, m_Content).GetComponent<XListItem>();
                item.SetPrefabIndex(prefabIndex);
            }
            return item;
        }
    
        private void UpdateListContent(Vector2 contentPos)
        {
            m_CurContentPos = contentPos;
            m_RangeMinIndex = 99999;
            m_RangeMaxIndex = -1;
            for (int i = 0; i < m_ItemCount; i++)
            {
                if (m_ItemPosMap.ContainsKey(i) && m_ItemPrefabIndexMap.ContainsKey(i))
                {
                    var pos = m_FillOrigin is XListViewFillType.TopToBottom or XListViewFillType.LeftToRight ? 
                        m_ItemPosMap[i] + m_ContentSize / 2f : m_ItemPosMap[i] - m_ContentSize / 2f; // 坐标须加上内容尺寸的一半,才能使所有内容居中排布
                    var size = m_ItemPrefabs[m_ItemPrefabIndexMap[i]].GetComponent<RectTransform>().sizeDelta;
                    var isVisible = IsItemVisible(pos, size, contentPos);
                    if (isVisible)
                    {
                        if (i < m_RangeMinIndex) m_RangeMinIndex = i;
                        if (i > m_RangeMaxIndex) m_RangeMaxIndex = i;
                    }
                    m_ItemMap.TryGetValue(i, out var item);
                    if (item != null)
                    {
                        if (isVisible) continue;
                        m_Pool.PushItemToPool(item);
                        m_ItemMap.Remove(i);
                    }
                    else
                    {
                        if (!isVisible) continue;
                        item = NewItem(m_ItemPrefabIndexMap[i]);
                        item.Init(i);
                        m_OnGetItemByIndex(item);
                        item.RectTransform.anchoredPosition = pos;
                        m_ItemMap.Add(i, item);
                    }
                }
            }
        }

        /// <summary>
        /// 预计算列表长度以及各item坐标
        /// </summary>
        private void UpdateContentSizeAndItemsPos()
        {
            // 内容区域计算
            m_ContentRectTransform.sizeDelta = Vector2.zero;
            for (int i = 0; i < m_ItemCount; i++)
            {
                var prefabIndex = m_OnGetItemPrefabIndex(i);
                if (prefabIndex < 0 || prefabIndex >= m_ItemPrefabs.Count) continue;
                m_ItemPrefabIndexMap.TryAdd(i, prefabIndex);
                var prefab = m_ItemPrefabs[prefabIndex];
                Vector2 sizeDelta = prefab.GetComponent<RectTransform>().sizeDelta;
                float itemSize;
                Vector2 pos;
                if (m_IsVertical)
                {
                    itemSize = sizeDelta.y;
                    m_MaxItemSize = itemSize > m_MaxItemSize ? itemSize : m_MaxItemSize;
                    m_ContentRectTransform.sizeDelta += new Vector2(0f, itemSize + m_ItemPadding);
                    if (i == 0)
                    {
                        m_ContentRectTransform.sizeDelta += new Vector2(0f, ContentStartOffset + ContentEndOffset);
                        pos = m_FillOrigin == XListViewFillType.TopToBottom ? new Vector2(0, -ContentStartOffset - itemSize / 2f) 
                            : new Vector2(0, ContentEndOffset + itemSize / 2f);
                    }
                    else
                    {
                        var lastPrefab = m_ItemPrefabs[m_ItemPrefabIndexMap[i-1]];
                        var lastPos = m_ItemPosMap[i-1];
                        pos = m_FillOrigin == XListViewFillType.TopToBottom ? new Vector2(0, lastPos.y - m_ItemPadding - itemSize / 2f - lastPrefab.GetComponent<RectTransform>().sizeDelta.y / 2f) 
                            : new Vector2(0, lastPos.y + m_ItemPadding + itemSize / 2f + lastPrefab.GetComponent<RectTransform>().sizeDelta.y / 2f);
                    }
                }
                else
                {
                    // 水平方向滚动暂时未处理完毕!!!有需求是再继续处理
                    itemSize = sizeDelta.x;
                    m_MaxItemSize = itemSize > m_MaxItemSize ? itemSize : m_MaxItemSize;
                    m_ContentRectTransform.sizeDelta += new Vector2(itemSize + m_ItemPadding, 0f);
                    if (i == 0)
                    {
                        m_ContentRectTransform.sizeDelta += new Vector2(ContentStartOffset + ContentEndOffset, 0f);
                        pos = new Vector2(0,-ContentStartOffset - itemSize / 2f);
                    }
                    else
                    {
                        var lastPrefab = m_ItemPrefabs[m_ItemPrefabIndexMap[i-1]];
                        var lastPos = m_ItemPosMap[i-1];
                        pos = new Vector2(0, lastPos.x - m_ItemPadding - itemSize / 2f - lastPrefab.GetComponent<RectTransform>().sizeDelta.x / 2f);
                    }
                }
                m_ItemPosMap.TryAdd(i, pos);
            }
        
            m_ContentSize = m_ContentRectTransform.sizeDelta;
            m_ViewPortSize = m_IsVertical ? m_ScrollRect.viewport.rect.height : m_ScrollRect.viewport.rect.width;
        }

        private void RecycleAllItem()
        {
            if (m_ItemMap.Count == 0) return;
            foreach (var item in m_ItemMap.Values) m_Pool.PushItemToPool(item);
        }
    
        private bool IsItemVisible(Vector2 itemPos, Vector2 itemSize, Vector2 pos)
        {
            Vector2 anchoredPosition = itemPos;
            float itemStart = 0f, itemEnd = 0f;
            var viewportArea = GetViewportArea(pos);
            if (m_IsVertical)
            {
                // 垂直列表
                itemStart = anchoredPosition.y + itemSize.y / 2;
                itemEnd = anchoredPosition.y - itemSize.y / 2;
            }
            else
            {
                // 水平列表
                itemStart = anchoredPosition.x + itemSize.x / 2;
                itemEnd = anchoredPosition.x - itemSize.x / 2;
            }
            // 判断Item是否出现在可视区域内(只要有一部分出现即可)
            if (BigVisibleArea)
            {
                // 预留了更多的空间
                return (itemStart <= viewportArea.x + m_MaxItemSize && itemStart >= viewportArea.y - m_MaxItemSize) || (itemEnd >= viewportArea.y - m_MaxItemSize && itemEnd <= viewportArea.x + m_MaxItemSize);   
            }
            // 超出就隐藏
            return (itemStart <= viewportArea.x && itemStart >= viewportArea.y) || (itemEnd >= viewportArea.y && itemEnd <= viewportArea.x);
        }
    
        private Vector2 GetViewportArea(Vector2 contentPos)
        {
            Vector2 area = Vector2.zero;
            if (m_IsVertical)
            {
                // 垂直列表
                var posY =(contentPos.y - 0.5f) * (m_ContentRectTransform.sizeDelta.y - m_ViewPortSize);
                area.Set(posY + m_ViewPortSize / 2f, posY - m_ViewPortSize / 2f);
            }
            else
            {
                // 水平列表
                var posX =(contentPos.x - 0.5f) * (m_ContentRectTransform.sizeDelta.x - m_ViewPortSize);
                area.Set(posX + m_ViewPortSize / 2f, posX - m_ViewPortSize / 2f);
            }
            return area;
        }
    }
    
    public enum XListViewFillType
    {
        TopToBottom,
        BottomToTop,
        LeftToRight,
        RightToLeft,
    }
    
    public class XListViewItemPool
    {
        private readonly Transform m_PoolRoot;
        private readonly Transform m_Content;
        
        private readonly Dictionary<int, Stack<XListItem>> m_PoolDict = new ();
        private readonly Vector3 m_HiddenPosition = new (99999, 99999, 0);
        
        public XListViewItemPool(Transform root, Transform content)
        {
            m_PoolRoot = root;
            m_Content = content;
        }

        public XListItem TryGetItemFromPool(int prefabIndex)
        {
            XListItem item = null;
            if (m_PoolDict.ContainsKey(prefabIndex) && m_PoolDict[prefabIndex].Count > 0)
            {
                item = m_PoolDict[prefabIndex].Pop();
                item.transform.SetParent(m_Content);
            }
            return item;
        }

        public void PushItemToPool(XListItem item)
        {
            if (!m_PoolDict.ContainsKey(item.PrefabIndex))
            {
                m_PoolDict.Add(item.PrefabIndex, new Stack<XListItem>());
            }
            item.transform.SetParent(m_PoolRoot);
            item.transform.localPosition = m_HiddenPosition;
            item.SetActive(false);
            m_PoolDict[item.PrefabIndex].Push(item);
        }
    }
}