using System;
using EFramework.Extensions.UI.VirtualList;
using EFramework.Generated;
using EFramework.Generated.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameApp.Modules.EFrameExtensionShowcase.UI
{
    public sealed class VirtualListShowcaseWindow : MonoBehaviour
    {
        public const string AssetPath = ResPath.Modules.EFrameExtensionShowcase.UI.Panels.EFrameExtensionShowcase.QVirtualListShowcaseWindow;

        public Action CloseRequested { get; set; }

        private Text m_infoText;
        private RectTransform m_contentRoot;
        private QVirtualListView m_listView;
        private QVirtualGridView m_gridView;
        private VariableListAdapter m_listAdapter;
        private GridAdapter m_gridAdapter;
        private bool m_gridMode;
        private float m_nextInfoRefresh;
        private Vector2 m_lastContentSize = new(-1f, -1f);

        private void Update()
        {
            RefreshLayoutIfContentSizeChanged();

            if (Time.unscaledTime < m_nextInfoRefresh)
            {
                return;
            }

            m_nextInfoRefresh = Time.unscaledTime + 0.2f;
            RefreshInfo();
        }

        private void OnDestroy()
        {
            CloseRequested = null;
        }

        public void Build(v_EFrameExtensionShowcaseQVirtualListShowcaseWindow view)
        {
            if (view == null)
            {
                Debug.LogError("Virtual List showcase generated view is missing.");
                return;
            }

            m_contentRoot = view.ContentRoot;
            m_infoText = view.InfoText;

            BindButton(view.CloseButton, () => CloseRequested?.Invoke());
            BindButton(view.VariableListButton, () => SwitchMode(false));
            BindButton(view.GridButton, () => SwitchMode(true));
            BindButton(view.TopButton, () => ScrollTo(0, QVirtualListAlign.Start));
            BindButton(view.CenterButton, () => ScrollTo(GetCount() / 2, QVirtualListAlign.Center));
            BindButton(view.EndButton, () => ScrollTo(Mathf.Max(0, GetCount() - 1), QVirtualListAlign.End));
            BindButton(view.ReloadButton, Reload);
            BindButton(view.RefreshButton, RefreshItemSeven);

            BuildCollections(view);
            if (m_listView == null || m_gridView == null)
            {
                return;
            }

            SwitchMode(false);
        }

        private void BuildCollections(v_EFrameExtensionShowcaseQVirtualListShowcaseWindow view)
        {
            if (view.ContentRoot == null)
            {
                Debug.LogError("Virtual List showcase prefab is missing the authored content root binding.");
                return;
            }

            m_listView = view.VariableList;
            m_gridView = view.Grid;
            var listItemPrefab = view.ListItemPrefab?.gameObject;
            var gridItemPrefab = view.GridItemPrefab?.gameObject;

            if (m_listView == null || m_gridView == null || listItemPrefab == null || gridItemPrefab == null)
            {
                Debug.LogError("Virtual List showcase prefab is missing authored list/grid views or item templates.");
                return;
            }

            listItemPrefab.SetActive(false);
            gridItemPrefab.SetActive(false);

            m_listAdapter = new VariableListAdapter(listItemPrefab, 120);
            m_listView.SetAdapter(m_listAdapter);

            m_gridAdapter = new GridAdapter(gridItemPrefab, 96);
            m_gridView.SetAdapter(m_gridAdapter);
        }

        private void SwitchMode(bool gridMode)
        {
            if (m_listView == null || m_gridView == null)
            {
                return;
            }

            m_gridMode = gridMode;
            m_listView.gameObject.SetActive(!gridMode);
            m_gridView.gameObject.SetActive(gridMode);
            RefreshActiveCollectionLayout();
            RefreshInfo();
        }

        private int GetCount()
        {
            return m_gridMode ? m_gridAdapter?.Count ?? 0 : m_listAdapter?.Count ?? 0;
        }

        private void ScrollTo(int index, QVirtualListAlign align)
        {
            if (m_listView == null || m_gridView == null)
            {
                return;
            }

            RefreshActiveCollectionLayout();

            if (m_gridMode)
            {
                m_gridView.ScrollToIndex(index, align);
            }
            else
            {
                m_listView.ScrollToIndex(index, align);
            }

            RefreshInfo();
        }

        private void Reload()
        {
            if (m_listView == null || m_gridView == null)
            {
                return;
            }

            if (m_gridMode)
            {
                m_gridAdapter.ToggleCount();
                m_gridView.Reload();
            }
            else
            {
                m_listAdapter.ToggleCount();
                m_listView.Reload();
            }

            RefreshInfo();
        }

        private void RefreshItemSeven()
        {
            if (m_listView == null || m_gridView == null)
            {
                return;
            }

            if (m_gridMode)
            {
                m_gridAdapter.MarkRefreshed(7);
                m_gridView.RefreshItem(7);
            }
            else
            {
                m_listAdapter.MarkRefreshed(7);
                m_listView.RefreshItem(7);
            }

            RefreshInfo();
        }

        private void RefreshInfo()
        {
            if (m_infoText == null || m_listView == null || m_gridView == null)
            {
                return;
            }

            var range = m_gridMode ? m_gridView.GetVisibleRange() : m_listView.GetVisibleRange();
            var mode = m_gridMode ? "Grid" : "Variable list";
            m_infoText.text = $"{mode} | Count {GetCount()} | Visible {range.x}-{range.y}";
        }

        private void RefreshLayoutIfContentSizeChanged()
        {
            if (m_contentRoot == null || m_listView == null || m_gridView == null)
            {
                return;
            }

            var size = m_contentRoot.rect.size;
            if ((size - m_lastContentSize).sqrMagnitude < 0.01f)
            {
                return;
            }

            m_lastContentSize = size;
            RefreshActiveCollectionLayout();
        }

        private void RefreshActiveCollectionLayout()
        {
            if (m_listView == null || m_gridView == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            if (m_gridMode)
            {
                m_gridView.RefreshLayout();
            }
            else
            {
                m_listView.RefreshLayout();
            }
        }

        private sealed class VariableListAdapter : IQVirtualListAdapter
        {
            private readonly GameObject m_itemPrefab;
            private readonly System.Collections.Generic.HashSet<int> m_refreshed = new();
            private int m_count;

            public VariableListAdapter(GameObject itemPrefab, int count)
            {
                m_itemPrefab = itemPrefab;
                m_count = count;
            }

            public int Count => m_count;

            public GameObject GetItemPrefab(int index)
            {
                return m_itemPrefab;
            }

            public Vector2 GetItemSize(int index)
            {
                return new Vector2(0f, 64f + index % 4 * 22f);
            }

            public void Bind(QVirtualListItem item, int index)
            {
                BindItem(item, index, $"Row {index:000} | size {GetItemSize(index).y:0}px | bind {item.BindVersion}", m_refreshed.Contains(index));
            }

            public void Unbind(QVirtualListItem item, int index)
            {
            }

            public void ToggleCount()
            {
                m_count = m_count == 120 ? 240 : 120;
            }

            public void MarkRefreshed(int index)
            {
                m_refreshed.Add(index);
            }
        }

        private sealed class GridAdapter : IQVirtualItemAdapter
        {
            private readonly GameObject m_itemPrefab;
            private readonly System.Collections.Generic.HashSet<int> m_refreshed = new();
            private int m_count;

            public GridAdapter(GameObject itemPrefab, int count)
            {
                m_itemPrefab = itemPrefab;
                m_count = count;
            }

            public int Count => m_count;

            public GameObject GetItemPrefab(int index)
            {
                return m_itemPrefab;
            }

            public void Bind(QVirtualListItem item, int index)
            {
                BindItem(item, index, $"Cell {index:000}\npooled bind {item.BindVersion}", m_refreshed.Contains(index));
            }

            public void Unbind(QVirtualListItem item, int index)
            {
            }

            public void ToggleCount()
            {
                m_count = m_count == 96 ? 192 : 96;
            }

            public void MarkRefreshed(int index)
            {
                m_refreshed.Add(index);
            }
        }

        private static void BindItem(QVirtualListItem item, int index, string text, bool refreshed)
        {
            var image = item.GetComponent<Image>();
            if (image != null)
            {
                image.color = refreshed
                    ? new Color32(92, 78, 34, 255)
                    : index % 2 == 0
                        ? new Color32(49, 61, 78, 255)
                        : new Color32(41, 52, 68, 255);
            }

            var label = item.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = refreshed ? $"{text}\nrefreshed" : text;
            }

            ShowcaseFeedbackInstaller.InstallItem(item.gameObject);
        }

        private static void BindButton(Button button, UnityAction onClick)
        {
            if (button == null)
            {
                Debug.LogError("Virtual List showcase prefab is missing an authored button binding.");
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
            ShowcaseFeedbackInstaller.InstallButton(button);
        }
    }
}
