using EFramework.Runtime.Asset;
using EFramework.Runtime.UI.Layout;
using EFramework.Runtime.UI.Handles;
using EFramework.Runtime.UI.Transitions;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using Object = UnityEngine.Object;

namespace EFramework.Runtime.UI
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
        FitHeight = 0,
        /// <summary>
        /// 按宽度适配（高度可能裁剪或留黑边）- 适合横屏游戏
        /// </summary>
        FitWidth = 1,
        /// <summary>
        /// 自动适配：宽屏按高度适配，窄长屏按宽度适配
        /// </summary>
        Auto = 2
    }

    public sealed class QUI : IUIService
    {
        private const string DefaultLayerName = "Default";
        private const string UiLayerName = "UI";
        private readonly IUIViewTransition m_defaultTransition = new ScaleFadeViewTransition();
        private readonly Dictionary<string, IUIViewTransition> m_transitionCache = new();

        public EventSystem EventSystem;
        private Dictionary<UILayer, RectTransform> m_uiLayerNodeTable;
        public RectTransform Root { get; private set; }
        public Canvas RootCanvas;
        private CanvasScaler m_rootCanvasScaler;
        public Camera UICamera { get; private set; }
        public IUIViewTransition DefaultTransition => m_defaultTransition;
        public Rect ScreenFitRect { get; private set; }
        public float ScaleFactor { get; private set; }
        public float ScaleFitFactor { get; private set; }
        public int DesignWidth { get; private set; }
        public int DesignHeight { get; private set; }
        public bool EnableScreenFitDebugLog { get; private set; }
        private UISceneCameraBinder m_sceneCameraBinder;
        private GameObject m_backgroundCanvasObject;
        private RectTransform m_backgroundCanvasRect;
        private RectTransform m_backgroundNode;
        private Canvas m_backgroundCanvas;
        private Camera m_backgroundCamera;

        #region 屏幕适配信息

        /// <summary>
        /// 当前适配模式
        /// </summary>
        public ScreenFitMode FitMode { get; private set; }

        /// <summary>
        /// 当前实际使用的适配模式。Auto 会根据屏幕宽高比解析为 FitHeight 或 FitWidth。
        /// </summary>
        public ScreenFitMode EffectiveFitMode { get; private set; }

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

        private const int DefaultMaxCachedViewsPerAsset = 8;

        private Dictionary<string, Stack<QUIBinding>> m_viewCache = new();
        public int MaxCachedViewsPerAsset { get; set; } = DefaultMaxCachedViewsPerAsset;

        public QUIBinding TakeBindingFromCache(string assetPath)
        {
            if (m_viewCache.TryGetValue(assetPath, out var stack) && stack.Count > 0)
            {
                var binding = stack.Pop();
                if (binding != null)
                {
                    binding.gameObject.SetActive(true);
                    return binding;
                }
            }

            return null;
        }

        public void RecycleBindingToCache(string assetPath, QUIBinding binding)
        {
            if (binding == null) return;

            if (!m_viewCache.ContainsKey(assetPath))
            {
                m_viewCache[assetPath] = new Stack<QUIBinding>();
            }

            if (m_viewCache[assetPath].Count >= MaxCachedViewsPerAsset)
            {
                AssetManager.ReleaseInstance(binding.gameObject);
                return;
            }

            binding.gameObject.SetActive(false);
            binding.transform.SetParent(null, false);
            m_viewCache[assetPath].Push(binding);
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
                    foreach (var binding in kvp.Value)
                    {
                        if (binding != null)
                        {
                            AssetManager.ReleaseInstance(binding.gameObject);
                        }
                    }
                    kvp.Value.Clear();
                }
                m_viewCache.Clear();
            }
            else if (m_viewCache.TryGetValue(assetPath, out var stack))
            {
                foreach (var binding in stack)
                {
                    if (binding != null)
                    {
                        AssetManager.ReleaseInstance(binding.gameObject);
                    }
                }
                stack.Clear();
                m_viewCache.Remove(assetPath);
            }
        }

        #endregion

        #region View 栈管理（可选）

        private Stack<IUIViewHandle> m_viewStack = new();

        public void PushView(IUIViewHandle viewHandle, UILayer layer)
        {
            if (viewHandle == null)
            {
                return;
            }

            m_viewStack.Push(viewHandle);
            viewHandle.Open(layer);
        }

        /// <summary>
        /// 返回（关闭栈顶 View）
        /// </summary>
        /// <returns>是否成功关闭</returns>
        public bool PopView()
        {
            if (m_viewStack.Count > 0)
            {
                var viewHandle = m_viewStack.Pop();
                if (viewHandle != null && viewHandle.IsAlive)
                {
                    viewHandle.Close();
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
                if (top?.View is T)
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
        /// 获取栈顶 View Handle
        /// </summary>
        public IUIViewHandle TopView => m_viewStack.Count > 0 ? m_viewStack.Peek() : null;

        /// <summary>
        /// 当前栈中 View 数量
        /// </summary>
        public int ViewStackCount => m_viewStack.Count;

        /// <summary>
        /// 从栈中移除指定 View（用于 View 直接关闭时同步栈状态）
        /// </summary>
        public void RemoveFromStack(BindingViewBase view)
        {
            if (view == null || m_viewStack.Count == 0) return;

            // 如果是栈顶，直接 Pop
            if (m_viewStack.Peek()?.View == view)
            {
                m_viewStack.Pop();
                return;
            }

            // 否则需要重建栈（较少发生）
            var tempList = new List<IUIViewHandle>(m_viewStack);
            tempList.RemoveAll(item => item == null || item.View == view);
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

        public void Init(Camera uiCamera, int designWidth, int designHeight, ScreenFitMode fitMode = ScreenFitMode.Auto, bool enableScreenFitDebugLog = false)
        {
            if (uiCamera == null)
            {
                throw new ArgumentNullException(nameof(uiCamera));
            }

            if (designWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(designWidth), designWidth, "Design width must be greater than zero.");
            }

            if (designHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(designHeight), designHeight, "Design height must be greater than zero.");
            }

            UICamera = uiCamera;
            DesignWidth = designWidth;
            DesignHeight = designHeight;
            FitMode = fitMode;
            EnableScreenFitDebugLog = enableScreenFitDebugLog;

            ValidateSortingLayers();
            m_sceneCameraBinder = new UISceneCameraBinder(UICamera, OnSceneCameraChanged);
            m_sceneCameraBinder.Initialize();

            GameObject rootObject = new("UIRoot");
            Root = rootObject.AddComponent<RectTransform>();
            Object.DontDestroyOnLoad(rootObject);

            rootObject.layer = ResolveLayerOrDefault(UiLayerName);
            RootCanvas = rootObject.AddComponent<Canvas>();
            EventSystem = rootObject.AddComponent<EventSystem>();
            AddInputModule(rootObject);

            RootCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            RootCanvas.worldCamera = UICamera;
            RootCanvas.vertexColorAlwaysGammaSpace = true;

            // 计算屏幕适配
            CalculateScreenFit();

            m_rootCanvasScaler = rootObject.AddComponent<CanvasScaler>();
            m_rootCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            m_rootCanvasScaler.scaleFactor = ScaleFactor;
            m_rootCanvasScaler.referencePixelsPerUnit = 100;
            CreateUILayers();
            rootObject.AddComponent<UIScreenFitWatcher>().Initialize(this);
        }

        private static void AddInputModule(GameObject rootObject)
        {
#if ENABLE_INPUT_SYSTEM
            rootObject.AddComponent<InputSystemUIInputModule>();
#else
            rootObject.AddComponent<StandaloneInputModule>();
#endif
        }

        /// <summary>
        /// 计算屏幕适配参数
        /// Auto 策略：
        /// - 宽屏（屏幕比设计宽）：按高度适配，UILayer 固定设计尺寸居中，两侧留空
        /// - 长屏（屏幕比设计窄长）：按宽度适配，UILayer 全屏铺满
        /// </summary>
        private void CalculateScreenFit()
        {
            if (EnableScreenFitDebugLog)
            {
                Debug.Log($"Screen: width={Screen.width}, height={Screen.height}, safeArea={Screen.safeArea}");
            }

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
            EffectiveFitMode = ResolveEffectiveFitMode(screenAspect, designAspect);

            if (EffectiveFitMode == ScreenFitMode.FitHeight)
            {
                // 按高度适配，保证设计高度完整显示，宽度可能留空或裁剪
                ScaleFactor = screenH / DesignHeight;

                float fitWidth = DesignWidth * ScaleFactor;
                ScreenFitRect = new Rect((screenW - fitWidth) * 0.5f, 0f, fitWidth, screenH);

                // 设计坐标系下的差值（用于扩展/留白计算）
                WidthDelta = (screenW / ScaleFactor) - DesignWidth;
                HeightDelta = 0f;
            }
            else
            {
                // 按宽度适配，保证设计宽度完整显示，高度可能扩展或裁剪
                ScaleFactor = screenW / DesignWidth;

                WidthDelta = 0f;
                HeightDelta = (screenH / ScaleFactor) - DesignHeight;

                if (HeightDelta >= 0f)
                {
                    ScreenFitRect = new Rect(0f, 0f, screenW, screenH);
                }
                else
                {
                    float fitHeight = DesignHeight * ScaleFactor;
                    ScreenFitRect = new Rect(0f, (screenH - fitHeight) * 0.5f, screenW, fitHeight);
                }
            }

            ScaleFitFactor = ScreenFitRect.width / Screen.width;

            if (EnableScreenFitDebugLog)
            {
                Debug.Log($"FitMode: {FitMode}, EffectiveFitMode: {EffectiveFitMode}, ScreenFitRect: {ScreenFitRect}, ScaleFactor: {ScaleFactor}");
                Debug.Log($"IsWideScreen: {IsWideScreen}, IsTallScreen: {IsTallScreen}, WidthDelta: {WidthDelta}, HeightDelta: {HeightDelta}");
            }
        }

        private ScreenFitMode ResolveEffectiveFitMode(float screenAspect, float designAspect)
        {
            if (FitMode != ScreenFitMode.Auto)
            {
                return FitMode;
            }

            return screenAspect >= designAspect ? ScreenFitMode.FitHeight : ScreenFitMode.FitWidth;
        }

        private static void ValidateSortingLayers()
        {
            var configuredLayers = SortingLayer.layers;
            if (configuredLayers == null || configuredLayers.Length == 0)
            {
                Debug.LogWarning("QUI: No Unity Sorting Layers are configured. UI canvases will not use the expected EFrame UI layer order.");
                return;
            }

            var configuredLayerNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sortingLayer in configuredLayers)
            {
                if (!string.IsNullOrEmpty(sortingLayer.name))
                {
                    configuredLayerNames.Add(sortingLayer.name);
                }
            }

            var missingLayers = new List<string>();
            foreach (var layerName in Enum.GetNames(typeof(UILayer)))
            {
                if (!configuredLayerNames.Contains(layerName))
                {
                    missingLayers.Add(layerName);
                }
            }

            if (missingLayers.Count > 0)
            {
                Debug.LogWarning($"QUI: Missing UI Sorting Layers: {string.Join(", ", missingLayers)}. Run 'EFrame Tools/自动创建 UI SortingLayer层级' or the project initialization flow to fix them.");
            }
        }

        private static int ResolveLayerOrDefault(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
            {
                return layer;
            }

            Debug.LogWarning($"QUI: Unity Layer '{layerName}' is missing. Falling back to '{DefaultLayerName}'. Configure the project layer if you need a dedicated UI layer.");
            return LayerMask.NameToLayer(DefaultLayerName);
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
                layerObject.layer = ResolveLayerOrDefault(UiLayerName);
                RectTransform layerNode = layerObject.AddComponent<RectTransform>();
                Canvas layerCanvas = layerObject.AddComponent<Canvas>();
                layerObject.AddComponent<GraphicRaycaster>();
                layerNode.SetParent(RootCanvas.transform, false);

                layerCanvas.overrideSorting = true;
                layerCanvas.sortingLayerName = layer;

                ApplyUILayerLayout(layerNode);

                m_uiLayerNodeTable.Add(layerType, layerNode);
            }
        }

        private void ApplyUILayerLayout(RectTransform layerNode)
        {
            layerNode.anchorMin = new Vector2(0.5f, 0.5f);
            layerNode.anchorMax = new Vector2(0.5f, 0.5f);
            layerNode.pivot = new Vector2(0.5f, 0.5f);
            layerNode.anchoredPosition = Vector2.zero;

            var expandWidth = DesignWidth >= DesignHeight && EffectiveFitMode == ScreenFitMode.FitHeight;
            var expandHeight = DesignHeight >= DesignWidth && EffectiveFitMode == ScreenFitMode.FitWidth;
            var width = DesignWidth + (expandWidth ? Mathf.Max(0f, WidthDelta) : 0f);
            var height = DesignHeight + (expandHeight ? Mathf.Max(0f, HeightDelta) : 0f);
            layerNode.sizeDelta = new Vector2(width, height);
        }

        private void CreateWorldBackgroundUINode()
        {
            var bgCamera = ResolveBackgroundCamera();

            // QuiBackground 是挂在场景相机下的 WorldSpace Canvas，用于实现“背景在场景下、常规 UI 在场景上”的两相机 UI 分层。
            string canvasName = "BackgroundCanvas";
            m_backgroundCanvasObject = new GameObject(canvasName);
            m_backgroundCanvasObject.layer = LayerMask.NameToLayer(DefaultLayerName);

            m_backgroundCanvasRect = m_backgroundCanvasObject.AddComponent<RectTransform>();
            m_backgroundCanvas = m_backgroundCanvasObject.AddComponent<Canvas>();
            m_backgroundCanvas.renderMode = RenderMode.WorldSpace;
            m_backgroundCanvas.sortingOrder = -100;
            m_backgroundCanvasObject.AddComponent<GraphicRaycaster>();
            m_backgroundCanvas.vertexColorAlwaysGammaSpace = true;

            string layerName = UI.UILayer.QuiBackground.ToString();
            GameObject layerObject = new(layerName);
            layerObject.layer = LayerMask.NameToLayer(DefaultLayerName);

            m_backgroundNode = layerObject.AddComponent<RectTransform>();
            m_backgroundNode.SetParent(m_backgroundCanvasRect, false);
            ApplyWorldBackgroundLayout(bgCamera);

            m_uiLayerNodeTable.Add(UI.UILayer.QuiBackground, m_backgroundNode);
        }

        private Camera ResolveBackgroundCamera()
        {
            return EFrame.Current?.SceneCamera != null ? EFrame.Current.SceneCamera : UICamera;
        }

        private void OnSceneCameraChanged(Camera sceneCamera)
        {
            if (m_backgroundCanvasObject == null)
            {
                return;
            }

            ApplyWorldBackgroundLayout(sceneCamera != null ? sceneCamera : UICamera);
        }

        private void ApplyWorldBackgroundLayout(Camera bgCamera)
        {
            if (bgCamera == null || m_backgroundCanvasRect == null || m_backgroundCanvas == null || m_backgroundNode == null)
            {
                return;
            }

            const float cameraDistance = 20f;
            m_backgroundCamera = bgCamera;
            m_backgroundCanvas.worldCamera = m_backgroundCamera;

            var canvasSize = CalculateCameraViewSize(m_backgroundCamera, cameraDistance);

            m_backgroundCanvasRect.SetParent(m_backgroundCamera.transform, false);
            m_backgroundCanvasRect.anchorMin = new Vector2(0.5f, 0.5f);
            m_backgroundCanvasRect.anchorMax = new Vector2(0.5f, 0.5f);
            m_backgroundCanvasRect.pivot = new Vector2(0.5f, 0.5f);
            m_backgroundCanvasRect.sizeDelta = canvasSize;
            m_backgroundCanvasRect.anchoredPosition = Vector2.zero;
            m_backgroundCanvasRect.localPosition = Vector3.forward * cameraDistance;
            m_backgroundCanvasRect.localRotation = Quaternion.identity;
            m_backgroundCanvasRect.localScale = Vector3.one;

            var canvasScaleFactor = EffectiveFitMode == ScreenFitMode.FitHeight
                ? canvasSize.y / DesignHeight
                : canvasSize.x / DesignWidth;

            ApplyUILayerLayout(m_backgroundNode);
            m_backgroundNode.localScale = new Vector3(canvasScaleFactor, canvasScaleFactor, 1);
        }

        private static Vector2 CalculateCameraViewSize(Camera camera, float distance)
        {
            if (camera.orthographic)
            {
                var height = camera.orthographicSize * 2f;
                return new Vector2(height * camera.aspect, height);
            }

            var heightAtDistance = 2f * distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            return new Vector2(heightAtDistance * camera.aspect, heightAtDistance);
        }

        public void RefreshScreenFit()
        {
            CalculateScreenFit();

            if (m_rootCanvasScaler != null)
            {
                m_rootCanvasScaler.scaleFactor = ScaleFactor;
            }

            if (m_uiLayerNodeTable != null)
            {
                foreach (var pair in m_uiLayerNodeTable)
                {
                    if (pair.Key == UI.UILayer.QuiBackground)
                    {
                        continue;
                    }

                    if (pair.Value != null)
                    {
                        ApplyUILayerLayout(pair.Value);
                    }
                }
            }

            ApplyWorldBackgroundLayout(m_backgroundCamera != null ? m_backgroundCamera : ResolveBackgroundCamera());
            RefreshLayoutFitters();
        }

        private void RefreshLayoutFitters()
        {
            if (Root != null)
            {
                foreach (var fitter in Root.GetComponentsInChildren<SafeAreaFitter>(true))
                {
                    fitter.ApplySafeArea();
                }

                foreach (var fitter in Root.GetComponentsInChildren<FullScreenFitter>(true))
                {
                    fitter.ApplyFullScreen();
                }
            }

            if (m_backgroundCanvasObject != null)
            {
                foreach (var fitter in m_backgroundCanvasObject.GetComponentsInChildren<SafeAreaFitter>(true))
                {
                    fitter.ApplySafeArea();
                }

                foreach (var fitter in m_backgroundCanvasObject.GetComponentsInChildren<FullScreenFitter>(true))
                {
                    fitter.ApplyFullScreen();
                }
            }
        }

        public void SetInteractive(bool isInteractive)
        {
            EventSystem.enabled = isInteractive;
        }

        public void Dispose()
        {
            ClearViewCache();
            PopAll();
            if (Root != null)
            {
                Object.Destroy(Root.gameObject);
            }

            if (m_backgroundCanvasObject != null)
            {
                Object.Destroy(m_backgroundCanvasObject);
            }

            m_uiLayerNodeTable?.Clear();
            m_transitionCache.Clear();
            m_sceneCameraBinder?.Dispose();
            m_sceneCameraBinder = null;
            m_backgroundCanvasObject = null;
            m_backgroundCanvasRect = null;
            m_backgroundNode = null;
            m_backgroundCanvas = null;
            m_backgroundCamera = null;
            m_rootCanvasScaler = null;
            Root = null;
            RootCanvas = null;
            UICamera = null;
            EventSystem = null;
        }

        #region UIBinding

        public QUIBinding CreateBinding(string assetPath, UILayer? layer = null)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new ArgumentNullException(nameof(assetPath));
            }

            var cached = TakeBindingFromCache(assetPath);
            if (cached != null)
            {
                if (layer.HasValue)
                {
                    cached.transform.SetParent(UILayer(layer.Value), false);
                }

                return cached;
            }

            var context = EFrame.Current;
            if (context == null)
            {
                throw new InvalidOperationException($"QUI.CreateBinding requires EFrame.Initialize() to complete before creating UI views. AssetPath: {assetPath}");
            }

            var parent = layer.HasValue ? UILayer(layer.Value) : null;
            var go = context.Assets.Instantiate(assetPath, parent);
            if (go == null)
            {
                throw new ArgumentException($"The UI asset path '{assetPath}' could not be instantiated.");
            }

            context.InjectInto(go);
            go.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            if (go.TryGetComponent<QUIBinding>(out var uiBinding))
            {
                return uiBinding;
            }

            AssetManager.ReleaseInstance(go);
            throw new ArgumentException($"The instantiated GameObject from path '{assetPath}' does not contain a QUIBinding component.");
        }

        public TView CreateView<TView>(string assetPath, UILayer? layer = null) where TView : BindingViewBase
        {
            var binding = CreateBinding(assetPath, layer);
            try
            {
                if (Activator.CreateInstance(typeof(TView)) is not TView view)
                {
                    throw new InvalidOperationException($"Failed to create UI view instance for type {typeof(TView).FullName}.");
                }

                view.BindContext(EFrame.Current);
                view.SetBinding(binding, assetPath);
                return view;
            }
            catch
            {
                ReleaseBinding(assetPath, binding, false);
                throw;
            }
        }

        public UIViewHandle<TView> CreateViewHandle<TView>(string assetPath, UILayer? layer = null) where TView : BindingViewBase
        {
            var view = CreateView<TView>(assetPath, layer);
            return new UIViewHandle<TView>(assetPath, view);
        }

        public void ReleaseBinding(string assetPath, QUIBinding binding, bool useCache)
        {
            if (binding == null)
            {
                return;
            }

            if (useCache && !string.IsNullOrEmpty(assetPath))
            {
                RecycleBindingToCache(assetPath, binding);
                return;
            }

            AssetManager.ReleaseInstance(binding.gameObject);
        }

        public UniTask PlayOpenTransitionAsync(BindingViewBase view)
        {
            return ResolveTransition(view, true).PlayOpenAsync(view);
        }

        public UniTask PlayCloseTransitionAsync(BindingViewBase view)
        {
            return ResolveTransition(view, false).PlayCloseAsync(view);
        }

        public void KillTransition(BindingViewBase view)
        {
            var openTransition = ResolveTransition(view, true);
            var closeTransition = ResolveTransition(view, false);

            openTransition.Kill(view);
            if (!ReferenceEquals(openTransition, closeTransition))
            {
                closeTransition.Kill(view);
            }
        }

        private IUIViewTransition ResolveTransition(BindingViewBase view, bool opening)
        {
            var transitionTypeName = view?.Config.GetTransitionTypeName(opening);
            if (string.IsNullOrWhiteSpace(transitionTypeName))
            {
                return m_defaultTransition;
            }

            if (m_transitionCache.TryGetValue(transitionTypeName, out var cachedTransition))
            {
                return cachedTransition;
            }

            var transition = CreateTransition(transitionTypeName);
            m_transitionCache[transitionTypeName] = transition;
            return transition;
        }

        private IUIViewTransition CreateTransition(string transitionTypeName)
        {
            if (transitionTypeName == ScaleFadeViewTransition.Id)
            {
                return m_defaultTransition;
            }

            if (transitionTypeName == NoneViewTransition.Id)
            {
                return new NoneViewTransition();
            }

            var transitionType = Type.GetType(transitionTypeName);
            if (transitionType == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    transitionType = assembly.GetType(transitionTypeName);
                    if (transitionType != null)
                    {
                        break;
                    }
                }
            }

            if (transitionType == null || transitionType.IsAbstract || typeof(MonoBehaviour).IsAssignableFrom(transitionType) || !typeof(IUIViewTransition).IsAssignableFrom(transitionType))
            {
                Debug.LogWarning($"QUI: View transition '{transitionTypeName}' is unavailable. Falling back to default transition.");
                return m_defaultTransition;
            }

            if (transitionType.GetConstructor(Type.EmptyTypes) == null)
            {
                Debug.LogWarning($"QUI: View transition '{transitionTypeName}' must have a public parameterless constructor. Falling back to default transition.");
                return m_defaultTransition;
            }

            try
            {
                return Activator.CreateInstance(transitionType) as IUIViewTransition ?? m_defaultTransition;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"QUI: Failed to create view transition '{transitionTypeName}'. Falling back to default transition. {ex}");
                return m_defaultTransition;
            }
        }

        public void OpenBindingView(BindingViewBase bindingViewBase, UILayer uiLayerType = UI.UILayer.QuiPanel)
        {
            bindingViewBase.transform.SetParent(m_uiLayerNodeTable[uiLayerType], false);
        }

        public void RegisterSceneCamera(EFrameSceneCamera sceneCamera)
        {
            m_sceneCameraBinder?.Register(sceneCamera);
        }

        public void UnregisterSceneCamera(EFrameSceneCamera sceneCamera)
        {
            m_sceneCameraBinder?.Unregister(sceneCamera);
        }

        public void RefreshSceneCameraBindings()
        {
            m_sceneCameraBinder?.Refresh();
            ApplyWorldBackgroundLayout(ResolveBackgroundCamera());
        }

        #endregion
    }

    public sealed class UIScreenFitWatcher : MonoBehaviour
    {
        private QUI m_ui;
        private int m_lastWidth;
        private int m_lastHeight;
        private Rect m_lastSafeArea;

        public void Initialize(QUI ui)
        {
            m_ui = ui;
            CacheState();
        }

        private void Update()
        {
            if (m_ui == null)
            {
                return;
            }

            if (Screen.width == m_lastWidth && Screen.height == m_lastHeight && Screen.safeArea == m_lastSafeArea)
            {
                return;
            }

            CacheState();
            m_ui.RefreshScreenFit();
        }

        private void CacheState()
        {
            m_lastWidth = Screen.width;
            m_lastHeight = Screen.height;
            m_lastSafeArea = Screen.safeArea;
        }
    }
}
