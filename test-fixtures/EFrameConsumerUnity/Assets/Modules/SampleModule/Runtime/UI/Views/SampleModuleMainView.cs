using EFramework.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EFrameConsumerFixture.Modules.SampleModule.UI.Views
{
    public sealed class SampleModuleMainView : BindingViewBase
    {
        public Button BackButton { get; private set; }

        public SampleModuleMainView()
        {
        }

        protected override void OnBindingSet()
        {
            base.OnBindingSet();
            CacheComponents();
        }

        private void CacheComponents()
        {
            BackButton = Binding == null ? null : Binding.transform.Find("Panel/BackButton")?.GetComponent<Button>();
        }
    }
}
