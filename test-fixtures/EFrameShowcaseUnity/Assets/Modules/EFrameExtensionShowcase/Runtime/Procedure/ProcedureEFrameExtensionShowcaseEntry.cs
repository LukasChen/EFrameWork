using EFramework.Runtime.Procedure;
using GameApp.Modules.EFrameExtensionShowcase.UI;
using GameApp.Procedure;
using UnityEngine;

namespace GameApp.Modules.EFrameExtensionShowcase.Procedure
{
    public sealed class ProcedureEFrameExtensionShowcaseEntry : EFrameProcedure
    {
        private EFrameExtensionShowcaseController m_controller;

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
