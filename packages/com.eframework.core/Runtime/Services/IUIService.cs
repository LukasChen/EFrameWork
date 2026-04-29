using System;
using EFrameWork.Runtime.UI;
using UnityEngine;

namespace EFrameWork.Runtime.UI
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
        void Init(Camera uiCamera, int designWidth, int designHeight, ScreenFitMode fitMode);
        void OpenBindingView(BindingViewBase bindingViewBase, UILayer layer);
        T GetViewFromCache<T>(string assetPath) where T : BindingViewBase;
        void RecycleViewToCache(string assetPath, BindingViewBase view);
        void RemoveFromStack(BindingViewBase view);
        void SetInteractive(bool isInteractive);
    }
}
