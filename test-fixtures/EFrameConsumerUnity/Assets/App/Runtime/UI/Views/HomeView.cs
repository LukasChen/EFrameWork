using EFramework.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EFrameConsumerFixture.UI.Views
{
    public sealed class HomeView : BindingViewBase
    {
        public Button ModuleTestButton { get; private set; }

        public HomeView()
        {
        }

        protected override void OnBindingSet()
        {
            base.OnBindingSet();
            CacheComponents();
        }

        private void CacheComponents()
        {
            ModuleTestButton = Binding == null ? null : Binding.transform.Find("Panel/ModuleTestButton")?.GetComponent<Button>();
        }

        public static string DefaultAssetPath => EFrameConsumerFixture.Common.ResPath.Generated.UI.Panels.Home.HomeView;
    }
}
