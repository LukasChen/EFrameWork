using System.Collections.Generic;
using EFramework.Runtime.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EFramework.Editor.AILoop
{
    internal static class EFrameUiElementAnnotator
    {
        private const int OverlaySortingOrder = 32767;
        private const int LabelFontSize = 20;
        private static readonly Vector3[] WorldCorners = new Vector3[4];
        private static readonly List<RaycastResult> RaycastResults = new();

        public static List<EFrameUiElementInfo> CollectInteractiveElements()
        {
            List<EFrameUiElementInfo> elements = new();
            HashSet<GameObject> processed = new();

            foreach (Selectable selectable in Selectable.allSelectablesArray)
            {
                if (selectable == null || !selectable.isActiveAndEnabled || !selectable.IsInteractable())
                {
                    continue;
                }

                processed.Add(selectable.gameObject);
                TryAddElement(elements, selectable.gameObject, ClassifySelectable(selectable));
            }

            MonoBehaviour[] behaviours = FindObjectsByTypeCompat<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null || !behaviour.isActiveAndEnabled || processed.Contains(behaviour.gameObject))
                {
                    continue;
                }

                string type = ClassifyEventHandler(behaviour);
                if (type == null)
                {
                    continue;
                }

                processed.Add(behaviour.gameObject);
                TryAddElement(elements, behaviour.gameObject, type);
            }

            return elements;
        }

        public static void AssignLabels(List<EFrameUiElementInfo> elements)
        {
            elements.Sort((a, b) =>
            {
                int sort = b.SortingOrder.CompareTo(a.SortingOrder);
                return sort != 0 ? sort : b.SiblingIndex.CompareTo(a.SiblingIndex);
            });

            for (int i = 0; i < elements.Count; i++)
            {
                elements[i].Label = GenerateLabel(i);
            }
        }

        public static void ConvertToTopLeftCoordinates(List<EFrameUiElementInfo> elements, int screenHeight)
        {
            foreach (EFrameUiElementInfo element in elements)
            {
                float originalMinY = element.BoundsMinY;
                element.Y = screenHeight - element.Y;
                element.BoundsMinY = screenHeight - element.BoundsMaxY;
                element.BoundsMaxY = screenHeight - originalMinY;
            }
        }

        public static GameObject CreateAnnotationOverlay(
            List<EFrameUiElementInfo> elements,
            float outputResolutionScale)
        {
            GameObject root = new("__EFrameAI_UiAnnotation__")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (EFrameUiElementInfo element in elements)
            {
                CreateAnnotation(root.transform, element, font, outputResolutionScale);
            }

            return root;
        }

        private static void TryAddElement(List<EFrameUiElementInfo> elements, GameObject target, string type)
        {
            if (!target.TryGetComponent(out RectTransform rectTransform))
            {
                return;
            }

            Canvas canvas = target.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.GetComponent<GraphicRaycaster>() == null)
            {
                return;
            }

            Camera camera = ResolveCanvasCamera(canvas);
            rectTransform.GetWorldCorners(WorldCorners);

            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, WorldCorners[0]);
            Vector2 max = min;
            for (int i = 1; i < WorldCorners.Length; i++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, WorldCorners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            Vector2 center = (min + max) * 0.5f;
            if (!IsRaycastReachable(target, center))
            {
                return;
            }

            QUIBinding binding = target.GetComponentInParent<QUIBinding>();
            elements.Add(new EFrameUiElementInfo
            {
                Name = target.name,
                Type = type,
                Interaction = GetInteraction(type),
                Path = GetPath(target),
                BindingName = binding != null ? binding.name : "",
                BindingPath = binding != null ? GetPath(binding.gameObject) : "",
                X = center.x,
                Y = center.y,
                BoundsMinX = min.x,
                BoundsMinY = min.y,
                BoundsMaxX = max.x,
                BoundsMaxY = max.y,
                SortingOrder = canvas.sortingOrder,
                SiblingIndex = target.transform.GetSiblingIndex()
            });
        }

        private static bool IsRaycastReachable(GameObject target, Vector2 screenPosition)
        {
            EventSystem eventSystem = EFrameMouseUiSimulator.ResolveEventSystem();
            if (eventSystem == null)
            {
                return true;
            }

            RaycastResults.Clear();
            PointerEventData pointerData = new(eventSystem)
            {
                position = screenPosition
            };
            eventSystem.RaycastAll(pointerData, RaycastResults);
            if (RaycastResults.Count == 0)
            {
                return true;
            }

            GameObject hit = RaycastResults[0].gameObject;
            return hit == target || hit.transform.IsChildOf(target.transform) || target.transform.IsChildOf(hit.transform);
        }

        private static T[] FindObjectsByTypeCompat<T>() where T : Object
        {
#pragma warning disable 0618
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#pragma warning restore 0618
        }

        private static void CreateAnnotation(
            Transform parent,
            EFrameUiElementInfo element,
            Font font,
            float outputResolutionScale)
        {
            Color color = Color.HSVToRGB(Mathf.Abs(element.Label.GetHashCode() % 1000) / 1000f, 0.85f, 1f);
            color.a = 0.95f;

            float minX = element.BoundsMinX;
            float maxX = element.BoundsMaxX;
            float minY = element.BoundsMinY;
            float maxY = element.BoundsMaxY;

            CreateLine(parent, new Vector2(minX, minY), new Vector2(maxX, minY), color);
            CreateLine(parent, new Vector2(maxX, minY), new Vector2(maxX, maxY), color);
            CreateLine(parent, new Vector2(maxX, maxY), new Vector2(minX, maxY), color);
            CreateLine(parent, new Vector2(minX, maxY), new Vector2(minX, minY), color);
            CreateLabel(parent, element.Label, new Vector2(minX, maxY + 4f), color, font, outputResolutionScale);
        }

        private static void CreateLine(Transform parent, Vector2 from, Vector2 to, Color color)
        {
            GameObject line = new("Line");
            line.transform.SetParent(parent, false);
            Image image = line.AddComponent<Image>();
            image.color = color;
            RectTransform rect = image.rectTransform;
            Vector2 delta = to - from;
            rect.pivot = new Vector2(0f, 0.5f);
            rect.position = from;
            rect.sizeDelta = new Vector2(delta.magnitude, 3f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static void CreateLabel(
            Transform parent,
            string text,
            Vector2 position,
            Color color,
            Font font,
            float outputResolutionScale)
        {
            GameObject label = new("Label");
            label.transform.SetParent(parent, false);
            Text uiText = label.AddComponent<Text>();
            uiText.text = text;
            uiText.font = font;
            uiText.fontSize = Mathf.Max(12, Mathf.RoundToInt(LabelFontSize / Mathf.Max(0.1f, outputResolutionScale)));
            uiText.fontStyle = FontStyle.Bold;
            uiText.color = color;
            uiText.alignment = TextAnchor.MiddleLeft;
            RectTransform rect = uiText.rectTransform;
            rect.position = position;
            rect.sizeDelta = new Vector2(80f, 28f);
        }

        private static Camera ResolveCanvasCamera(Canvas canvas)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        private static string ClassifySelectable(Selectable selectable)
        {
            if (selectable is Button) return "Button";
            if (selectable is Toggle) return "Toggle";
            if (selectable is Slider) return "Slider";
            if (selectable is Dropdown) return "Dropdown";
            if (selectable is InputField) return "InputField";
            if (selectable is Scrollbar) return "Scrollbar";
            if (selectable is IDragHandler) return "Draggable";
            if (selectable is IDropHandler) return "DropTarget";
            return "Selectable";
        }

        private static string ClassifyEventHandler(MonoBehaviour behaviour)
        {
            if (behaviour is IDragHandler) return "Draggable";
            if (behaviour is IDropHandler) return "DropTarget";
            if (behaviour is IPointerClickHandler) return "Button";
            if (behaviour is IPointerDownHandler) return "Button";
            return null;
        }

        private static string GetInteraction(string type)
        {
            if (type == "Draggable" || type == "Slider" || type == "Scrollbar") return "Drag";
            if (type == "DropTarget") return "Drop";
            if (type == "InputField") return "Text";
            return "Click";
        }

        private static string GenerateLabel(int index)
        {
            string label = "";
            int remaining = index;
            do
            {
                label = (char)('A' + remaining % 26) + label;
                remaining = remaining / 26 - 1;
            } while (remaining >= 0);

            return label;
        }

        internal static string GetPath(GameObject gameObject)
        {
            List<string> names = new();
            Transform current = gameObject.transform;
            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }
    }
}
