param(
    [string]$TargetRoot,
    [string]$RootNamespace,
    [switch]$Force
)

if (-not $TargetRoot) {
    throw "TargetRoot is required."
}

if (-not (Test-Path $TargetRoot)) {
    throw "TargetRoot does not exist: $TargetRoot"
}

$resolvedTargetRoot = (Resolve-Path $TargetRoot).Path

if (-not $RootNamespace) {
    $RootNamespace = Split-Path -Leaf $resolvedTargetRoot
}

$RootNamespace = $RootNamespace -replace '[^a-zA-Z0-9_\.]', ''

if ([string]::IsNullOrWhiteSpace($RootNamespace)) {
    $RootNamespace = 'GameApp'
}

function Write-TemplateFile {
    param(
        [string]$Path,
        [string]$Content,
        [switch]$Overwrite
    )

    if ((Test-Path $Path) -and -not $Overwrite) {
        Write-Warning "Skip existing file: $Path (use -Force to overwrite)"
        return
    }

    $directory = Split-Path -Parent $Path
    if (-not (Test-Path $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    Set-Content -Path $Path -Value $Content -Encoding UTF8
    Write-Host "Generated file $Path"
}

$resPathContent = @"
namespace $RootNamespace.Common
{
    public static class ResPath
    {
        public const string HomeView = "UI/Views/HomeView";

        public static string GetUIView(string viewName)
        {
            return $"UI/Views/{viewName}";
        }
    }
}
"@

$procedureLauncherContent = @"
using GameFramework.Procedure;
using UnityEngine;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace $RootNamespace.Procedure
{
    public sealed class ProcedureLauncher : ProcedureBase
    {
        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Debug.Log("[ProcedureLauncher] Entered. TODO: preload resources and switch to ProcedureHome.");
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            base.OnLeave(procedureOwner, isShutdown);
        }
    }
}
"@

$procedureHomeContent = @"
using EFrameWork.Runtime;
using GameFramework.Procedure;
using UnityEngine;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace $RootNamespace.Procedure
{
    public sealed class ProcedureHome : ProcedureBase
    {
        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);

            if (EFrame.UI == null)
            {
                Debug.LogError("[ProcedureHome] EFrame UI is not initialized.");
                return;
            }

            Debug.Log("[ProcedureHome] Entered. TODO: open HomeView on QuiPanel.");
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            base.OnLeave(procedureOwner, isShutdown);
        }
    }
}
"@

$homeViewControllerContent = @"
using UnityEngine;

namespace $RootNamespace.UI
{
    public sealed class HomeViewController : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("[HomeViewController] TODO: bind view events and refresh initial state.");
        }
    }
}
"@

$sceneSetupContent = @"
# StartUp Scene Setup

Create a startup scene named `StartUp.unity` under `Assets/Scenes` and keep the root structure minimal:

```text
StartUp
|- Boot
|  |- ProcedureComponent
|  |- EFrameComponent
|- Main Camera
```

Required setup:

1. Add `ProcedureComponent` and `EFrameComponent` to `Boot`
2. Assign the same `ProcedureComponent` instance to `EFrameComponent.m_procedureComponent`
3. Set `Main Camera` and UI camera references on `EFrameComponent`
4. Register these procedures in `ProcedureComponent`:
   - `$RootNamespace.Procedure.ProcedureLauncher`
   - `$RootNamespace.Procedure.ProcedureHome`
5. Set the entrance procedure type name to `$RootNamespace.Procedure.ProcedureLauncher`

Generated placeholder code:

- `Assets/App/Runtime/Common/ResPath.cs`
- `Assets/App/Runtime/Procedure/ProcedureLauncher.cs`
- `Assets/App/Runtime/Procedure/ProcedureHome.cs`
- `Assets/App/Runtime/UI/Controllers/HomeViewController.cs`

Next refactors:

- Replace placeholder logs with actual preload / state transition logic
- Create `HomeView.prefab` under `Assets/App/Res/UI/Panels`
- Replace temporary UI loading code with `ResPath.GetUIView("HomeView")`
"@

Write-TemplateFile -Path (Join-Path $resolvedTargetRoot "Assets/App/Runtime/Common/ResPath.cs") -Content $resPathContent -Overwrite:$Force
Write-TemplateFile -Path (Join-Path $resolvedTargetRoot "Assets/App/Runtime/Procedure/ProcedureLauncher.cs") -Content $procedureLauncherContent -Overwrite:$Force
Write-TemplateFile -Path (Join-Path $resolvedTargetRoot "Assets/App/Runtime/Procedure/ProcedureHome.cs") -Content $procedureHomeContent -Overwrite:$Force
Write-TemplateFile -Path (Join-Path $resolvedTargetRoot "Assets/App/Runtime/UI/Controllers/HomeViewController.cs") -Content $homeViewControllerContent -Overwrite:$Force
Write-TemplateFile -Path (Join-Path $resolvedTargetRoot "Assets/Scenes/StartUp_SETUP.md") -Content $sceneSetupContent -Overwrite:$Force

Write-Host "EFrame bootstrap code scaffold complete for namespace $RootNamespace"