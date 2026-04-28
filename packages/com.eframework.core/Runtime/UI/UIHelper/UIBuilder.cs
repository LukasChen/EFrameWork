using EFrameWork.Runtime.Utils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
namespace EFrameWork.Runtime.UI
{
    /// <summary>
    /// UIBuilder is a utility class for creating UI elements in Unity.
    /// </summary>
    /// <remarks>
    /// This class provides methods to create common UI components like buttons, panels, etc.
    /// It can be extended to include more complex UI creation logic as needed.
    /// </remarks>
    [UnityEngine.Scripting.Preserve]

    public sealed class UIBuilder
    {
        public static RectTransform CreateStretchRectTransform(string name, Transform parent)
        {
            var rectTransform = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero; // Left, Bottom
            rectTransform.offsetMax = Vector2.zero; // Right, Top
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.gameObject.layer = (int)GameLayer.UI; //LayerMask.NameToLayer("UI");
            return rectTransform;
        }

        public static RectTransform CreateStretchPanel(string name, Transform parent, Color color = default)
        {
            var panel = new GameObject(name).AddComponent<Image>();
            if (color != default)
            {
                panel.color = color; // Set the color if provided
            }
            else
            {
                panel.color = Color.white; // Default color
            }
            var rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero; // Left, Bottom
            rectTransform.offsetMax = Vector2.zero; // Right, Top
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            panel.transform.SetParent(parent, false);
            panel.gameObject.layer = (int)GameLayer.UI; //LayerMask.NameToLayer("UI");
            return rectTransform;
        }

        public static Text CreateText(string name, string text, Transform parent, Color color = default)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            textObject.text = text;
            textObject.alignment = TextAnchor.MiddleCenter;
            textObject.transform.SetParent(parent, false);
            textObject.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            textObject.GetComponent<RectTransform>().anchorMax = Vector2.one;
            textObject.GetComponent<RectTransform>().offsetMin = Vector2.zero; // Left, Bottom
            textObject.GetComponent<RectTransform>().offsetMax = Vector2.zero; // Right, Top
            textObject.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            textObject.color = color == default ? Color.black : color;
            textObject.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textObject.fontSize = 24; // Use a default font
            textObject.gameObject.layer = (int)GameLayer.UI; //LayerMask.NameToLayer("UI");
            return textObject;
        }

        public static Button CreateButton(string name, string label, float width, float height, Transform parent, UnityAction onClick)
        {
            var button = new GameObject(name, typeof(RectTransform), typeof(Image)).AddComponent<Button>();
            button.transform.SetParent(parent, false);
            button.gameObject.layer = (int)GameLayer.UI; //LayerMask.NameToLayer("UI");
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
            button.onClick.AddListener(onClick);

            var text = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.transform.SetParent(button.transform, false);
            text.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            text.GetComponent<RectTransform>().anchorMax = Vector2.one;
            text.GetComponent<RectTransform>().offsetMin = Vector2.zero; // Left, Bottom
            text.GetComponent<RectTransform>().offsetMax = Vector2.zero; // Right, Top
            text.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            text.color = Color.black; // Set text color to black
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24; // Use a default font
            text.gameObject.layer = (int)GameLayer.UI; //LayerMask.NameToLayer("UI");
            return button;
        }

        public static ScrollRect CreateScrollRect(string name, Transform parent)
        {
            // 创建 ScrollRect GameObject
            var go = new GameObject("ScrollRect", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect), typeof(Mask));
            go.gameObject.layer = (int)GameLayer.UI; //LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            // 设置 ScrollRect 大小
            RectTransform scrollRectTrans = go.GetComponent<RectTransform>();
            scrollRectTrans.anchorMin = Vector2.zero;
            scrollRectTrans.anchorMax = Vector2.one;
            scrollRectTrans.offsetMin = Vector2.zero; // Left, Bottom
            scrollRectTrans.offsetMax = Vector2.zero; // Right, Top

            // 创建 Content GameObject
            GameObject contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.layer = (int)GameLayer.UI; //LayerMask.NameToLayer("UI");
            contentGO.transform.SetParent(scrollRectTrans, false);

            RectTransform contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = Vector2.zero; // Left, Bottom
            contentRect.offsetMax = Vector2.zero; // Right, Top
            contentRect.anchoredPosition = Vector2.zero;

            // 设置 ScrollRect 的 Content
            var scrollRect = go.GetComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            return scrollRect;
        }
    }
}