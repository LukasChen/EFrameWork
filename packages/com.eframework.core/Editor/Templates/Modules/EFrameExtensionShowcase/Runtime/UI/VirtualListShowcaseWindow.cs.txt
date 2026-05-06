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

            Window = gameObject.GetComponent<VirtualListShowcaseWindow>() ?? gameObject.AddComponent<VirtualListShowcaseWindow>();
            Window.Build();
        }
    }

    public sealed class VirtualListShowcaseWindow : MonoBehaviour
    {
        public const string AssetPath = ResPath.Generated.Modules.EFrameExtensionShowcase.Res.UI.Panels.EFrameExtensionShowcase.QVirtualListShowcaseWindow;

        public Action CloseRequested { get; set; }

        private Text m_infoText;
        private QVirtualListView m_listView;
        private QVirtualGridView m_gridView;
        private VariableListAdapter m_listAdapter;
        private GridAdapter m_gridAdapter;
        private bool m_gridMode;
        private float m_nextInfoRefresh;

        private void Update()
        {
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
            m_infoText = infoTextRect?.GetComponent<Text>();

            var closeButton = panel?.Find("CloseButton")?.GetComponent<Button>();
            ConfigurePanelLayout(panel);
            ConfigureShellLayout(title, infoTextRect, closeButton?.transform as RectTransform, toolbarRoot, contentRoot);
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
            }

            BuildToolbar(toolbarRoot);
            BuildCollections(contentRoot);
            SwitchMode(false);
        }

        private void BuildToolbar(RectTransform toolbar)
        {
            if (toolbar == null)
            {
                return;
            }

            ClearChildren(toolbar);
            var horizontalLayout = toolbar.GetComponent<HorizontalLayoutGroup>();
            if (horizontalLayout != null)
            {
                Destroy(horizontalLayout);
            }

            var gridLayout = toolbar.GetComponent<GridLayoutGroup>() ?? toolbar.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(112f, 32f);
            gridLayout.spacing = new Vector2(8f, 8f);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            gridLayout.constraintCount = 2;

            CreateButton(toolbar, "VariableListButton", "Variable List", () => SwitchMode(false));
            CreateButton(toolbar, "GridButton", "Grid", () => SwitchMode(true));
            CreateButton(toolbar, "TopButton", "Top", () => ScrollTo(0, QVirtualListAlign.Start));
            CreateButton(toolbar, "CenterButton", "Center", () => ScrollTo(GetCount() / 2, QVirtualListAlign.Center));
            CreateButton(toolbar, "EndButton", "End", () => ScrollTo(Mathf.Max(0, GetCount() - 1), QVirtualListAlign.End));
            CreateButton(toolbar, "ReloadButton", "Reload", Reload);
            CreateButton(toolbar, "RefreshButton", "Refresh #7", RefreshItemSeven);
        }

        private void BuildCollections(RectTransform contentRoot)
        {
            if (contentRoot == null)
            {
                return;
            }

            ClearChildren(contentRoot);

            var listObject = CreateScrollArea("VariableList", contentRoot, out var listContent);
            m_listView = listObject.AddComponent<QVirtualListView>();
            SetPrivateField(m_listView, "m_spacing", 6f);
            SetPrivateField(m_listView, "m_padding", new RectOffset(8, 8, 8, 8));
            SetPrivateField(m_listView, "m_overscan", 180f);
            m_listAdapter = new VariableListAdapter(CreateItemPrefab("ListItemPrefab", contentRoot, new Vector2(0f, 56f)), 120);
            m_listView.SetAdapter(m_listAdapter);

            var gridObject = CreateScrollArea("Grid", contentRoot, out var gridContent);
            m_gridView = gridObject.AddComponent<QVirtualGridView>();
            SetPrivateField(m_gridView, "m_cellSize", new Vector2(118f, 76f));
            SetPrivateField(m_gridView, "m_spacing", new Vector2(8f, 8f));
            SetPrivateField(m_gridView, "m_constraintCount", 3);
            SetPrivateField(m_gridView, "m_autoWrapCrossAxis", true);
            SetPrivateField(m_gridView, "m_minAutoConstraintCount", 1);
            SetPrivateField(m_gridView, "m_maxAutoConstraintCount", 0);
            SetPrivateField(m_gridView, "m_padding", new RectOffset(8, 8, 8, 8));
            SetPrivateField(m_gridView, "m_overscan", 180f);
            m_gridAdapter = new GridAdapter(CreateItemPrefab("GridItemPrefab", contentRoot, new Vector2(118f, 76f)), 96);
            m_gridView.SetAdapter(m_gridAdapter);

            listContent.name = "VariableListContent";
            gridContent.name = "GridContent";
        }

        private GameObject CreateScrollArea(string name, Transform parent, out RectTransform content)
        {
            var scrollObject = CreateUIObject(name, parent);
            var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            Stretch(scrollRectTransform);

            var background = scrollObject.AddComponent<Image>();
            background.color = new Color32(30, 36, 46, 255);

            var viewportObject = CreateUIObject("Viewport", scrollObject.transform);
            var viewport = viewportObject.GetComponent<RectTransform>();
            Stretch(viewport);
            viewportObject.AddComponent<Image>().color = new Color32(22, 27, 36, 255);
            viewportObject.AddComponent<Mask>().showMaskGraphic = false;

            var contentObject = CreateUIObject("Content", viewportObject.transform);
            content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 32f;

            return scrollObject;
        }

        private GameObject CreateItemPrefab(string name, Transform parent, Vector2 size)
        {
            var item = CreateUIObject(name, parent);
            item.SetActive(false);
            var rect = item.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            item.AddComponent<QVirtualListItem>();
            item.AddComponent<Image>().color = new Color32(51, 64, 82, 255);

            var label = CreateText("Label", item.transform, string.Empty, 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(12f, 0f);
            label.rectTransform.offsetMax = new Vector2(-12f, 0f);
            return item;
        }

        private void SwitchMode(bool gridMode)
        {
            m_gridMode = gridMode;
            m_listView.gameObject.SetActive(!gridMode);
            m_gridView.gameObject.SetActive(gridMode);
            RefreshInfo();
        }

        private int GetCount()
        {
            return m_gridMode ? m_gridAdapter.Count : m_listAdapter.Count;
        }

        private void ScrollTo(int index, QVirtualListAlign align)
        {
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
                return new Vector2(0f, 48f + index % 4 * 18f);
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

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            var textObject = CreateUIObject(name, parent);
            var label = textObject.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.color = new Color32(235, 239, 246, 255);
            label.raycastTarget = false;
            return label;
        }

        private static void CreateButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = CreateUIObject(name, parent);
            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(112f, 32f);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color32(61, 77, 100, 255);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var text = CreateText("Text", buttonObject.transform, label, 12, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
        }

        private static void ConfigurePanelLayout(RectTransform panel)
        {
            if (panel == null)
            {
                return;
            }

            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.offsetMin = new Vector2(36f, 36f);
            panel.offsetMax = new Vector2(-36f, -36f);
        }

        private static void ConfigureShellLayout(RectTransform title, RectTransform infoText, RectTransform closeButton, RectTransform toolbar, RectTransform content)
        {
            SetTopBand(title, 18f, 360f, 12f, 32f);
            SetTopBand(infoText, 430f, 96f, 12f, 32f);
            SetTopRight(closeButton, 18f, 12f, 66f, 32f);
            SetTopBand(toolbar, 18f, 18f, 58f, 72f);
            SetStretchOffsets(content, 18f, 18f, 144f, 18f);
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

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
