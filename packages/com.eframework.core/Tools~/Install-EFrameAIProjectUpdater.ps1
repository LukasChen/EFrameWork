param(
    [string]$TargetRoot,
    [string]$FrameworkRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [switch]$Force
)

if (-not $TargetRoot) {
    throw "TargetRoot is required."
}

$resolvedTargetRoot = (Resolve-Path $TargetRoot).Path
$resolvedFrameworkRoot = (Resolve-Path $FrameworkRoot).Path
$projectToolsDirectory = Join-Path $resolvedTargetRoot "tools"
$updaterPath = Join-Path $projectToolsDirectory "Sync-EFrameAIFromFramework.ps1"

if (-not (Test-Path $projectToolsDirectory)) {
    New-Item -ItemType Directory -Path $projectToolsDirectory -Force | Out-Null
}

if ((Test-Path $updaterPath) -and -not $Force) {
    Write-Warning "Project updater already exists: $updaterPath (use -Force to overwrite)"
    return
}

$escapedFrameworkRoot = $resolvedFrameworkRoot.Replace("'", "''")

$content = @'
param(
    [string]$FrameworkRoot = '__FRAMEWORK_ROOT__',
    [string[]]$Clients = @("all"),
    [switch]$Force,
    [switch]$StatusOnly
)

$projectRoot = Split-Path -Parent $PSScriptRoot
$syncScript = Join-Path $FrameworkRoot 'Tools~\Initialize-EFrameAI.ps1'
if (-not (Test-Path $syncScript)) {
    $syncScript = Join-Path $FrameworkRoot 'packages\com.eframework.core\Tools~\Initialize-EFrameAI.ps1'
}
if (-not (Test-Path $syncScript)) {
    $syncScript = Join-Path $FrameworkRoot 'tools\Initialize-EFrameAI.ps1'
}

if (-not (Test-Path $syncScript)) {
    throw "Framework sync script not found: $syncScript"
}

& $syncScript -TargetRoot $projectRoot -Clients $Clients -Force:$Force -StatusOnly:$StatusOnly
'@

$content = $content.Replace('__FRAMEWORK_ROOT__', $escapedFrameworkRoot)

Set-Content -Path $updaterPath -Value $content -Encoding UTF8
Write-Host "Project AI updater created at $updaterPath"
Write-Host "Default framework root: $resolvedFrameworkRoot"
