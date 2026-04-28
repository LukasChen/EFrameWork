using EFrameWork.Runtime.Asset;
using EFrameWork.Runtime.Utils;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using Object = UnityEngine.Object;

namespace EFrameWork.Runtime.UI
{
    public enum UILayer
    {
        QuiBackground,
        QuiPanel,
        QuiPopUp,
        QuiTooltip,
        QuiEffect,
        QuiTop
    }

    /// <summary>
    /// 屏幕适配模式
    /// </summary>
    public enum ScreenFitMode
    {
        /// <summary>
        /// 按高度适配（宽度可能裁剪或留黑边）- 适合竖屏游戏
        /// </summary>
        FitHeight,
        /// <summary>
        /// 按宽度适配（高度可能裁剪或留黑边）- 适合横屏游戏
        /// </summary>
        FitWidth
    }

    public sealed class QUI
    {
        public EventSystem EventSystem;
        private Dictionary<UILayer, RectTransform> m_uiLayerNodeTable;
        public RectTransform Root;
        public Canvas RootCanvas;
        public Camera UICamera;
        public Rect ScreenFitRect { get; private set; }
        public float ScaleFactor { get; private set; }
        public float ScaleFitFactor { get; private set; }
        public int DesignWidth { get; private set; }
        public int DesignHeight { get; private set; }

        #region 屏幕适配信息

        /// <summary>
        /// 当前适配模式
        /// </summary>
        public ScreenFitMode FitMode { get; private set; }

        /// <summary>
        /// 设计宽高比
        /// </summary>
        public float DesignAspect => (float)DesignWidth / DesignHeight;

        /// <summary>
        /// 实际屏幕宽高比
        /// </summary>
        public float ScreenAspect => (float)Screen.width / Screen.height;

        /// <summary>
        /// 原始 SafeArea（像素坐标）
        /// </summary>
        public Rect SafeAreaRect { get; private set; }

        /// <summary>
        /// SafeArea 各边偏移量（像素，左/下/右/上）
        /// </summary>
        public Vector4 SafeAreaOffset { get; private set; }

        /// <summary>
        /// 是否宽屏（屏幕比设计宽）
        /// </summary>
        public bool IsWideScreen => ScreenAspect >= DesignAspect;

        /// <summary>
        /// 是否窄长屏（屏幕比设计高）
        /// </summary>
        public bool IsTallScreen => ScreenAspect < DesignAspect;

        /// <summary>
        /// 设计分辨率与实际分辨率的宽度差异（像素，正值表示屏幕更宽）
        /// </summary>
        public float WidthDelta { get; private set; }

        /// <summary>
        /// 设计分辨率与实际分辨率的高度差异（像素，正值表示屏幕更高）
        /// </summary>
        public float HeightDelta { get; private set; }

        #endregion

        #region View 缓存池

        private Dictionary<string, Stack<BindingViewBase>> m_viewCache = new();

        /// <summary>
        /// 从缓存池获取 View，如果缓存中没有则返回 null
        /// </summary>
        /// <typeparam name="T">View 类型</typeparam>
        /// <param name="assetPath">资源路径（作为缓存 key）</param>
        /// <returns>缓存的 View 实例，或 null</returns>
        public T GetViewFromCache<T>(string assetPath) where T : BindingViewBase
        {
            if (m_viewCache.TryGetValue(assetPath, out var stack) && stack.Count > 0)
            {
                var view = stack.Pop() as T;
                if (view != null && !view.IsDisposed)
                {
                    view.gameObject.SetActive(true);
                    return view;
                }
            }
            return null;
        }

        /// <summary>
        /// 回收 View 到缓存池
        /// </summary>
        /// <param name="assetPath">资源路径（作为缓存 key）</param>
        /// <param name="view">要回收的 View</param>
        public void RecycleViewToCache(string assetPath, BindingViewBase view)
        {
            if (view == null || view.IsDisposed) return;

            if (!m_viewCache.ContainsKey(assetPath))
            {
                m_viewCache[assetPath] = new Stack<BindingViewBase>();
            }

            view.gameObject.SetActive(false);
            view.transform.SetParent(null, false);
            m_viewCache[assetPath].Push(view);
        }

