using System.Collections.Generic;
using UnityEngine;

namespace EFramework.Extensions.UI.VirtualList
{
    internal sealed class QVirtualItemPool
    {
        private readonly RectTransform m_content;
        private readonly RectTransform m_poolRoot;
        private readonly Dictionary<GameObject, Stack<QVirtualListItem>> m_items = new();

        public QVirtualItemPool(Transform owner, RectTransform content)
        {
            m_content = content;
            var poolObject = new GameObject("VirtualItemPool", typeof(RectTransform));
            m_poolRoot = poolObject.GetComponent<RectTransform>();
            m_poolRoot.SetParent(owner, false);
            m_poolRoot.anchorMin = Vector2.zero;
            m_poolRoot.anchorMax = Vector2.zero;
            m_poolRoot.pivot = Vector2.zero;
            m_poolRoot.anchoredPosition = new Vector2(99999f, 99999f);
        }

        public QVirtualListItem Get(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("QVirtualListView item prefab is null.");
                return null;
            }

            if (m_items.TryGetValue(prefab, out var stack) && stack.Count > 0)
            {
                var pooledItem = stack.Pop();
                pooledItem.transform.SetParent(m_content, false);
                pooledItem.gameObject.SetActive(true);
                return pooledItem;
            }

            var itemObject = Object.Instantiate(prefab, m_content);
            var item = itemObject.GetComponent<QVirtualListItem>();
            if (item == null)
            {
                Debug.LogError($"Virtualized item prefab '{prefab.name}' must contain a QVirtualListItem component.");
                Object.Destroy(itemObject);
                return null;
            }

            item.SetSourcePrefab(prefab);
            itemObject.SetActive(true);
            return item;
        }

        public void Release(QVirtualListItem item)
        {
            if (item == null || item.SourcePrefab == null)
            {
                return;
            }

            if (!m_items.TryGetValue(item.SourcePrefab, out var stack))
            {
                stack = new Stack<QVirtualListItem>();
                m_items.Add(item.SourcePrefab, stack);
            }

            item.gameObject.SetActive(false);
            item.transform.SetParent(m_poolRoot, false);
            stack.Push(item);
        }

        public void DestroyAll()
        {
            foreach (var stack in m_items.Values)
            {
                while (stack.Count > 0)
                {
                    var item = stack.Pop();
                    if (item != null)
                    {
                        Object.Destroy(item.gameObject);
                    }
                }
            }

            m_items.Clear();

            if (m_poolRoot != null)
            {
                Object.Destroy(m_poolRoot.gameObject);
            }
        }
    }
}
