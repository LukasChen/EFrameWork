using Cysharp.Threading.Tasks;
using EFramework.Generated;
using EFramework.Runtime.Asset;
using EFramework.Runtime.Procedure;
using GameApp.Modules.EFrameExtensionShowcase.UI;
using GameApp.Procedure;
using UnityEngine;

namespace GameApp.Modules.EFrameExtensionShowcase.Procedure
{
    public sealed class ProcedureEFrameExtensionShowcaseEntry : EFrameProcedure
    {
        private EFrameExtensionShowcaseController m_controller;

        protected override async UniTask OnPreloadAsync(IAssetPreloadScope assets, ProcedureEnterContext context)
        {
            await assets.PreloadAsync<GameObject>(ResPath.Generated.Modules.EFrameExtensionShowcase.Res.UI.Panels.EFrameExtensionShowcase.EFrameExtensionShowcaseView);
        }

        protected override void OnEnter(ProcedureEnterContext context)
        {
            base.OnEnter(context);

            m_controller = new EFrameExtensionShowcaseController
            {
                BackRequested = () => ChangeState<ProcedureHome>()
            };
            m_controller.Show();

            Debug.Log("[ProcedureEFrameExtensionShowcaseEntry] Entered.");
        }

        protected override void OnLeave(bool isShutdown)
        {
            if (m_controller != null)
            {
                m_controller.Dispose();
                m_controller = null;
            }

            base.OnLeave(isShutdown);
        }
    }
}
