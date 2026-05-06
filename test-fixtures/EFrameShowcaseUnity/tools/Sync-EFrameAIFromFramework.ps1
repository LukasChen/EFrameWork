param(
    [string]$FrameworkRoot = 'C:\Users\Ethan\Desktop\Work\project\EFrameWork\packages\com.eframework.core',
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
