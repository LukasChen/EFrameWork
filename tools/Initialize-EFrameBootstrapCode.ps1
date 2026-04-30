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
    public static partial class ResPath
    {
        public static class App
        {
            public static class UI
            {
                public static string Asset(string relativePath)
                {
                    return Normalize($"UI/{relativePath}");
                }

                public static string Panel(string viewName)
                {
                    return Normalize($"UI/Panels/{viewName}");
                }

                public static string Panel(string panelName, string viewName)
                {
                    return Normalize($"UI/Panels/{panelName}/{viewName}");
                }

                public static string Popup(string viewName)
                {
                    return Normalize($"UI/Popups/{viewName}");
                }

                public static string Widget(string viewName)
                {
                    return Normalize($"UI/Widgets/{viewName}");
                }
            }

            public static string Bootstrap(string assetName)
            {
                return Normalize($"Bootstrap/{assetName}");
            }

            public static string SceneAsset(string assetName)
            {
                return Normalize($"SceneAssets/{assetName}");
            }
        }

        public static class Scenes
        {
            public static string Project(string sceneName)
            {
                return Normalize($"Scenes/{sceneName}");
            }
        }

        public static class Modules
        {
            public static string Asset(string moduleName, string relativePath)
            {
                return Normalize($"Modules/{moduleName}/{relativePath}");
            }

            public static string Scene(string moduleName, string sceneName)
            {
                return Normalize($"Modules/{moduleName}/Scenes/{sceneName}");
            }
        }

        public static string GetUIView(string viewName)
        {
            return App.UI.Panel(viewName);
        }

        private static string Normalize(string address)
        {
            return address.Replace('\\', '/').Trim('/');
        }
    }
}
"@

$procedureLauncherContent = @"
using EFrameWork.Runtime.Procedure;
using UnityEngine;

namespace $RootNamespace.Procedure
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
"@

$procedureHomeContent = @"
using EFrameWork.Runtime;
using EFrameWork.Runtime.Procedure;
using $RootNamespace.Modules.SampleModule.Procedure;
using $RootNamespace.UI.Controllers;
using UnityEngine;

namespace $RootNamespace.Procedure
{
    public sealed class ProcedureHome : EFrameProcedure
    {
        private HomeViewController m_homeViewController;

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
"@

$homeViewContent = @"
using $RootNamespace.Common;
using EFrameWork.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace $RootNamespace.UI.Views
{
    public sealed class HomeView : BindingViewBase
    {
        public Button ModuleTestButton { get; private set; }

        public HomeView()
        {
        }

        protected override void OnBindingSet()
        {
            base.OnBindingSet();
            CacheComponents();
        }

        private void CacheComponents()
        {
            ModuleTestButton = Binding == null ? null : Binding.transform.Find("Panel/ModuleTestButton")?.GetComponent<Button>();
        }

        public static string DefaultAssetPath => ResPath.App.UI.Panel("Home", "HomeView");
    }
}
"@

$homeViewControllerContent = @"
using System;
using $RootNamespace.Common;
using $RootNamespace.UI.Views;
using EFrameWork.Runtime.UI;

namespace $RootNamespace.UI.Controllers
{
    public sealed class HomeViewController : UIControllerBase<HomeView>
    {
        public Action ModuleTestRequested { get; set; }

        protected override string AssetPath => ResPath.App.UI.Panel("Home", "HomeView");

        protected override void OnViewCreated()
        {
            base.OnViewCreated();
            AddButtonClickListener(TypedViewHandle.TypedView.ModuleTestButton, OnModuleTestButtonClick);
        }

        protected override void OnViewDestroyed()
        {
            ModuleTestRequested = null;
            base.OnViewDestroyed();
        }

