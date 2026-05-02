using EFramework.Runtime.Procedure;
using UnityEngine;

namespace EFrameConsumerFixture.Procedure
{
    public sealed class ProcedureLauncher : EFrameProcedure
    {
        protected override void OnEnter(ProcedureEnterContext context)
        {
            base.OnEnter(context);
            Debug.Log("[ProcedureLauncher] Entered. Switching to ProcedureHome.");
            ChangeState<ProcedureHome>();
        }

        protected override void OnLeave(bool isShutdown)
        {
            base.OnLeave(isShutdown);
        }
    }
}
