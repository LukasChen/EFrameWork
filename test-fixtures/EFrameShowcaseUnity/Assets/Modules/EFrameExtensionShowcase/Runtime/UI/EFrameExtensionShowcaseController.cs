using System;
using GameApp.Modules.EFrameExtensionShowcase.Demos;
using EFramework.Generated;
using EFramework.Generated.UI;
using EFramework.Extensions.DebugConsole;
using EFramework.Runtime.UI;
using UnityEngine.UI;

namespace GameApp.Modules.EFrameExtensionShowcase.UI
{
    public sealed class EFrameExtensionShowcaseController : UIControllerBase<v_EFrameExtensionShowcaseView>
    {
        private const string VirtualListPackageName = "com.eframework.ui.virtual-list";
        private const string DebugConsolePackageName = "com.eframework.debug-console";

        private VirtualListShowcaseController m_virtualListController;
        public Action BackRequested { get; set; }

        protected override string AssetPath => ResPath.Generated.Modules.EFrameExtensionShowcase.Res.UI.Panels.EFrameExtensionShowcase.EFrameExtensionShowcaseView;

        protected override void OnViewCreated()
        {
            base.OnViewCreated();

            var demos = EFrameExtensionShowcaseRegistry.Demos;
            var buttons = new[]
            {
                CurrentView?.VirtualListButton,
                CurrentView?.DebugConsoleButton
            };
            var labels = new[]
            {
                CurrentView?.VirtualListLabelText,
                CurrentView?.DebugConsoleLabelText
            };
            var count = Math.Min(demos.Count, buttons.Length);
            for (var i = 0; i < count; i++)
            {
                BindDemoButton(buttons[i], i < labels.Length ? labels[i] : null, demos[i]);
            }

            for (var i = count; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    buttons[i].gameObject.SetActive(false);
                }
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
            m_virtualListController = new VirtualListShowcaseController();
            m_virtualListController.BindContext(Context);
            m_virtualListController.Show(UILayer.QuiPopUp);
            SetDetail($"{demo.Title}\nPackage: {demo.PackageName}\nStatus: installed\nOpened popup window.");
        }

        private void HideVirtualListSample()
        {
            if (m_virtualListController != null)
            {
                m_virtualListController.Dispose();
                m_virtualListController = null;
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
