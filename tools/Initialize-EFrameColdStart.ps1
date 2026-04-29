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
    $aiDocsState = if ((Test-RelativePath "AGENTS.md") -and (Test-RelativePath "EFRAME_AI_ARCHITECTURE.md") -and (Test-RelativePath "EFRAME_AI_SETUP.md") -and (Test-RelativePath "EFRAME_AI_RELEASE_CHECKLIST.md")) { "OK" } else { "WARN" }

    Write-ColdStartStatus -Name "Directory scaffold" -State $directoryState -Detail "$($directories.Count) standard directories under Assets/ and tools/"
    Write-ColdStartStatus -Name "AI workspace" -State (Get-StateForPath ".github/eframe-ai.manifest.json" $false) -Detail ".github instructions, skills, and manifest"
    Write-ColdStartStatus -Name "AI docs" -State $aiDocsState -Detail "AGENTS.md, architecture, setup, and release checklist docs"
    Write-ColdStartStatus -Name "Project updater" -State (Get-StateForPath "tools/Sync-EFrameAIFromFramework.ps1" $SkipProjectUpdater) -Detail "tools/Sync-EFrameAIFromFramework.ps1"
    Write-ColdStartStatus -Name "Project AI overlay" -State (Get-StateForPath ".github/instructions/project-local.instructions.md" $SkipProjectOverlay) -Detail ".github/instructions/project-local.instructions.md"
    Write-ColdStartStatus -Name "Bootstrap code" -State (Get-StateForPath "Assets/App/Runtime/Common/ResPath.cs" $SkipBootstrapCode) -Detail "ResPath, Procedure, Home UI, SampleModule, and StartUp_SETUP.md"
    Write-ColdStartStatus -Name "Audio event config" -State (Get-StateForPath "Assets/Resources/Audio/AudioEventConfig.asset" $false) -Detail "Resources config loaded by AudioEventManager"

    Write-Host ""
    Write-Host "Next steps:"
    Write-Host "1. Open the project in VS Code from $resolvedTargetRoot"

    $step = 2
    if ($SkipProjectOverlay) {
        Write-Host "$step. Create a project overlay later with tools/New-EFrameProjectAIOverlay.ps1"
    }
    else {
        Write-Host "$step. Fill in .github/instructions/project-local.instructions.md with project-specific rules"
    }
    $step++

    if ($SkipBootstrapCode) {
        Write-Host "$step. Generate bootstrap code later with tools/Initialize-EFrameBootstrapCode.ps1"
    }
    else {
        Write-Host "$step. Open Assets/Scenes/StartUp_SETUP.md and build the startup scene in Unity"
    }
    $step++

    if ($SkipProjectUpdater) {
        Write-Host "$step. Install the project updater later with tools/Install-EFrameAIProjectUpdater.ps1"
    }
    else {
        Write-Host "$step. Continue upgrades with tools/Sync-EFrameAIFromFramework.ps1 -StatusOnly / -Force"
    }
}

function New-AudioEventConfigAsset {
    $relativeAssetPath = "Assets/Resources/Audio/AudioEventConfig.asset"
    $assetPath = Join-Path $resolvedTargetRoot $relativeAssetPath

    if (Test-Path $assetPath) {
        Write-Host "Audio event config already exists: $assetPath"
        return
    }

    $directory = Split-Path -Parent $assetPath
    if (-not (Test-Path $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $content = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 3d1aba615367ca54fbb9f141bba21c7d, type: 3}
  m_Name: AudioEventConfig
  m_EditorClassIdentifier:
  ConfigItems: []
"@

    Set-Content -Path $assetPath -Value $content -Encoding UTF8
    Write-Host "Generated audio event config $assetPath"
}

if (-not $SkipDirectoryScaffold) {

    foreach ($relativeDirectory in $directories) {
        New-RequiredDirectory -Path (Join-Path $resolvedTargetRoot $relativeDirectory)
    }
}

New-AudioEventConfigAsset

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
Write-ColdStartSummary
