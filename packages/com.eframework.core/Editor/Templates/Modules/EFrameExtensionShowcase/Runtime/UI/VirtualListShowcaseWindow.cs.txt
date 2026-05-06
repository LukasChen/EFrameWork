using System;
using EFramework.Extensions.UI.VirtualList;
using EFramework.Generated;
using EFramework.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameApp.Modules.EFrameExtensionShowcase.UI
{
    public sealed class VirtualListShowcaseController : UIControllerBase<VirtualListShowcaseView>
    {
        protected override string AssetPath => VirtualListShowcaseWindow.AssetPath;

        protected override void OnViewCreated()
        {
            base.OnViewCreated();
            if (CurrentView?.Window != null)
            {
                CurrentView.Window.CloseRequested = Hide;
            }
        }

        protected override void OnViewDestroyed()
        {
            if (CurrentView?.Window != null)
            {
                CurrentView.Window.CloseRequested = null;
            }

            base.OnViewDestroyed();
        }
    }

    public sealed class VirtualListShowcaseView : BindingViewBase
    {
        public VirtualListShowcaseWindow Window { get; private set; }

        protected override void OnBindingSet()
        {
            base.OnBindingSet();

            Window = gameObject.GetComponent<VirtualListShowcaseWindow>();
            if (Window == null)
            {
                Debug.LogError($"{nameof(VirtualListShowcaseWindow)} must be authored on the showcase prefab.");
                return;
            }

            Window.Build();
        }
    }

    public sealed class VirtualListShowcaseWindow : MonoBehaviour
    {
        public const string AssetPath = ResPath.Generated.Modules.EFrameExtensionShowcase.Res.UI.Panels.EFrameExtensionShowcase.QVirtualListShowcaseWindow;

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

        public void Build()
        {
            var panel = transform.Find("Panel") as RectTransform;
            var title = panel?.Find("Title") as RectTransform;
            var contentRoot = panel?.Find("ContentRoot") as RectTransform;
            var toolbarRoot = panel?.Find("ToolbarRoot") as RectTransform;
            var infoTextRect = panel?.Find("InfoText") as RectTransform;
            m_contentRoot = contentRoot;
            m_infoText = infoTextRect?.GetComponent<Text>();

            var closeButton = panel?.Find("CloseButton")?.GetComponent<Button>();
            ConfigureShellLayout(title, infoTextRect, closeButton?.transform as RectTransform, toolbarRoot, contentRoot);
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
            }

            BuildToolbar(toolbarRoot);
            BuildCollections(contentRoot);
            if (m_listView == null || m_gridView == null)
            {
                return;
            }

            SwitchMode(false);
        }

        private void BuildToolbar(RectTransform toolbar)
        {
            if (toolbar == null)
            {
                return;
            }

            BindButton(toolbar, "VariableListButton", () => SwitchMode(false));
            BindButton(toolbar, "GridButton", () => SwitchMode(true));
            BindButton(toolbar, "TopButton", () => ScrollTo(0, QVirtualListAlign.Start));
            BindButton(toolbar, "CenterButton", () => ScrollTo(GetCount() / 2, QVirtualListAlign.Center));
            BindButton(toolbar, "EndButton", () => ScrollTo(Mathf.Max(0, GetCount() - 1), QVirtualListAlign.End));
            BindButton(toolbar, "ReloadButton", Reload);
            BindButton(toolbar, "RefreshButton", RefreshItemSeven);
        }

        private void BuildCollections(RectTransform contentRoot)
        {
            if (contentRoot == null)
            {
                return;
            }

            var listObject = contentRoot.Find("VariableList")?.gameObject;
            var gridObject = contentRoot.Find("Grid")?.gameObject;
            var listItemPrefab = contentRoot.Find("ListItemPrefab")?.gameObject;
            var gridItemPrefab = contentRoot.Find("GridItemPrefab")?.gameObject;

            m_listView = listObject != null ? listObject.GetComponent<QVirtualListView>() : null;
            m_gridView = gridObject != null ? gridObject.GetComponent<QVirtualGridView>() : null;

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
            return m_gridMode ? m_gridAdapter.Count : m_listAdapter.Count;
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
        }

        private static void BindButton(Transform parent, string name, UnityEngine.Events.UnityAction onClick)
        {
            var button = parent.Find(name)?.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError($"Virtual List showcase button is missing from prefab: {name}");
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
        }

        private static void ConfigureShellLayout(RectTransform title, RectTransform infoText, RectTransform closeButton, RectTransform toolbar, RectTransform content)
        {
            SetTopBand(title, 24f, 420f, 20f, 44f);
            SetTopBand(infoText, 500f, 124f, 20f, 44f);
            SetTopRight(closeButton, 24f, 20f, 92f, 44f);
            SetTopBand(toolbar, 24f, 24f, 82f, 116f);
            SetStretchOffsets(content, 24f, 24f, 218f, 24f);
            ConfigureToolbarButtons(toolbar);
        }

        private static void SetTopBand(RectTransform rectTransform, float left, float right, float top, float height)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(-left - right, height);
            rectTransform.anchoredPosition = new Vector2((left - right) * 0.5f, -top - height * 0.5f);
        }

        private static void SetTopRight(RectTransform rectTransform, float right, float top, float width, float height)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.one;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.anchoredPosition = new Vector2(-right - width * 0.5f, -top - height * 0.5f);
        }

        private static void SetStretchOffsets(RectTransform rectTransform, float left, float right, float top, float bottom)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(-left - right, -top - bottom);
            rectTransform.anchoredPosition = new Vector2((left - right) * 0.5f, (bottom - top) * 0.5f);
        }

        private static void ConfigureToolbarButtons(RectTransform toolbar)
        {
            if (toolbar == null)
            {
                return;
            }

            const float buttonWidth = 148f;
            const float buttonHeight = 48f;
            const float spacing = 12f;

            for (var i = 0; i < toolbar.childCount; i++)
            {
                var child = toolbar.GetChild(i) as RectTransform;
                if (child == null)
                {
                    continue;
                }

                var row = i / 4;
                var column = i % 4;
                child.anchorMin = new Vector2(0f, 1f);
                child.anchorMax = new Vector2(0f, 1f);
                child.pivot = new Vector2(0f, 1f);
                child.sizeDelta = new Vector2(buttonWidth, buttonHeight);
                child.anchoredPosition = new Vector2(column * (buttonWidth + spacing), -row * (buttonHeight + spacing));
            }
        }
    }
}
