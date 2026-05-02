using Cysharp.Threading.Tasks;
using EFramework.Generated;
using EFramework.Runtime;
using EFramework.Runtime.Asset;
using EFramework.Runtime.Procedure;
using GameApp.Modules.SampleModule.Procedure;
using GameApp.UI.Controllers;
using UnityEngine;

namespace GameApp.Procedure
{
    public sealed class ProcedureHome : EFrameProcedure
    {
        private HomeViewController m_homeViewController;

        protected override async UniTask OnPreloadAsync(IAssetPreloadScope assets, ProcedureEnterContext context)
        {
            await assets.PreloadAsync<GameObject>(ResPath.Generated.UI.Panels.Home.HomeView);
        }

        protected override void OnEnter(ProcedureEnterContext context)
        {
            base.OnEnter(context);

            if (Context?.UI == null)
            {
                Debug.LogError("[ProcedureHome] EFrame UI is not initialized.");
                return;
            }

            m_homeViewController = new HomeViewController
            {
                ModuleTestRequested = () => ChangeState<ProcedureSampleModuleEntry>()
            };

            m_homeViewController.Show();
            Debug.Log("[ProcedureHome] Entered. HomeView is shown.");
        }

        protected override void OnLeave(bool isShutdown)
        {
            if (m_homeViewController != null)
            {
                m_homeViewController.ModuleTestRequested = null;
                m_homeViewController.Dispose();
                m_homeViewController = null;
            }

            base.OnLeave(isShutdown);
        }
    }
}
