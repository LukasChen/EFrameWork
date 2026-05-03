using EFramework.Generated;
using EFramework.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameApp.Modules.EFrameExtensionShowcase.UI
{
    public sealed class EFrameExtensionShowcaseView : BindingViewBase
    {
        public static string DefaultAssetPath => ResPath.Generated.Modules.EFrameExtensionShowcase.Res.UI.Panels.EFrameExtensionShowcase.EFrameExtensionShowcaseView;

        public Text DetailText { get; private set; }
        public Button[] DemoButtons { get; private set; } = new Button[0];
        public Text[] DemoLabels { get; private set; } = new Text[0];
        public Button BackButton { get; private set; }

        protected override void OnBindingSet()
        {
            base.OnBindingSet();

            DetailText = FindComponent<Text>("Panel/DetailText");
            BackButton = FindComponent<Button>("Panel/BackButton");

            var buttonRoot = transform == null ? null : transform.Find("Panel/DemoButtons");
            if (buttonRoot == null)
            {
                DemoButtons = new Button[0];
                DemoLabels = new Text[0];
                return;
            }

            DemoButtons = buttonRoot.GetComponentsInChildren<Button>(true);
            DemoLabels = new Text[DemoButtons.Length];
            for (var i = 0; i < DemoButtons.Length; i++)
            {
                DemoLabels[i] = DemoButtons[i].transform.Find("LabelText")?.GetComponent<Text>();
            }
        }

        private T FindComponent<T>(string path) where T : Component
        {
            return transform == null ? null : transform.Find(path)?.GetComponent<T>();
        }
    }
}
