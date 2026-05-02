using Cysharp.Threading.Tasks;
using EFramework.Generated;
using GameApp.Procedure;
using EFramework.Runtime.Asset;
using EFramework.Runtime.Procedure;
using GameApp.Modules.SampleModule.UI.Controllers;
using UnityEngine;

namespace GameApp.Modules.SampleModule.Procedure
{
    public sealed class ProcedureSampleModuleEntry : EFrameProcedure
    {
        private SampleModuleMainViewController m_viewController;

        protected override async UniTask OnPreloadAsync(IAssetPreloadScope assets, ProcedureEnterContext context)
        {
            await assets.PreloadAsync<GameObject>(ResPath.Generated.Modules.SampleModule.Res.UI.Panels.SampleModuleMain.SampleModuleMainView);
        }

        protected override void OnEnter(ProcedureEnterContext context)
        {
            base.OnEnter(context);

            m_viewController = new SampleModuleMainViewController
            {
                BackRequested = () => ChangeState<ProcedureHome>()
            };

            m_viewController.Show();
            Debug.Log("[ProcedureSampleModuleEntry] Entered. SampleModuleMainView is shown.");
        }

        protected override void OnLeave(bool isShutdown)
        {
            if (m_viewController != null)
            {
                m_viewController.BackRequested = null;
                m_viewController.Dispose();
                m_viewController = null;
            }

            base.OnLeave(isShutdown);
        }
    }
}
