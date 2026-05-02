using UnityEngine;

namespace EFramework.Extensions.UI.VirtualList
{
    /// <summary>
    /// Base component for pooled virtual list and grid items.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class QVirtualListItem : MonoBehaviour
    {
        public int Index { get; private set; } = -1;
        public int BindVersion { get; private set; }
        public GameObject SourcePrefab { get; private set; }
        public RectTransform RectTransform { get; private set; }

        protected virtual void Awake()
        {
            CacheRectTransform();
        }

        internal void SetSourcePrefab(GameObject sourcePrefab)
        {
            SourcePrefab = sourcePrefab;
            CacheRectTransform();
        }

        internal void MarkBound(int index)
        {
            CacheRectTransform();
            Index = index;
            BindVersion++;
            OnBind(index);
        }

        internal void MarkRecycled()
        {
            OnRecycle();
            Index = -1;
            BindVersion++;
        }

        protected virtual void OnBind(int index)
        {
        }

        protected virtual void OnRecycle()
        {
        }

        private void CacheRectTransform()
        {
            if (RectTransform == null)
            {
                RectTransform = GetComponent<RectTransform>();
            }
        }
    }
}
