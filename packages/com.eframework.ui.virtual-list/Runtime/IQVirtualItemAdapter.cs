using UnityEngine;

namespace EFramework.Extensions.UI.VirtualList
{
    /// <summary>
    /// Supplies item data and binding logic for virtualized UI collections.
    /// </summary>
    public interface IQVirtualItemAdapter
    {
        int Count { get; }

        GameObject GetItemPrefab(int index);

        void Bind(QVirtualListItem item, int index);

        void Unbind(QVirtualListItem item, int index);
    }

    /// <summary>
    /// Supplies per-item size information for a single-axis virtualized list.
    /// </summary>
    public interface IQVirtualListAdapter : IQVirtualItemAdapter
    {
        Vector2 GetItemSize(int index);
    }
}