        private void OnModuleTestButtonClick()
        {
            ModuleTestRequested?.Invoke();
        }
    }
}
"@

$bootstrapReadmeContent = @"
# Bootstrap Resources

Put startup-critical resources here when they must be available before the main flow enters normal on-demand loading.

Recommended contents:

- loading screen prefabs
- startup config assets
- always-resident shared atlases used before the home page finishes loading

Addressables note:

- assets under `Assets/App/Res/Bootstrap` are mapped to the default `App Bootstrap Group`
- avoid putting feature-specific resources here; keep this folder small and stable
"@

function New-ModuleScaffold {
    param(
        [string]$ModuleName
    )

    $moduleNamespace = "$RootNamespace.Modules.$ModuleName"

    $moduleGuideContent = @"
# $ModuleName

This is the default formal module scaffold generated by EFrame bootstrap.

Recommended next steps:

1. Replace placeholder logs with actual module entry logic.
2. Create the module's main view prefab under `Assets/Modules/$ModuleName/Res/UI/Panels/${ModuleName}Main` and add a `QUIBinding` root.
3. Add module scenes under `Assets/Modules/$ModuleName/Scenes` if this module owns scene content.
4. Put module-private FX under `Assets/Modules/$ModuleName/Res/FX`; only move shared content back to `Assets/App/...` when it is truly common.
"@

    $moduleResPathContent = @"
using $RootNamespace.Common;

namespace $moduleNamespace.Common
{
    public static class ${ModuleName}ResPath
    {
        public static string MainView => ResPath.Modules.Asset("$ModuleName", "Res/UI/Panels/${ModuleName}Main/${ModuleName}MainView");

        public static string GetScene(string sceneName)
        {
            return ResPath.Modules.Scene("$ModuleName", sceneName);
        }
    }
}
"@

    $moduleProcedureContent = @"
using $RootNamespace.Procedure;
using EFrameWork.Runtime.Procedure;
using $moduleNamespace.UI.Controllers;
using UnityEngine;

namespace $moduleNamespace.Procedure
{
    public sealed class Procedure${ModuleName}Entry : EFrameProcedure
    {
        private ${ModuleName}MainViewController m_viewController;

        protected override void OnEnter(ProcedureEnterContext context)
        {
            base.OnEnter(context);

            m_viewController = new ${ModuleName}MainViewController
            {
                BackRequested = () => ChangeState<ProcedureHome>()
            };

            m_viewController.Show();
            Debug.Log("[Procedure${ModuleName}Entry] Entered. ${ModuleName}MainView is shown.");
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
"@

    $moduleViewContent = @"
using EFrameWork.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace $moduleNamespace.UI.Views
{
    public sealed class ${ModuleName}MainView : BindingViewBase
    {
        public Button BackButton { get; private set; }

        public ${ModuleName}MainView()
        {
        }

        protected override void OnBindingSet()
        {
            base.OnBindingSet();
            CacheComponents();
        }

        private void CacheComponents()
        {
            BackButton = Binding == null ? null : Binding.transform.Find("Panel/BackButton")?.GetComponent<Button>();
        }
    }
}
"@

    $moduleControllerContent = @"
using System;
using EFrameWork.Runtime.UI;
using $moduleNamespace.Common;
using $moduleNamespace.UI.Views;

namespace $moduleNamespace.UI.Controllers
{
    public sealed class ${ModuleName}MainViewController : UIControllerBase<${ModuleName}MainView>
    {
        public Action BackRequested { get; set; }

        protected override string AssetPath => ${ModuleName}ResPath.MainView;

        protected override void OnViewCreated()
        {
            base.OnViewCreated();
            AddButtonClickListener(TypedViewHandle.TypedView.BackButton, OnBackButtonClick);
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
"@

    Write-TemplateFile -Path (Join-Path $resolvedTargetRoot "Assets/Modules/$ModuleName/README.md") -Content $moduleGuideContent -Overwrite:$Force
    Write-TemplateFile -Path (Join-Path $resolvedTargetRoot "Assets/Modules/$ModuleName/Runtime/Common/${ModuleName}ResPath.cs") -Content $moduleResPathContent -Overwrite:$Force
    Write-TemplateFile -Path (Join-Path $resolvedTargetRoot "Assets/Modules/$ModuleName/Runtime/Procedure/Procedure${ModuleName}Entry.cs") -Content $moduleProcedureContent -Overwrite:$Force
    Write-TemplateFile -Path (Join-Path $resolvedTargetRoot "Assets/Modules/$ModuleName/Runtime/UI/Views/${ModuleName}MainView.cs") -Content $moduleViewContent -Overwrite:$Force
    Write-TemplateFile -Path (Join-Path $resolvedTargetRoot "Assets/Modules/$ModuleName/Runtime/UI/Controllers/${ModuleName}MainViewController.cs") -Content $moduleControllerContent -Overwrite:$Force

    $moduleResourceDirectories = @(
        "Assets/Modules/$ModuleName/Res/UI/Panels/${ModuleName}Main",
        "Assets/Modules/$ModuleName/Res/UI/Common",
        "Assets/Modules/$ModuleName/Res/SceneAssets/Common",
        "Assets/Modules/$ModuleName/Res/FX/Common",
        "Assets/Modules/$ModuleName/Res/FX/UI",
        "Assets/Modules/$ModuleName/Res/FX/Scene",
        "Assets/Modules/$ModuleName/Res/FX/Gameplay",
        "Assets/Modules/$ModuleName/Scenes"
    )

    foreach ($relativeDirectory in $moduleResourceDirectories) {
        $directoryPath = Join-Path $resolvedTargetRoot $relativeDirectory
        if (-not (Test-Path $directoryPath)) {
            New-Item -ItemType Directory -Path $directoryPath -Force | Out-Null
            Write-Host "Created directory $directoryPath"
        }
    }
}

$sceneSetupContent = @"
# StartUp Scene Setup

Create a startup scene named `StartUp.unity` under `Assets/Scenes` and keep the root structure minimal:

```text
StartUp
|- Boot
|  |- EFrameProcedureComponent
|  |- EFrameComponent
|- Main Camera
```

Required setup:

1. Add `EFrameProcedureComponent` and `EFrameComponent` to `Boot`
2. Assign the same `EFrameProcedureComponent` instance to `EFrameComponent.m_procedureComponent`
3. Set `Main Camera` and UI camera references on `EFrameComponent`
4. Register these procedures in `EFrameProcedureComponent`:
    - `$RootNamespace.Procedure.ProcedureLauncher`
    - `$RootNamespace.Procedure.ProcedureHome`
    - `$RootNamespace.Modules.SampleModule.Procedure.ProcedureSampleModuleEntry`
5. Set the entrance procedure type name to `$RootNamespace.Procedure.ProcedureLauncher`
6. In Unity Editor, run `Copy Sample UI Prefab Templates` once so HomeView and SampleModuleMainView prefabs exist.

Generated placeholder code:

- `Assets/App/Runtime/Common/ResPath.cs`
- `Assets/App/Runtime/Generated/Res/ResPath.Generated.cs` (generated after Addressables groups are ready)
- `Assets/App/Runtime/Procedure/ProcedureLauncher.cs`
- `Assets/App/Runtime/Procedure/ProcedureHome.cs`
- `Assets/App/Runtime/UI/Views/HomeView.cs`
- `Assets/App/Runtime/UI/Controllers/HomeViewController.cs`
- `Assets/App/Res/UI/Panels/Home/HomeView.prefab` (copied from the package template in Unity Editor)
- `Assets/App/Res/Bootstrap/README.md`
- `Assets/Modules/SampleModule/README.md`
- `Assets/Modules/SampleModule/Runtime/Common/SampleModuleResPath.cs`
- `Assets/Modules/SampleModule/Runtime/Procedure/ProcedureSampleModuleEntry.cs`
- `Assets/Modules/SampleModule/Runtime/UI/Views/SampleModuleMainView.cs`
- `Assets/Modules/SampleModule/Runtime/UI/Controllers/SampleModuleMainViewController.cs`
- `Assets/Modules/SampleModule/Res/UI/Panels/SampleModuleMain/SampleModuleMainView.prefab` (copied from the package template in Unity Editor)

Next refactors:

- Replace the sample prefab visuals with your project's final art style while keeping QUIBinding names stable
- Put startup-only resources under `Assets/App/Res/Bootstrap`
- Use `SampleModule` as the reference example when creating your first real module
- Prefer generated constants such as `ResPath.Generated.*` once Addressables groups have been synced
"@

Write-TemplateFile -Path (Join-Path $resolvedTargetRoot "Assets/App/Runtime/Common/ResPath.cs") -Content $resPathContent -Overwrite:$Force
Write-TemplateFile -Path (Join-Path $resolvedTargetRoot "Assets/App/Runtime/Procedure/ProcedureLauncher.cs") -Content $procedureLauncherContent -Overwrite:$Force
Write-TemplateFile -Path (Join-Path $resolvedTargetRoot "Assets/App/Runtime/Procedure/ProcedureHome.cs") -Content $procedureHomeContent -Overwrite:$Force
Write-TemplateFile -Path (Join-Path $resolvedTargetRoot "Assets/App/Runtime/UI/Views/HomeView.cs") -Content $homeViewContent -Overwrite:$Force
Write-TemplateFile -Path (Join-Path $resolvedTargetRoot "Assets/App/Runtime/UI/Controllers/HomeViewController.cs") -Content $homeViewControllerContent -Overwrite:$Force
Write-TemplateFile -Path (Join-Path $resolvedTargetRoot "Assets/App/Res/Bootstrap/README.md") -Content $bootstrapReadmeContent -Overwrite:$Force
New-ModuleScaffold -ModuleName "SampleModule"
Write-TemplateFile -Path (Join-Path $resolvedTargetRoot "Assets/Scenes/StartUp_SETUP.md") -Content $sceneSetupContent -Overwrite:$Force

Write-Host "EFrame bootstrap code scaffold complete for namespace $RootNamespace"
