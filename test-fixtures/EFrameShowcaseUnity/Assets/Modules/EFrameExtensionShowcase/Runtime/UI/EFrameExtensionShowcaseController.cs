using System;
using GameApp.Modules.EFrameExtensionShowcase.Demos;
using UnityEngine;
using UnityEngine.UI;

namespace GameApp.Modules.EFrameExtensionShowcase.UI
{
    public sealed class EFrameExtensionShowcaseController : IDisposable
    {
        private GameObject m_root;
        private Text m_detailText;

        public Action BackRequested { get; set; }

        public void Show()
        {
            if (m_root != null)
            {
                return;
            }

            m_root = new GameObject("EFrameExtensionShowcaseUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = m_root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;

            var scaler = m_root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;

            var panel = CreatePanel(m_root.transform);
            CreateText(panel, "EFrame Extension Showcase", 42, FontStyle.Bold, new Color(0.1f, 0.12f, 0.16f), new Vector2(0f, -70f), new Vector2(860f, 80f));
            CreateText(panel, "Optional extension demos. Installed packages are detected at runtime.", 24, FontStyle.Normal, new Color(0.24f, 0.27f, 0.32f), new Vector2(0f, -140f), new Vector2(860f, 80f));

            var y = -260f;
            foreach (var demo in EFrameExtensionShowcaseRegistry.Demos)
            {
                CreateDemoButton(panel, demo, new Vector2(0f, y));
                y -= 150f;
            }

            m_detailText = CreateText(panel, "Select an extension entry.", 24, FontStyle.Normal, new Color(0.14f, 0.16f, 0.2f), new Vector2(0f, y - 30f), new Vector2(860f, 120f));
            CreateButton(panel, "Back To Basic", new Vector2(0f, -830f), new Vector2(360f, 92f), OnBackClicked);
        }

        public void Dispose()
        {
            BackRequested = null;
            if (m_root != null)
            {
                UnityEngine.Object.Destroy(m_root);
                m_root = null;
                m_detailText = null;
            }
        }

        private static RectTransform CreatePanel(Transform parent)
        {
            var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);

            var rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = panelObject.GetComponent<Image>();
            image.color = new Color(0.93f, 0.95f, 0.97f);
            return rect;
        }

        private void CreateDemoButton(RectTransform parent, EFrameExtensionShowcaseDemo demo, Vector2 anchoredPosition)
        {
            var installed = EFrameExtensionShowcaseRegistry.IsInstalled(demo);
            var label = installed ? $"{demo.Title}  [Installed]" : $"{demo.Title}  [Package Missing]";
            CreateButton(parent, label, anchoredPosition, new Vector2(820f, 108f), () => OnDemoSelected(demo));
        }

        private static Button CreateButton(RectTransform parent, string label, Vector2 anchoredPosition, Vector2 size, Action clicked)
        {
            var buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.18f, 0.26f);

            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() => clicked?.Invoke());

            CreateText(rect, label, 24, FontStyle.Bold, Color.white, Vector2.zero, size);
            return button;
        }

        private static Text CreateText(RectTransform parent, string text, int fontSize, FontStyle fontStyle, Color color, Vector2 anchoredPosition, Vector2 size)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var label = textObject.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private void OnDemoSelected(EFrameExtensionShowcaseDemo demo)
        {
            demo.Run?.Invoke();
            var state = EFrameExtensionShowcaseRegistry.IsInstalled(demo) ? "installed" : "not installed";
            if (m_detailText != null)
            {
                m_detailText.text = $"{demo.Title}\nPackage: {demo.PackageName}\nStatus: {state}\n{demo.Description}";
            }
        }

        private void OnBackClicked()
        {
            BackRequested?.Invoke();
        }
    }
}