        /// <summary>
        /// 清空指定资源路径的缓存
        /// </summary>
        /// <param name="assetPath">资源路径，为 null 则清空所有缓存</param>
        public void ClearViewCache(string assetPath = null)
        {
            if (assetPath == null)
            {
                foreach (var kvp in m_viewCache)
                {
                    foreach (var view in kvp.Value)
                    {
                        if (view != null && !view.IsDisposed)
                        {
                            Object.Destroy(view.gameObject);
                        }
                    }
                    kvp.Value.Clear();
                }
                m_viewCache.Clear();
            }
            else if (m_viewCache.TryGetValue(assetPath, out var stack))
            {
                foreach (var view in stack)
                {
                    if (view != null && !view.IsDisposed)
                    {
                        Object.Destroy(view.gameObject);
                    }
                }
                stack.Clear();
                m_viewCache.Remove(assetPath);
            }
        }

        #endregion

        #region View 栈管理（可选）

        private Stack<BindingViewBase> m_viewStack = new();

        /// <summary>
        /// 入栈打开 View（用于需要返回导航的场景）
        /// </summary>
        /// <param name="view">要打开的 View</param>
        /// <param name="layer">UI 层级</param>
        public void PushView(BindingViewBase view, UILayer layer)
        {
            if (view == null) return;

            m_viewStack.Push(view);
            view.Open(layer);
        }

