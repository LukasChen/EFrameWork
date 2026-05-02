using UnityEngine;
using UnityEngine.UI;

namespace EFramework.Runtime.UI.Layout
{
    public enum ImageScaleFitMode
    {
        Contain = 1,
        Cover = 2,
        Stretch = 3,
        FitWidth = 4,
        FitHeight = 5
    }

    /// <summary>
    /// Scale image size to fit the parent RectTransform.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ImageScaleFitter : MonoBehaviour
    {
        private RectTransform m_rectTransform;
        private Image m_image;
        private RawImage m_rawImage;
        private bool m_isApplying;
        private Vector2 m_fallbackSourceSize;
#if UNITY_EDITOR
        private bool m_editorApplyQueued;
#endif

        [SerializeField] private ImageScaleFitMode m_fitMode = ImageScaleFitMode.Contain;

        private void Awake()
        {
            CacheComponents();
            CacheFallbackSize();
        }

        private void OnEnable()
        {
            ApplyFit();
        }

        private void Start()
        {
            ApplyFit();
        }

        private void OnTransformParentChanged()
        {
            ApplyFit();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled || m_isApplying)
            {
                return;
            }

            ApplyFit();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            CacheComponents();

            if (!Application.isPlaying)
            {
                CacheFallbackSize();
                QueueEditorApplyFit();
                return;
            }

            ApplyFit();
        }

        private void QueueEditorApplyFit()
        {
            if (m_editorApplyQueued)
            {
                return;
            }

            m_editorApplyQueued = true;
            UnityEditor.EditorApplication.delayCall += ApplyFitFromEditorDelay;
        }

        private void ApplyFitFromEditorDelay()
        {
            m_editorApplyQueued = false;

            if (this == null || !isActiveAndEnabled)
            {
                return;
            }

            ApplyFit();
        }
#endif

        [ContextMenu("Apply Fit")]
        public void ApplyFit()
        {
            CacheComponents();
            NormalizeFitMode();

            RectTransform parentRect = m_rectTransform != null ? m_rectTransform.parent as RectTransform : null;
            if (parentRect == null)
            {
                return;
            }

            Vector2 parentSize = parentRect.rect.size;
            if (parentSize.x <= 0f || parentSize.y <= 0f)
            {
                return;
            }

            Vector2 sourceSize = GetSourceSize();
            if (sourceSize.x <= 0f || sourceSize.y <= 0f)
            {
                return;
            }

            Vector2 targetSize = CalculateTargetSize(parentSize, sourceSize);

            m_isApplying = true;
            m_rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetSize.x);
            m_rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetSize.y);
            m_isApplying = false;
        }

        private void CacheComponents()
        {
            if (m_rectTransform == null)
            {
                m_rectTransform = GetComponent<RectTransform>();
            }

            if (m_image == null)
            {
                TryGetComponent(out m_image);
            }

            if (m_rawImage == null)
            {
                TryGetComponent(out m_rawImage);
            }
        }

        private void CacheFallbackSize()
        {
            if (m_rectTransform == null)
            {
                return;
            }

            Vector2 currentSize = m_rectTransform.rect.size;
            if (currentSize.x > 0f && currentSize.y > 0f)
            {
                m_fallbackSourceSize = currentSize;
            }
        }

        private Vector2 GetSourceSize()
        {
            if (TryGetImageSourceSize(out Vector2 size))
            {
                return size;
            }

            CacheFallbackSize();
            return m_fallbackSourceSize;
        }

        private bool TryGetImageSourceSize(out Vector2 size)
        {
            if (m_image != null && m_image.sprite != null)
            {
                Sprite sprite = m_image.sprite;
                Canvas canvas = GetComponentInParent<Canvas>();
                float referencePixelsPerUnit = canvas != null ? canvas.referencePixelsPerUnit : 100f;
                float pixelsPerUnit = sprite.pixelsPerUnit / referencePixelsPerUnit;

                if (pixelsPerUnit > 0f)
                {
                    size = sprite.rect.size / pixelsPerUnit;
                    return true;
                }
            }

            if (m_rawImage != null && m_rawImage.texture != null)
            {
                size = new Vector2(m_rawImage.texture.width, m_rawImage.texture.height);
                return true;
            }

            size = Vector2.zero;
            return false;
        }

        private Vector2 CalculateTargetSize(Vector2 parentSize, Vector2 sourceSize)
        {
            switch (m_fitMode)
            {
                case ImageScaleFitMode.Contain:
                    return ScaleByAspect(parentSize, sourceSize, useMaxScale: false);
                case ImageScaleFitMode.Cover:
                    return ScaleByAspect(parentSize, sourceSize, useMaxScale: true);
                case ImageScaleFitMode.Stretch:
                    return parentSize;
                case ImageScaleFitMode.FitWidth:
                    return ScaleToWidth(parentSize.x, sourceSize, keepAspect: true);
                case ImageScaleFitMode.FitHeight:
                    return ScaleToHeight(parentSize.y, sourceSize, keepAspect: true);
            }

            return ScaleByAspect(parentSize, sourceSize, useMaxScale: false);
        }

        private static Vector2 ScaleByAspect(Vector2 parentSize, Vector2 sourceSize, bool useMaxScale)
        {
            float widthScale = parentSize.x / sourceSize.x;
            float heightScale = parentSize.y / sourceSize.y;
            float scale = useMaxScale ? Mathf.Max(widthScale, heightScale) : Mathf.Min(widthScale, heightScale);
            return sourceSize * scale;
        }

        private static Vector2 ScaleToWidth(float targetWidth, Vector2 sourceSize, bool keepAspect)
        {
            if (!keepAspect)
            {
                return new Vector2(targetWidth, sourceSize.y);
            }

            float scale = targetWidth / sourceSize.x;
            return sourceSize * scale;
        }

        private static Vector2 ScaleToHeight(float targetHeight, Vector2 sourceSize, bool keepAspect)
        {
            if (!keepAspect)
            {
                return new Vector2(sourceSize.x, targetHeight);
            }

            float scale = targetHeight / sourceSize.y;
            return sourceSize * scale;
        }

        private void NormalizeFitMode()
        {
            if ((int)m_fitMode <= 0)
            {
                m_fitMode = ImageScaleFitMode.Contain;
            }
        }
    }
}
