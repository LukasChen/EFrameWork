using EFramework.Generated;
using EFramework.Runtime.UI;
using GameApp.UI.Views;

namespace GameApp.UI.Controllers
{
    public sealed class HomeViewController : UIControllerBase<HomeView>
    {
        protected override string AssetPath => ResPath.UI.Panels.Home.HomeView;
    }
}
