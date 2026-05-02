using System;
using EFramework.Generated;
using EFramework.Runtime.UI;
using GameApp.Modules.SampleModule.UI.Views;

namespace GameApp.Modules.SampleModule.UI.Controllers
{
    public sealed class SampleModuleMainViewController : UIControllerBase<SampleModuleMainView>
    {
        public Action BackRequested { get; set; }

        protected override string AssetPath => ResPath.Generated.Modules.SampleModule.Res.UI.Panels.SampleModuleMain.SampleModuleMainView;

        protected override void OnViewCreated()
        {
            base.OnViewCreated();
            AddButtonClickListener(CurrentView.BackButton, OnBackButtonClick);
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
