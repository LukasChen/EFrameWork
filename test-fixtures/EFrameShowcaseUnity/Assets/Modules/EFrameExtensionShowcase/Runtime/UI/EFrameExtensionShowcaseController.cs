using System;
using GameApp.Modules.EFrameExtensionShowcase.Demos;
using EFramework.Generated;
using EFramework.Runtime.UI;
using UnityEngine.UI;

namespace GameApp.Modules.EFrameExtensionShowcase.UI
{
    public sealed class EFrameExtensionShowcaseController : UIControllerBase<EFrameExtensionShowcaseView>
    {
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

            AddButtonClickListener(CurrentView?.BackButton, OnBackClicked, true);
        }

        protected override void OnViewOpened()
        {
            base.OnViewOpened();
            SetDetail("Select an extension entry.");
        }

        protected override void OnViewDestroyed()
        {
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
            demo.Run?.Invoke();
            var state = EFrameExtensionShowcaseRegistry.IsInstalled(demo) ? "installed" : "not installed";
            SetDetail($"{demo.Title}\nPackage: {demo.PackageName}\nStatus: {state}\n{demo.Description}");
        }

        private void SetDetail(string message)
        {
            if (CurrentView?.DetailText != null)
            {
                CurrentView.DetailText.text = message;
            }
        }

        private void OnBackClicked()
        {
            BackRequested?.Invoke();
        }
    }
}
