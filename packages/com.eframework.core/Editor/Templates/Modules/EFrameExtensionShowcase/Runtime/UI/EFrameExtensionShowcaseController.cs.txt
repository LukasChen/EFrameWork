using System;
using GameApp.Modules.EFrameExtensionShowcase.Demos;
using EFramework.Generated;
using EFramework.Extensions.DebugConsole;
using EFramework.Runtime.UI;
using UnityEngine.UI;

namespace GameApp.Modules.EFrameExtensionShowcase.UI
{
    public sealed class EFrameExtensionShowcaseController : UIControllerBase<EFrameExtensionShowcaseView>
    {
        private const string VirtualListPackageName = "com.eframework.ui.virtual-list";
        private const string DebugConsolePackageName = "com.eframework.debug-console";

        private UnityEngine.GameObject m_virtualListWindow;
        public Action BackRequested { get; set; }

        protected override string AssetPath => ResPath.Generated.Modules.EFrameExtensionShowcase.Res.UI.Panels.EFrameExtensionShowcase.EFrameExtensionShowcaseView;

        protected override void OnViewCreated()
        {
            base.OnViewCreated();

            var demos = EFrameExtensionShowcaseRegistry.Demos;
            var buttons = CurrentView?.DemoButtons ?? Array.Empty<UnityEngine.UI.Button>();
            var labels = CurrentView?.DemoLabels ?? Array.Empty<Text>();
            var count = Math.Min(demos.Count, buttons.Length);
            for (var i = 0; i < count; i++)
            {
                BindDemoButton(buttons[i], i < labels.Length ? labels[i] : null, demos[i]);
            }

            for (var i = count; i < buttons.Length; i++)
            {
                buttons[i].gameObject.SetActive(false);
            }

            AddButtonClickListener(CurrentView?.BackButton, OnBackClicked, true);
        }

        protected override void OnViewOpened()
        {
            base.OnViewOpened();
            HideVirtualListSample();
            SetDetail("Select an extension entry.");
        }

        protected override void OnViewDestroyed()
        {
            HideVirtualListSample();
            BackRequested = null;
            base.OnViewDestroyed();
        }

        private void BindDemoButton(UnityEngine.UI.Button button, Text label, EFrameExtensionShowcaseDemo demo)
        {
            if (label != null)
            {
                var installed = EFrameExtensionShowcaseRegistry.IsInstalled(demo);
                label.text = installed ? $"{demo.Title}  [Installed]" : $"{demo.Title}  [Package Missing]";
            }

            AddButtonClickListener(button, () => OnDemoSelected(demo), true);
        }

        private void OnDemoSelected(EFrameExtensionShowcaseDemo demo)
        {
            var state = EFrameExtensionShowcaseRegistry.IsInstalled(demo) ? "installed" : "not installed";
            if (!EFrameExtensionShowcaseRegistry.IsInstalled(demo))
            {
                HideVirtualListSample();
                SetDetail($"{demo.Title}\nPackage: {demo.PackageName}\nStatus: {state}\n{demo.Description}");
                return;
            }

            if (demo.PackageName == VirtualListPackageName)
            {
                ShowVirtualListSample(demo);
                return;
            }

            if (demo.PackageName == DebugConsolePackageName)
            {
                HideVirtualListSample();
                ShowDebugConsole(demo);
                return;
            }

            HideVirtualListSample();
            SetDetail($"{demo.Title}\nPackage: {demo.PackageName}\nStatus: {state}\n{demo.Description}");
        }

        private void SetDetail(string message)
        {
            if (CurrentView?.DetailText != null)
            {
                CurrentView.DetailText.enabled = true;
                CurrentView.DetailText.text = message;
            }
        }

        private void ShowVirtualListSample(EFrameExtensionShowcaseDemo demo)
        {
            if (CurrentView == null)
            {
                SetDetail("UI Virtual List sample entry was not found.");
                return;
            }

            HideVirtualListSample();
            m_virtualListWindow = VirtualListShowcaseWindow.Show(Context, CurrentView.transform);
            SetDetail($"{demo.Title}\nPackage: {demo.PackageName}\nStatus: installed\nOpened prefab window.");
        }

        private void HideVirtualListSample()
        {
            if (m_virtualListWindow != null)
            {
                VirtualListShowcaseWindow.Close(Context, m_virtualListWindow);
                m_virtualListWindow = null;
            }
        }

        private void ShowDebugConsole(EFrameExtensionShowcaseDemo demo)
        {
            EFrameDebugConsole.Show();
            SetDetail($"{demo.Title}\nPackage: {demo.PackageName}\nStatus: installed\nDebug console panel is visible.");
        }

        private void OnBackClicked()
        {
            HideVirtualListSample();
            BackRequested?.Invoke();
        }
    }
}
