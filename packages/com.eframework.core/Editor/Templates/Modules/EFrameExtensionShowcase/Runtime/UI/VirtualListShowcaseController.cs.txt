using EFramework.Generated.UI;
using EFramework.Runtime.UI;

namespace GameApp.Modules.EFrameExtensionShowcase.UI
{
    public sealed class VirtualListShowcaseController : UIControllerBase<v_EFrameExtensionShowcaseQVirtualListShowcaseWindow>
    {
        protected override string AssetPath => VirtualListShowcaseWindow.AssetPath;

        protected override void OnViewCreated()
        {
            base.OnViewCreated();
            if (CurrentView?.Window != null)
            {
                CurrentView.Window.Build(CurrentView);
                CurrentView.Window.CloseRequested = Hide;
            }
        }

        protected override void OnViewDestroyed()
        {
            if (CurrentView?.Window != null)
            {
                CurrentView.Window.CloseRequested = null;
            }

            base.OnViewDestroyed();
        }
    }
}
