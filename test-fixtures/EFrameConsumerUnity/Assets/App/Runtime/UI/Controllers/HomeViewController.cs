using System;
using EFrameConsumerFixture.Common;
using EFrameConsumerFixture.UI.Views;
using EFramework.Runtime.UI;

namespace EFrameConsumerFixture.UI.Controllers
{
    public sealed class HomeViewController : UIControllerBase<HomeView>
    {
        public Action ModuleTestRequested { get; set; }

        protected override string AssetPath => ResPath.Generated.UI.Panels.Home.HomeView;

        protected override void OnViewCreated()
        {
            base.OnViewCreated();
            AddButtonClickListener(TypedViewHandle.TypedView.ModuleTestButton, OnModuleTestButtonClick);
        }

        protected override void OnViewDestroyed()
        {
            ModuleTestRequested = null;
            base.OnViewDestroyed();
        }

        private void OnModuleTestButtonClick()
        {
            ModuleTestRequested?.Invoke();
        }
    }
}
