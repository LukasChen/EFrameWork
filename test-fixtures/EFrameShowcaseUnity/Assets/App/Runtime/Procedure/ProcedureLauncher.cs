using EFramework.Runtime.Procedure;
using UnityEngine;

namespace GameApp.Procedure
{
    public sealed class ProcedureLauncher : EFrameProcedure
    {
        protected override void OnEnter(ProcedureEnterContext context)
        {
            base.OnEnter(context);
            Debug.Log("[ProcedureLauncher] Entered. Switching to ProcedureHome.");
            ChangeState<ProcedureHome>();
        }
    }
}