        /// <summary>
        /// 返回（关闭栈顶 View）
        /// </summary>
        /// <returns>是否成功关闭</returns>
        public bool PopView()
        {
            if (m_viewStack.Count > 0)
            {
                var view = m_viewStack.Pop();
                if (view != null && !view.IsDisposed)
                {
                    view.Close();
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 返回到指定类型的 View（关闭其上所有 View）
        /// </summary>
        /// <typeparam name="T">目标 View 类型</typeparam>
        public void PopTo<T>() where T : BindingViewBase
        {
            while (m_viewStack.Count > 0)
            {
                var top = m_viewStack.Peek();
                if (top is T)
                {
                    break;
                }
                PopView();
            }
        }

        /// <summary>
        /// 关闭栈中所有 View
        /// </summary>
        public void PopAll()
        {
            while (m_viewStack.Count > 0)
            {
                PopView();
            }
        }

        /// <summary>
        /// 获取栈顶 View
        /// </summary>
        public BindingViewBase TopView => m_viewStack.Count > 0 ? m_viewStack.Peek() : null;

        /// <summary>
        /// 当前栈中 View 数量
        /// </summary>
        public int ViewStackCount => m_viewStack.Count;

        /// <summary>
        /// 从栈中移除指定 View（用于 View 直接关闭时同步栈状态）
        /// </summary>
        internal void RemoveFromStack(BindingViewBase view)
        {
            if (view == null || m_viewStack.Count == 0) return;

            // 如果是栈顶，直接 Pop
            if (m_viewStack.Peek() == view)
            {
                m_viewStack.Pop();
                return;
            }

            // 否则需要重建栈（较少发生）
            var tempList = new List<BindingViewBase>(m_viewStack);
            tempList.Remove(view);
            m_viewStack.Clear();
            for (int i = tempList.Count - 1; i >= 0; i--)
            {
                m_viewStack.Push(tempList[i]);
            }
        }

        #endregion

        public Vector2 GetCameraSize()
        {
            float height = 2f * UICamera.orthographicSize;
            float width = height * UICamera.aspect;
            return new Vector2(width, height);
        }

        public Rect GetCameraRect()
        {
            float height = 2f * UICamera.orthographicSize;
            float width = height * UICamera.aspect;
            Vector3 pos = UICamera.transform.position;
            return new Rect(pos.x - width / 2, pos.y - height / 2, width, height);
        }

        public RectTransform UILayer(UILayer uILayerType)
        {
            return m_uiLayerNodeTable[uILayerType];
        }

        public void Init(Camera uiCamera, int designWidth, int designHeight, ScreenFitMode fitMode = ScreenFitMode.FitHeight)
        {
            UICamera = uiCamera;
            DesignWidth = designWidth;
            DesignHeight = designHeight;
            FitMode = fitMode;

            GameObject rootObject = new("UIRoot");
            Root = rootObject.AddComponent<RectTransform>();
            Object.DontDestroyOnLoad(rootObject);

            rootObject.layer = (int)GameLayer.UI;
            RootCanvas = rootObject.AddComponent<Canvas>();
            EventSystem = rootObject.AddComponent<EventSystem>();
            AddCompatibleInputModule(rootObject);

            RootCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            RootCanvas.worldCamera = UICamera;
            RootCanvas.vertexColorAlwaysGammaSpace = true;

            // 计算屏幕适配
            CalculateScreenFit();

            CanvasScaler canvasScaler = rootObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasScaler.scaleFactor = ScaleFactor;
            canvasScaler.referencePixelsPerUnit = 100;
            CreateUILayers();
        }

        private static void AddCompatibleInputModule(GameObject rootObject)
        {
#if ENABLE_INPUT_SYSTEM
            rootObject.AddComponent<InputSystemUIInputModule>();
#else
            rootObject.AddComponent<StandaloneInputModule>();
#endif
        }

        /// <summary>
        /// 计算屏幕适配参数
        /// 策略：
        /// - 宽屏（屏幕比设计宽）：按高度适配，UILayer 固定设计尺寸居中，两侧留空
        /// - 长屏（屏幕比设计窄长）：按宽度适配，UILayer 全屏铺满
        /// </summary>
        private void CalculateScreenFit()
        {
            Debug.Log($"Screen: width={Screen.width}, height={Screen.height}, safeArea={Screen.safeArea}");

            Rect safeArea = Screen.safeArea;
            SafeAreaRect = safeArea;

            // 计算 SafeArea 偏移量（左/下/右/上）
            SafeAreaOffset = new Vector4(
                safeArea.xMin,
                safeArea.yMin,
                Screen.width - safeArea.xMax,
                Screen.height - safeArea.yMax
            );

            // 注意：UILayer 只负责“屏幕比例适配”，不参与 SafeArea 裁切。
            // SafeAreaFitter/FullScreenFitter 将基于 SafeAreaRect/ScreenFitRect 各自处理。

            float screenW = Screen.width;
            float screenH = Screen.height;

            float screenAspect = screenW / screenH;
            float designAspect = DesignAspect;

            if (screenAspect >= designAspect)
            {
                // 宽屏：按高度适配，保证设计高度完整显示，两侧留空
                ScaleFactor = screenH / DesignHeight;

                float fitWidth = DesignWidth * ScaleFactor;
                ScreenFitRect = new Rect((screenW - fitWidth) * 0.5f, 0f, fitWidth, screenH);

                // 设计坐标系下的差值（用于扩展/留白计算）
                WidthDelta = (screenW / ScaleFactor) - DesignWidth;
                HeightDelta = 0f;
            }
            else
            {
                // 长屏：按宽度适配，保证设计宽度完整显示；高度扩展铺满全屏
                ScaleFactor = screenW / DesignWidth;

                ScreenFitRect = new Rect(0f, 0f, screenW, screenH);

                WidthDelta = 0f;
                HeightDelta = (screenH / ScaleFactor) - DesignHeight;
            }

            ScaleFitFactor = ScreenFitRect.width / Screen.width;

            Debug.Log($"FitMode: {FitMode}, ScreenFitRect: {ScreenFitRect}, ScaleFactor: {ScaleFactor}");
            Debug.Log($"IsWideScreen: {IsWideScreen}, IsTallScreen: {IsTallScreen}, WidthDelta: {WidthDelta}, HeightDelta: {HeightDelta}");
        }

        private void CreateUILayers()
        {
            m_uiLayerNodeTable = new Dictionary<UILayer, RectTransform>();

            foreach (string layer in Enum.GetNames(typeof(UILayer)))
            {
                UILayer layerType = (UILayer)Enum.Parse(typeof(UILayer), layer);

                if (layerType == UI.UILayer.QuiBackground)
                {
                    CreateWorldBackgroundUINode();
                    continue;
                }

                GameObject layerObject = new(layer);
                layerObject.layer = (int)GameLayer.UI; //LayerMask.NameToLayer("UI");
                RectTransform layerNode = layerObject.AddComponent<RectTransform>();
                Canvas layerCanvas = layerObject.AddComponent<Canvas>();
                layerObject.AddComponent<GraphicRaycaster>();
                layerNode.SetParent(RootCanvas.transform, false);

                layerCanvas.overrideSorting = true;
                layerCanvas.sortingLayerName = layer;

                // 根据屏幕类型设置 UILayer 尺寸
                layerNode.pivot = new Vector2(0.5f, 0.5f);
                layerNode.anchoredPosition = Vector2.zero;

                if (IsWideScreen)
                {
                    // 宽屏：UILayer 固定为设计尺寸，居中显示
                    layerNode.anchorMin = new Vector2(0.5f, 0.5f);
                    layerNode.anchorMax = new Vector2(0.5f, 0.5f);
                    layerNode.sizeDelta = new Vector2(DesignWidth, DesignHeight);
                }
                else
                {
                    // 长屏：UILayer 全屏铺满（宽度为设计宽度，高度扩展）
                    layerNode.anchorMin = new Vector2(0.5f, 0.5f);
                    layerNode.anchorMax = new Vector2(0.5f, 0.5f);
                    // 高度扩展为实际屏幕高度（设计坐标系下）
                    float actualHeight = DesignHeight + HeightDelta;
                    layerNode.sizeDelta = new Vector2(DesignWidth, actualHeight);
                }

                m_uiLayerNodeTable.Add(layerType, layerNode);
            }
        }

        private void CreateWorldBackgroundUINode()
        {
            var bgCamera = EFrame.SceneCamera;
            if (bgCamera == null) bgCamera = UICamera;

            float cameraDistance = 20f;

            // 计算相机在该距离下的视野尺寸
            float heightAtDistance = 2f * cameraDistance * Mathf.Tan(bgCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float widthAtDistance = heightAtDistance * bgCamera.aspect;

            // ========== 第一层：Canvas（完全覆盖相机视野）==========
            string canvasName = "BackgroundCanvas";
            GameObject canvasObject = new(canvasName);
            canvasObject.layer = (int)GameLayer.Default;

            RectTransform canvasRect = canvasObject.AddComponent<RectTransform>();
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.sortingOrder = -100;
            canvasObject.AddComponent<GraphicRaycaster>();
            canvas.vertexColorAlwaysGammaSpace = true;

            // Canvas 尺寸设为相机视野尺寸（世界单位）
            canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
            canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.sizeDelta = new Vector2(widthAtDistance, heightAtDistance);
            canvasRect.anchoredPosition = Vector2.zero;
            canvasRect.localScale = Vector3.one;

            // 放置到相机下
            canvasObject.transform.SetParent(bgCamera.transform, false);
            Vector3 canvasPos = bgCamera.transform.position + bgCamera.transform.forward * cameraDistance;
            canvasObject.transform.position = canvasPos;

            // ========== 第二层：BackgroundLayer（屏幕比例适配）==========
            string layerName = UI.UILayer.QuiBackground.ToString();
            GameObject layerObject = new(layerName);
            layerObject.layer = (int)GameLayer.Default;

            RectTransform bgNode = layerObject.AddComponent<RectTransform>();
            bgNode.SetParent(canvasRect, false);

            // 计算 Canvas 内的缩放因子（将设计坐标系映射到世界坐标系）
            // Canvas 宽度 = widthAtDistance（世界单位），设计宽度 = DesignWidth
            float canvasScaleFactor;
            if (IsWideScreen)
            {
                // 宽屏：按高度适配
                canvasScaleFactor = heightAtDistance / DesignHeight;
            }
            else
            {
                // 长屏：按宽度适配
                canvasScaleFactor = widthAtDistance / DesignWidth;
            }

            // BackgroundLayer 尺寸设置（设计坐标系）
            bgNode.anchorMin = new Vector2(0.5f, 0.5f);
            bgNode.anchorMax = new Vector2(0.5f, 0.5f);
            bgNode.pivot = new Vector2(0.5f, 0.5f);
            bgNode.anchoredPosition = Vector2.zero;
            bgNode.localScale = new Vector3(canvasScaleFactor, canvasScaleFactor, 1);

            if (IsWideScreen)
            {
                // 宽屏：固定设计尺寸，居中显示
                bgNode.sizeDelta = new Vector2(DesignWidth, DesignHeight);
            }
            else
            {
                // 长屏：宽度为设计宽度，高度扩展
                float actualHeight = DesignHeight + HeightDelta;
                bgNode.sizeDelta = new Vector2(DesignWidth, actualHeight);
            }

            m_uiLayerNodeTable.Add(UI.UILayer.QuiBackground, bgNode);
        }

        public void SetInteractive(bool isInteractive)
        {
            EventSystem.enabled = isInteractive;
        }

        #region UIBinding

        public void OpenBindingView(BindingViewBase bindingViewBase, UILayer uiLayerType = UI.UILayer.QuiPanel)
        {
            bindingViewBase.transform.SetParent(m_uiLayerNodeTable[uiLayerType], false);
        }
        #endregion
    }
}
