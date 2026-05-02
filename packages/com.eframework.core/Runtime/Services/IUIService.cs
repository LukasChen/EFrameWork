using System;
using Cysharp.Threading.Tasks;
using EFramework.Runtime;
using EFramework.Runtime.UI;
using EFramework.Runtime.UI.Handles;
using EFramework.Runtime.UI.Transitions;
using UnityEngine;

namespace EFramework.Runtime.UI
{
    public interface IUIService : IDisposable
    {
        Camera UICamera { get; }
        RectTransform Root { get; }
        Rect ScreenFitRect { get; }
        Rect SafeAreaRect { get; }
        float ScaleFactor { get; }
        int DesignWidth { get; }
        int DesignHeight { get; }
        RectTransform UILayer(UILayer layer);
        void Init(Camera uiCamera, int designWidth, int designHeight, ScreenFitMode fitMode, bool enableScreenFitDebugLog = false);
        void RefreshScreenFit();
        QUIBinding CreateBinding(string assetPath, UILayer? layer = null);
        TView CreateView<TView>(string assetPath, UILayer? layer = null) where TView : BindingViewBase;
        UIViewHandle<TView> CreateViewHandle<TView>(string assetPath, UILayer? layer = null) where TView : BindingViewBase;
        void ReleaseBinding(string assetPath, QUIBinding binding, bool useCache);
        IUIViewTransition DefaultTransition { get; }
        UniTask PlayOpenTransitionAsync(BindingViewBase view);
        UniTask PlayCloseTransitionAsync(BindingViewBase view);
        void KillTransition(BindingViewBase view);
        void OpenBindingView(BindingViewBase bindingViewBase, UILayer layer);
        QUIBinding TakeBindingFromCache(string assetPath);
        void RecycleBindingToCache(string assetPath, QUIBinding binding);
        void ClearViewCache(string assetPath = null);
        void PushView(IUIViewHandle viewHandle, UILayer layer);
        bool PopView();
        void PopTo<T>() where T : BindingViewBase;
        void PopAll();
        IUIViewHandle TopView { get; }
        int ViewStackCount { get; }
        void RemoveFromStack(BindingViewBase view);
        void RegisterSceneCamera(EFrameSceneCamera sceneCamera);
        void UnregisterSceneCamera(EFrameSceneCamera sceneCamera);
        void RefreshSceneCameraBindings();
        void SetInteractive(bool isInteractive);
    }
}
