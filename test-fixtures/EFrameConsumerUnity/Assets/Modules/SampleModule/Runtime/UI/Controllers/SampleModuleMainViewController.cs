using System;
using EFramework.Runtime.UI;
using EFrameConsumerFixture.Common;
using EFrameConsumerFixture.Modules.SampleModule.UI.Views;

namespace EFrameConsumerFixture.Modules.SampleModule.UI.Controllers
{
    public sealed class SampleModuleMainViewController : UIControllerBase<SampleModuleMainView>
    {
        public Action BackRequested { get; set; }

        protected override string AssetPath => ResPath.Generated.Modules.SampleModule.Res.UI.Panels.SampleModuleMain.SampleModuleMainView;

        protected override void OnViewCreated()
        {
            base.OnViewCreated();
            AddButtonClickListener(TypedViewHandle.TypedView.BackButton, OnBackButtonClick);
        }

        protected override void OnViewDestroyed()
        {
            BackRequested = null;
            base.OnViewDestroyed();
        }

        private void OnBackButtonClick()
        {
            BackRequested?.Invoke();
        }
    }
}
