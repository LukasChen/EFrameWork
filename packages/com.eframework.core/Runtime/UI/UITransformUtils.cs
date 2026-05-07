using UnityEngine;
using UnityEngine.UI;

namespace EFramework.Runtime.UI
{
    public static class UITransformUtils
    {
        public static void SetFullStretch(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        public static void SetLayer(RectTransform root, int layer, bool includeChildren)
        {
            if (root == null || layer < 0 || layer > 31)
            {
                return;
            }

            if (!includeChildren)
            {
                root.gameObject.layer = layer;
                return;
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i].gameObject.layer = layer;
            }
        }

        public static void SetRectTransformRelativeToCenter(RectTransform currentRect, RectTransform targetRect, Vector2 offset)
        {
            if (targetRect == null || currentRect == null || currentRect.parent == null) return;
            Vector3 worldPos = targetRect.TransformPoint(targetRect.rect.center + new Vector2(targetRect.pivot.x * targetRect.rect.width - targetRect.rect.width / 2f, targetRect.pivot.y * targetRect.rect.height - targetRect.rect.height / 2f));
            Vector3 localPos = currentRect.parent.InverseTransformPoint(worldPos);
            currentRect.anchoredPosition = (Vector2)localPos + offset;
        }

        public static void SetRectTransformRelativeToTop(RectTransform currentRect, RectTransform targetRect, Vector2 offset)
        {
            if (targetRect == null || currentRect == null || currentRect.parent == null) return;
            Vector3 worldPos = targetRect.TransformPoint(new Vector3(targetRect.rect.width * (0.5f - targetRect.pivot.x), targetRect.rect.height * (1 - targetRect.pivot.y), 0));
            Vector3 localPos = currentRect.parent.InverseTransformPoint(worldPos);
            currentRect.anchoredPosition = (Vector2)localPos + offset;
        }

        public static void SetRectTransformRelativeToBottom(RectTransform currentRect, RectTransform targetRect, Vector2 offset)
        {
            if (targetRect == null || currentRect == null || currentRect.parent == null) return;
            Vector3 worldPos = targetRect.TransformPoint(new Vector3(targetRect.rect.width * (0.5f - targetRect.pivot.x), targetRect.rect.height * -targetRect.pivot.y, 0));
            Vector3 localPos = currentRect.parent.InverseTransformPoint(worldPos);
            currentRect.anchoredPosition = (Vector2)localPos + offset;
        }

        public static void SetRectTransformRelativeToLeft(RectTransform currentRect, RectTransform targetRect, Vector2 offset)
        {
            if (targetRect == null || currentRect == null || currentRect.parent == null) return;
            Vector3 worldPos = targetRect.TransformPoint(new Vector3(targetRect.rect.width * (0 - targetRect.pivot.x), targetRect.rect.height * (0.5f - targetRect.pivot.y), 0));
            Vector3 localPos = currentRect.parent.InverseTransformPoint(worldPos);
            currentRect.anchoredPosition = (Vector2)localPos + offset;
        }

        public static Vector3 GetRectTransformCenter(RectTransform targetRect)
        {
            if (targetRect == null)
            {
                return Vector3.zero;
            }

            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);
            return (corners[0] + corners[1] + corners[2] + corners[3]) / 4f;
        }

        public static void SetAdjustedFillAmount(Image image, float fillAmount, float startCutout = 0.15f, float endCutout = 0.15f)
        {
            if (image == null)
            {
                return;
            }

            float adjustedFill = (1 - startCutout - endCutout) * fillAmount + startCutout;
            image.fillAmount = Mathf.Clamp01(adjustedFill);
        }
    }
}
