using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EFrameWork.Runtime.UI
{
    /// <summary>
    ///     QList 用于管理可复用的 UI 列表项，实现对象池和虚拟化，提高性能。
    /// </summary>
    public class QList
    {
        // 对象池，存放未激活的列表项 GameObject
        private readonly Queue<GameObject> m_itemPool = new();

        // 列表项的预制体
        private readonly GameObject m_itemPrefab;

        // 滚动视图组件
        private readonly ScrollRect m_scrollRect;

        // 列表内容的根节点
        public RectTransform ContentRoot;

        // 上一次滚动位置
        private Vector2 m_lastPos = Vector2.zero;

        // 当前可视区域
        private Rect m_viewRect;

        private float m_spacer;

        // 列表项点击事件
        public Action<QItemBase> OnItemClick;
        public ScrollRect ScrollRect => m_scrollRect;

        /// <summary>
        ///     构造函数，初始化列表，设置内容根节点和对象池
        /// </summary>
        /// <param name="scrollRect">滚动视图</param>
        public QList(ScrollRect scrollRect, float spacer = 0f)
        {
            m_scrollRect = scrollRect;
            ContentRoot = new GameObject("ContentRoot").AddComponent<RectTransform>();
            ContentRoot.SetParent(m_scrollRect.content, false);
            m_itemPrefab = m_scrollRect.content.Find("ItemPrefab").gameObject;
            m_itemPrefab.SetActive(false);

            Items = new List<QItemBase>();
            m_spacer = spacer;

            m_viewRect = new Rect(Vector2.zero, m_scrollRect.viewport.rect.size + Vector2.one * 200f);
            m_scrollRect.onValueChanged.AddListener(OnScrollChange);
        }

        /// <summary>
        ///     当前所有数据项
        /// </summary>
        public List<QItemBase> Items { get; protected set; }

        /// <summary>
        ///     从对象池获取或实例化一个 QItemMonoBase
        /// </summary>
        public QItemMonoBase GetItemMono()
        {
            if (m_itemPool.Count > 0)
            {
                GameObject item = m_itemPool.Dequeue();
                item.SetActive(true);
                return item.GetComponent<QItemMonoBase>();
            }

            GameObject go = Object.Instantiate(m_itemPrefab, ContentRoot);
            go.SetActive(true);
            QItemMonoBase itemMono = go.GetComponent<QItemMonoBase>();
            itemMono.OnItemClick += OnItemClickHandler;
            return itemMono;
        }

        /// <summary>
        ///     回收 QItemMonoBase 到对象池
        /// </summary>
        public void RecycleItemMono(GameObject go)
        {
            go.SetActive(false);
            m_itemPool.Enqueue(go);
        }

        /// <summary>
        ///     列表项点击事件回调
        /// </summary>
        protected void OnItemClickHandler(QItemBase item)
        {
            OnItemClick?.Invoke(item);
        }

        /// <summary>
        ///     释放资源，移除事件监听并销毁所有池中对象
        /// </summary>
        public void Dispose()
        {
            m_scrollRect.onValueChanged.RemoveListener(OnScrollChange);
            Items.Clear();
            foreach (GameObject item in m_itemPool) Object.Destroy(item);
        }

        /// <summary>
        ///     添加一个新数据项,批量添加时建议 updateLayoutNow 设置为 false，
        ///     最后统一调用 UpdateLayout() 来更新布局
        /// </summary>
        public QItemBase AddItem(object data, Vector2 size, bool updateLayoutNow = false)
        {
            QItemBase item = new(this, data, new Rect(-size / 2f, size));
            Items.Add(item);
            if (updateLayoutNow) UpdateLayout();
            return item;
        }

        /// <summary>
        ///     添加已有的数据项,批量添加时建议 updateLayoutNow 设置为 false，
        ///     最后统一调用 UpdateLayout() 来更新布局
        /// </summary>
        public void AddItem(QItemBase item, bool updateLayoutNow = false)
        {
            Items.Add(item);
            if (updateLayoutNow) UpdateLayout();
        }

        /// <summary>
        ///     在指定位置插入数据项
        /// </summary>
        public void InsertItem(QItemBase item, int index, bool updateLayoutNow = false)
        {
            Items.Insert(index, item);
            if (updateLayoutNow) UpdateLayout();
        }

        /// <summary>
        ///     移除指定数据项
        /// </summary>
        public void RemoveItem(QItemBase item, bool updateLayoutNow = false)
        {
            if (Items.Remove(item) && updateLayoutNow)
                UpdateLayout();
        }

        /// <summary>
        ///     移除指定索引的数据项
        /// </summary>
        public void RemoveItem(int index, bool updateLayoutNow = false)
        {
            if (index >= 0 && index < Items.Count)
                Items.RemoveAt(index);
            if (updateLayoutNow) UpdateLayout();
        }

        public QItemBase GetItem(int index)
        {
            if (index >= 0 && index < Items.Count)
                return Items[index];
            return null;
        }

        public void CenterViewItemAt(int index)
        {
            var item = GetItem(index);
            if (item == null) return;

            float contentSize = m_scrollRect.vertical ? m_scrollRect.content.sizeDelta.y : m_scrollRect.content.sizeDelta.x;
            float viewSize = m_scrollRect.vertical ? m_scrollRect.viewport.rect.height : m_scrollRect.viewport.rect.width;
            float itemPos = m_scrollRect.vertical ? item.Rect.y : item.Rect.x;
            float normalizedPos = itemPos / contentSize - (viewSize / 2) / contentSize;
            normalizedPos = Mathf.Clamp01(normalizedPos);
            if (m_scrollRect.vertical)
                m_scrollRect.verticalNormalizedPosition = normalizedPos;
            else
                m_scrollRect.horizontalNormalizedPosition = normalizedPos;
        }

        /// <summary>
        ///     滚动回调，判断是否需要刷新可见项
        /// </summary>
        private void OnScrollChange(Vector2 position)
        {
            if ((m_scrollRect.content.anchoredPosition - m_lastPos).magnitude > 20)
            {
                m_lastPos = m_scrollRect.content.anchoredPosition;
                UpdateVisible();
            }
        }

        /// <summary>
        ///     更新所有项的位置和布局
        /// </summary>
        public void UpdateLayout()
        {
            float x = m_spacer, y = m_spacer;
            Vector2 pivot = m_scrollRect.content.pivot;

            if (m_scrollRect.vertical)
            {
                if (Mathf.Approximately(pivot.y, 1f))
                {
                    // 顶部为原点，y递减
                    foreach (QItemBase item in Items)
                    {
                        item.SetPosition(0, y);
                        y -= item.Rect.height;
                    }
                }
                else if (pivot.y == 0f)
                {
                    // 底部为原点，y递减
                    foreach (QItemBase item in Items)
                    {
                        item.SetPosition(0, y);
                        y += item.Rect.height;
                    }
                }

                // 设置 content 高度
                float contentHeight = Mathf.Abs(y) + m_spacer;
                m_scrollRect.content.sizeDelta = new Vector2(m_scrollRect.content.sizeDelta.x, contentHeight);
                ContentRoot.anchoredPosition = new Vector2(0, -y / 2);
            }
            else
            {
                if (pivot.x == 0f)
                {
                    // 左侧为原点，x递增
                    x += Items.Count > 0 ? Items[0].Rect.width / 2f : 0;
                    foreach (QItemBase item in Items)
                    {
                        item.SetPosition(x, 0);
                        x += item.Rect.width;
                    }
                }
                else if (Mathf.Approximately(pivot.x, 1f))
                {
                    // 右侧为原点，x递减
                    x -= Items.Count > 0 ? Items[0].Rect.width / 2f : 0;
                    foreach (QItemBase item in Items)
                    {
                        item.SetPosition(x, 0);
                        x -= item.Rect.width;
                    }
                }

                float contentWidth = Mathf.Abs(x) + m_spacer;
                m_scrollRect.content.sizeDelta = new Vector2(contentWidth, m_scrollRect.content.sizeDelta.y);
                ContentRoot.anchoredPosition = new Vector2(-x / 2, 0);
            }


            UpdateVisible();
        }

        /// <summary>
        ///     刷新哪些项是可见的，进行虚拟化
        /// </summary>
        private void UpdateVisible()
        {
            if (m_scrollRect.vertical)
                m_viewRect.y = -m_scrollRect.content.anchoredPosition.y;
            else
                m_viewRect.x = -m_scrollRect.content.anchoredPosition.x;

            foreach (QItemBase item in Items) item.SetVisible(m_viewRect.Overlaps(item.Rect, true));
        }
    }

    /// <summary>
    ///     QItemBase 表示列表中的一个数据项，负责管理其可见性和位置
    /// </summary>
    public class QItemBase
    {
        // 关联的 Mono 行为
        protected QItemMonoBase m_itemMono;

        // 所属的 QList
        protected QList m_list;

        // 当前项的矩形区域
        public Rect Rect;

        /// <summary>
        ///     构造函数
        /// </summary>
        public QItemBase(QList list, object data, Rect rect)
        {
            m_list = list;
            Rect = rect;
            Data = data;
        }

        /// <summary>
        ///     数据项内容
        /// </summary>
        public object Data { get; protected set; }

        /// <summary>
        ///     设置该项是否可见，自动实例化或回收 Mono
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (visible)
            {
                if (m_itemMono == null)
                {
                    m_itemMono = m_list.GetItemMono().GetComponent<QItemMonoBase>();
                    m_itemMono.InitItem(this);
                }
            }
            else if (m_itemMono != null)
            {
                m_list.RecycleItemMono(m_itemMono.gameObject);
                m_itemMono = null;
            }
        }

        /// <summary>
        ///     设置该项的位置
        /// </summary>
        public void SetPosition(float x, float y)
        {
            Rect.x = x;
            Rect.y = y;
        }
    }

    /// <summary>
    ///     QItemMonoBase 是所有列表项 Mono 行为的基类
    /// </summary>
    public class QItemMonoBase : MonoBehaviour
    {
        // 关联的数据项
        protected QItemBase m_item;

        // 列表项点击事件
        public Action<QItemBase> OnItemClick;

        /// <summary>
        ///     初始化该 Mono 行为
        /// </summary>
        public virtual void InitItem(QItemBase item)
        {
            m_item = item;
            UpdateView();
            UpdatePosition();
        }

        /// <summary>
        ///     更新显示内容（可重写）
        /// </summary>
        public virtual void UpdateView()
        {
        }

        /// <summary>
        ///     更新位置（可重写）
        /// </summary>
        public virtual void UpdatePosition()
        {
        }
    }
}
