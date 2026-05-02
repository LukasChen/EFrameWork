param(
    [string]$TargetRoot,
    [switch]$Force,
    [switch]$IncludeAIWorkspace,
    [string[]]$AIClients = @("all"),
    # Deprecated: project-owned AI rules now live directly in project instructions outside EFrame managed blocks.
    [switch]$SkipProjectOverlay,
    [switch]$SkipProjectUpdater,
    [switch]$SkipDirectoryScaffold,
    [switch]$SkipBootstrapCode
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

$directories = @(
    "Assets/App/Editor",
    "Assets/App/Runtime/Common",
    "Assets/App/Runtime/Config",
    "Assets/App/Runtime/Data",
    "Assets/App/Runtime/Events",
    "Assets/App/Runtime/Generated",
    "Assets/App/Runtime/Procedure",
    "Assets/App/Runtime/Scene",
    "Assets/App/Runtime/Services",
    "Assets/App/Runtime/UI/Controllers",
    "Assets/App/Runtime/UI/Views",
    "Assets/App/Runtime/UI/Widgets",
    "Assets/App/Res/Audios",
    "Assets/App/Res/Bootstrap",
    "Assets/App/Res/Config",
    "Assets/App/Res/Fonts",
    "Assets/App/Res/FX",
    "Assets/App/Res/FX/Common",
    "Assets/App/Res/FX/UI",
    "Assets/App/Res/FX/Scene",
    "Assets/App/Res/FX/Gameplay",
    "Assets/App/Res/Materials",
    "Assets/App/Res/SceneAssets",
    "Assets/App/Res/SceneAssets/Common",
    "Assets/App/Res/Shaders",
    "Assets/App/Res/UI/Common",
    "Assets/App/Res/UI/Common/Atlases",
    "Assets/App/Res/UI/Common/Materials",
    "Assets/App/Res/UI/Common/Sprites",
    "Assets/App/Res/UI/Common/Transitions",
    "Assets/App/Res/UI/Panels",
    "Assets/App/Res/UI/Panels/Home",
    "Assets/App/Res/UI/Popups",
    "Assets/App/Res/UI/Widgets",
    "Assets/Modules",
    "Assets/Resources/Audio",
    "Assets/Scenes",
    "Assets/Settings",
    "tools"
)

function Test-RelativePath {
    param(
        [string]$RelativePath
    )

    return Test-Path (Join-Path $resolvedTargetRoot $RelativePath)
}

function Write-ColdStartStatus {
    param(
        [string]$Name,
        [string]$State,
        [string]$Detail
    )

    Write-Host "[$State] $Name - $Detail"
}

function Get-StateForPath {
    param(
        [string]$RelativePath,
        [bool]$Skipped
    )

    if ($Skipped) {
        return "SKIP"
    }

    if (Test-RelativePath $RelativePath) {
        return "OK"
    }

    return "WARN"
}

function Write-ColdStartSummary {
    Write-Host ""
    Write-Host "EFrame cold start summary"
    Write-Host "Target root: $resolvedTargetRoot"
    Write-Host "Framework root: $frameworkRoot"
    Write-Host ""

    $directoryState = if ($SkipDirectoryScaffold) { "SKIP" } elseif (Test-RelativePath "Assets/App/Runtime" ) { "OK" } else { "WARN" }
    Write-ColdStartStatus -Name "Directory scaffold" -State $directoryState -Detail "$($directories.Count) standard directories under Assets/ and tools/"
    Write-ColdStartStatus -Name "AI workspace" -State (Get-StateForPath ".github/eframe-ai.manifest.json" (-not $IncludeAIWorkspace)) -Detail ".github instructions, skills, and manifest"
    Write-ColdStartStatus -Name "Project updater" -State (Get-StateForPath "tools/Sync-EFrameAIFromFramework.ps1" ((-not $IncludeAIWorkspace) -or $SkipProjectUpdater)) -Detail "tools/Sync-EFrameAIFromFramework.ps1"
    Write-ColdStartStatus -Name "Bootstrap code" -State (Get-StateForPath "Assets/App/Runtime/Generated/Res/ResPath.Generated.cs" $SkipBootstrapCode) -Detail "Generated ResPath, Procedure, Home UI, SampleModule, and StartUp_SETUP.md"

    Write-Host ""
    Write-Host "Next steps:"
    Write-Host "1. Open the project in VS Code from $resolvedTargetRoot"

    $step = 2
    if (-not $IncludeAIWorkspace) {
        Write-Host "$step. Open Unity and use EFrame Tools/AI to choose AI clients and sync the AI workspace"
    }
    else {
        Write-Host "$step. Add project-specific AI rules outside the EFrame managed block in your project instruction files"
    }
    $step++

    if ($SkipBootstrapCode) {
        Write-Host "$step. Generate bootstrap code later with tools/Initialize-EFrameBootstrapCode.ps1"
    }
    else {
        Write-Host "$step. Open Assets/Scenes/StartUp_SETUP.md and build the startup scene in Unity"
    }
    $step++

    if (-not $IncludeAIWorkspace) {
        Write-Host "$step. After syncing AI, run the AI health check from EFrame Tools/AI"
    }
    elseif ($SkipProjectUpdater) {
        Write-Host "$step. Install the project updater later with tools/Install-EFrameAIProjectUpdater.ps1"
    }
    else {
        Write-Host "$step. Continue upgrades with tools/Sync-EFrameAIFromFramework.ps1 -StatusOnly / -Force"
    }
}

if (-not $SkipDirectoryScaffold) {

    foreach ($relativeDirectory in $directories) {
        New-RequiredDirectory -Path (Join-Path $resolvedTargetRoot $relativeDirectory)
    }
}

if ($IncludeAIWorkspace) {
    & (Join-Path $scriptRoot "Initialize-EFrameAI.ps1") -TargetRoot $resolvedTargetRoot -Clients $AIClients -Force:$Force
}
else {
    Write-Host "Skipped AI workspace sync. Use the Unity EFrame Tools/AI menu or run Initialize-EFrameAI.ps1 when you are ready to choose AI clients."
}

if ($IncludeAIWorkspace -and -not $SkipProjectUpdater) {
    & (Join-Path $scriptRoot "Install-EFrameAIProjectUpdater.ps1") -TargetRoot $resolvedTargetRoot -FrameworkRoot $frameworkRoot -Force:$Force
}

if (-not $SkipBootstrapCode) {
    & (Join-Path $scriptRoot "Initialize-EFrameBootstrapCode.ps1") -TargetRoot $resolvedTargetRoot -Force:$Force
}

Write-Host "EFrame cold start complete for $resolvedTargetRoot"
Write-ColdStartSummary
