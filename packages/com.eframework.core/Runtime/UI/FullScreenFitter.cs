using UnityEngine;

namespace EFrameWork.Runtime.UI
{
    /// <summary>
    /// 全屏适配组件 - 将 UI 元素扩展到安全区之外的全屏区域
    /// 用于背景图、遮罩等需要覆盖整个屏幕的元素
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FullScreenFitter : MonoBehaviour
    {
        [Header("扩展方向")]
        [Tooltip("向左扩展到屏幕边缘")]
        public bool FitLeft = true;
        [Tooltip("向右扩展到屏幕边缘")]
        public bool FitRight = true;
        [Tooltip("向上扩展到屏幕边缘")]
        public bool FitTop = true;
        [Tooltip("向下扩展到屏幕边缘")]
        public bool FitBottom = true;

        private RectTransform m_rectTransform;

        private void Awake()
        {
            m_rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            ApplyFullScreen();
        }

        /// <summary>
        /// 应用全屏适配
        /// </summary>
        public void ApplyFullScreen()
        {
            if (EFrame.UI == null) return;

            var ui = EFrame.UI;
            float scaleFactor = ui.ScaleFactor;

            // 计算各边需要扩展的距离（转换为设计坐标系）
            float leftExpand = FitLeft ? ui.ScreenFitRect.xMin / scaleFactor : 0;
            float rightExpand = FitRight ? (Screen.width - ui.ScreenFitRect.xMax) / scaleFactor : 0;
            float bottomExpand = FitBottom ? ui.ScreenFitRect.yMin / scaleFactor : 0;
            float topExpand = FitTop ? (Screen.height - ui.ScreenFitRect.yMax) / scaleFactor : 0;

            // 保存原始锚点和偏移
            Vector2 anchorMin = m_rectTransform.anchorMin;
            Vector2 anchorMax = m_rectTransform.anchorMax;
            Vector2 offsetMin = m_rectTransform.offsetMin;
            Vector2 offsetMax = m_rectTransform.offsetMax;

            // 向左扩展
            if (FitLeft)
            {
                anchorMin.x = 0;
                offsetMin.x = -leftExpand;
            }

            // 向右扩展
            if (FitRight)
            {
                anchorMax.x = 1;
                offsetMax.x = rightExpand;
            }

            // 向下扩展
            if (FitBottom)
            {
                anchorMin.y = 0;
                offsetMin.y = -bottomExpand;
            }

            // 向上扩展
            if (FitTop)
            {
                anchorMax.y = 1;
                offsetMax.y = topExpand;
            }

            m_rectTransform.anchorMin = anchorMin;
            m_rectTransform.anchorMax = anchorMax;
            m_rectTransform.offsetMin = offsetMin;
            m_rectTransform.offsetMax = offsetMax;
        }
    }
}
