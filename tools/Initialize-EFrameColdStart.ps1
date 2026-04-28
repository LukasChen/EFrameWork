param(
    [string]$TargetRoot,
    [switch]$Force,
    [switch]$SkipProjectOverlay,
    [switch]$SkipProjectUpdater,
    [switch]$SkipDirectoryScaffold,
    [switch]$SkipBootstrapCode,
    [string]$RootNamespace
)

if (-not $TargetRoot) {
    throw "TargetRoot is required."
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$frameworkRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path

if (-not (Test-Path $TargetRoot)) {
    New-Item -ItemType Directory -Path $TargetRoot -Force | Out-Null
}

$resolvedTargetRoot = (Resolve-Path $TargetRoot).Path

function New-RequiredDirectory {
    param(
        [string]$Path
    )

    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
        Write-Host "Created directory $Path"
    }
}

if (-not $SkipDirectoryScaffold) {
    $directories = @(
        "Assets/App/Runtime/Common",
        "Assets/App/Runtime/Config",
        "Assets/App/Runtime/Data",
        "Assets/App/Runtime/Events",
        "Assets/App/Runtime/Procedure",
        "Assets/App/Runtime/Scene",
        "Assets/App/Runtime/Services",
        "Assets/App/Runtime/UI/Controllers",
        "Assets/App/Runtime/UI/Views",
        "Assets/App/Runtime/UI/Widgets",
        "Assets/App/Res/Audios",
        "Assets/App/Res/Config",
        "Assets/App/Res/Fonts",
        "Assets/App/Res/FX",
        "Assets/App/Res/Materials",
        "Assets/App/Res/Scenes",
        "Assets/App/Res/Shaders",
        "Assets/App/Res/UI/Common",
        "Assets/App/Res/UI/Panels",
        "Assets/App/Res/UI/Popups",
        "Assets/App/Res/UI/Widgets",
        "Assets/MiniGames",
        "Assets/Scenes",
        "Assets/Settings",
        "tools"
    )

    foreach ($relativeDirectory in $directories) {
        New-RequiredDirectory -Path (Join-Path $resolvedTargetRoot $relativeDirectory)
    }
}

& (Join-Path $frameworkRoot "tools\Initialize-EFrameAI.ps1") -TargetRoot $resolvedTargetRoot -Force:$Force

if (-not $SkipProjectUpdater) {
    & (Join-Path $frameworkRoot "tools\Install-EFrameAIProjectUpdater.ps1") -TargetRoot $resolvedTargetRoot -FrameworkRoot $frameworkRoot -Force:$Force
}

if (-not $SkipProjectOverlay) {
    & (Join-Path $frameworkRoot "tools\New-EFrameProjectAIOverlay.ps1") -TargetRoot $resolvedTargetRoot -Force:$Force
}

if (-not $SkipBootstrapCode) {
    & (Join-Path $frameworkRoot "tools\Initialize-EFrameBootstrapCode.ps1") -TargetRoot $resolvedTargetRoot -RootNamespace $RootNamespace -Force:$Force
}

Write-Host "EFrame cold start complete for $resolvedTargetRoot"
Write-Host "Next steps:"
Write-Host "1. Open the project in VS Code from $resolvedTargetRoot"
Write-Host "2. Fill in .github/instructions/project-local.instructions.md with project-specific rules"
Write-Host "3. Open Assets/Scenes/StartUp_SETUP.md and build the startup scene in Unity"
Write-Host "4. Continue upgrades with tools/Sync-EFrameAIFromFramework.ps1 -StatusOnly / -Force"