using UnityEngine;

namespace EFramework.Runtime.UI.Layout
{
    /// <summary>
    /// 屏幕比例/安全区/ScreenFit 调试输出工具。
    /// 挂到任意场景物体上，在运行时输出 EFrame.Current.UI 的计算结果，方便验证：
    /// - 宽屏：按高度适配，左右留白（ScreenFitRect 居中且非全宽）
    /// - 长屏：按宽度适配，全屏铺满（ScreenFitRect 全屏且 HeightDelta > 0）
    /// - SafeAreaFitter：以内缩发生在 ScreenFitRect 内为准
    /// </summary>
    public class ScreenFitDebugLogger : MonoBehaviour
    {
        [Header("Log")]
        public bool LogOnStart = true;
        public bool LogOnResolutionChange = true;

        [Header("Validate")]
        public bool ValidateOnLog = true;
        [Tooltip("比较浮点时允许的误差")]
        public float Epsilon = 0.01f;

        private int m_lastW;
        private int m_lastH;
        private Rect m_lastSafeArea;

        private void Start()
        {
            CacheState();
            if (LogOnStart)
            {
                Dump();
            }
        }

        private void Update()
        {
            if (!LogOnResolutionChange)
                return;

            if (Screen.width != m_lastW || Screen.height != m_lastH || Screen.safeArea != m_lastSafeArea)
            {
                CacheState();
                Dump();
            }
        }

        private void CacheState()
        {
            m_lastW = Screen.width;
            m_lastH = Screen.height;
            m_lastSafeArea = Screen.safeArea;
        }

        [ContextMenu("Dump")]
        public void Dump()
        {
            if (EFrame.Current?.UI == null)
            {
                Debug.Log("[ScreenFitDebug] EFrame.Current.UI is null");
                return;
            }

            var ui = EFrame.Current.UI as QUI;
            if (ui == null)
            {
                Debug.Log("[ScreenFitDebug] UI service is not QUI, detailed validation skipped.");
                return;
            }

            Debug.Log("================ [ScreenFitDebug] ================");
            Debug.Log($"Screen: {Screen.width}x{Screen.height}, aspect={(float)Screen.width / Screen.height:F4}");
            Debug.Log($"SafeArea(px): {Screen.safeArea}, insets(px) L={Screen.safeArea.xMin:F0} R={(Screen.width - Screen.safeArea.xMax):F0} B={Screen.safeArea.yMin:F0} T={(Screen.height - Screen.safeArea.yMax):F0}");

            Debug.Log($"Design: {ui.DesignWidth}x{ui.DesignHeight}, designAspect={ui.DesignAspect:F4}");
            Debug.Log($"UI: ScreenAspect={ui.ScreenAspect:F4}, IsWideScreen={ui.IsWideScreen}, IsTallScreen={ui.IsTallScreen}, FitMode={ui.FitMode}, EffectiveFitMode={ui.EffectiveFitMode}");
            Debug.Log($"UI: ScaleFactor={ui.ScaleFactor:F6}, ScreenFitRect(px)={ui.ScreenFitRect}, ScaleFitFactor={ui.ScaleFitFactor:F6}");
            Debug.Log($"UI: WidthDelta(design)={ui.WidthDelta:F3}, HeightDelta(design)={ui.HeightDelta:F3}");

            // SafeAreaFitter 关键：以 ScreenFitRect 内为准的安全区内缩
            var fit = ui.ScreenFitRect;
            var sa = ui.SafeAreaRect;
            float leftInsetPx = Mathf.Max(0f, sa.xMin - fit.xMin);
            float rightInsetPx = Mathf.Max(0f, fit.xMax - sa.xMax);
            float bottomInsetPx = Mathf.Max(0f, sa.yMin - fit.yMin);
            float topInsetPx = Mathf.Max(0f, fit.yMax - sa.yMax);

            Debug.Log($"SafeAreaInsideFit(px): L={leftInsetPx:F0} R={rightInsetPx:F0} B={bottomInsetPx:F0} T={topInsetPx:F0}");
            Debug.Log($"SafeAreaInsideFit(design): L={(leftInsetPx / ui.ScaleFactor):F3} R={(rightInsetPx / ui.ScaleFactor):F3} B={(bottomInsetPx / ui.ScaleFactor):F3} T={(topInsetPx / ui.ScaleFactor):F3}");

            if (ValidateOnLog)
            {
                Validate(ui);
            }

            Debug.Log("==================================================");
        }

        private void Validate(QUI ui)
        {
            bool ok = true;

            float screenW = Screen.width;
            float screenH = Screen.height;
            float designW = ui.DesignWidth;
            float designH = ui.DesignHeight;

            if (ui.EffectiveFitMode == ScreenFitMode.FitHeight)
            {
                ok &= AssertNear(ui.ScreenFitRect.height, screenH, "FitHeight: ScreenFitRect.height == Screen.height");
                ok &= AssertNear(ui.ScaleFactor, screenH / designH, "FitHeight: ScaleFactor == ScreenH/DesignH");
                ok &= AssertNear(ui.ScreenFitRect.width, designW * ui.ScaleFactor, "FitHeight: ScreenFitRect.width == DesignW*ScaleFactor");
                ok &= AssertNear(ui.HeightDelta, 0f, "FitHeight: HeightDelta == 0");
            }
            else
            {
                ok &= AssertNear(ui.ScreenFitRect.width, screenW, "FitWidth: ScreenFitRect.width == Screen.width");
                ok &= AssertNear(ui.ScaleFactor, screenW / designW, "FitWidth: ScaleFactor == ScreenW/DesignW");
                ok &= AssertNear(ui.WidthDelta, 0f, "FitWidth: WidthDelta == 0");

                if (ui.HeightDelta >= 0f)
                {
                    ok &= AssertNear(ui.ScreenFitRect.height, screenH, "FitWidth expanded: ScreenFitRect.height == Screen.height");
                }
                else
                {
                    ok &= AssertNear(ui.ScreenFitRect.height, designH * ui.ScaleFactor, "FitWidth cropped: ScreenFitRect.height == DesignH*ScaleFactor");
                }
            }

            Debug.Log(ok ? "[ScreenFitDebug] Validate: OK" : "[ScreenFitDebug] Validate: FAILED (see logs above)");
        }

        private bool AssertNear(float value, float expected, string label)
        {
            if (Mathf.Abs(value - expected) <= Epsilon)
                return true;

            Debug.Log($"[ScreenFitDebug][AssertNear] {label}: value={value:F6}, expected={expected:F6}, eps={Epsilon:F6}");
            return false;
        }

        private bool AssertInRange(float value, float min, float max, string label)
        {
            if (value >= min && value <= max)
                return true;

            Debug.Log($"[ScreenFitDebug][AssertRange] {label}: value={value:F6}, range=[{min:F6},{max:F6}]");
            return false;
        }
    }
}
