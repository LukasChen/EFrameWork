using UnityEngine;

namespace EFramework.Runtime.UI.Layout
{
    /// <summary>
    /// 安全区适配组件 - 将 UI 元素限制在安全区内
    /// 用于需要避开刘海屏、圆角等区域的 UI 内容
    /// 只应挂在安全区容器节点上，不要直接挂在按钮、文本、局部面板等具体内容节点上。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Header("安全区容器适配")]
        [Tooltip("适配左边距。组件应挂在安全区容器节点上。")]
        public bool FitLeft = true;
        [Tooltip("适配右边距。组件应挂在安全区容器节点上。")]
        public bool FitRight = true;
        [Tooltip("适配上边距。组件应挂在安全区容器节点上。")]
        public bool FitTop = true;
        [Tooltip("适配下边距。组件应挂在安全区容器节点上。")]
        public bool FitBottom = true;

        private RectTransform m_rectTransform;

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            ApplySafeArea();
        }

        private void Start()
        {
            ApplySafeArea();
        }

        /// <summary>
        /// 应用安全区适配
        /// </summary>
        public void ApplySafeArea()
        {
            CacheComponents();
            if (m_rectTransform == null) return;
            if (EFrame.Current?.UI == null) return;

            var ui = EFrame.Current.UI;
            float scaleFactor = ui.ScaleFactor;
            if (scaleFactor <= 0f) return;

            Rect safeArea = ui.SafeAreaRect;
            Rect fitRect = ui.ScreenFitRect;

            // 计算各边需要内缩的距离（转换为设计坐标系）
            // 注意：SafeArea 是相对“屏幕”的像素坐标；而 UI 可能存在宽屏留边（ScreenFitRect）。
            // 因此这里以内缩发生在 ScreenFitRect 内为准（避免刘海在留白区时仍错误内缩）。
            float leftInsetPx = FitLeft ? Mathf.Max(0f, safeArea.xMin - fitRect.xMin) : 0f;
            float rightInsetPx = FitRight ? Mathf.Max(0f, fitRect.xMax - safeArea.xMax) : 0f;
            float bottomInsetPx = FitBottom ? Mathf.Max(0f, safeArea.yMin - fitRect.yMin) : 0f;
            float topInsetPx = FitTop ? Mathf.Max(0f, fitRect.yMax - safeArea.yMax) : 0f;

            float leftInset = leftInsetPx / scaleFactor;
            float rightInset = rightInsetPx / scaleFactor;
            float bottomInset = bottomInsetPx / scaleFactor;
            float topInset = topInsetPx / scaleFactor;

            // 设置锚点为全屏
            m_rectTransform.anchorMin = Vector2.zero;
            m_rectTransform.anchorMax = Vector2.one;

            // 设置偏移以适配安全区
            m_rectTransform.offsetMin = new Vector2(leftInset, bottomInset);
            m_rectTransform.offsetMax = new Vector2(-rightInset, -topInset);
        }

        private void CacheComponents()
        {
            if (m_rectTransform == null)
            {
                m_rectTransform = GetComponent<RectTransform>();
            }
        }
    }
}
