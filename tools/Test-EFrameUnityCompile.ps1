param(
    [string]$TargetRoot = (Get-Location).Path,
    [string]$UnityPath,
    [string]$CscPath,
    [string[]]$AssemblyName = @(),
    [switch]$ListOnly
)

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$frameworkRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path
$packageScript = Join-Path $frameworkRoot "packages/com.eframework.core/Tools~/Test-EFrameUnityCompile.ps1"

if (-not (Test-Path -LiteralPath $packageScript)) {
    throw "Package Unity compile check script not found: $packageScript"
}

& $packageScript -TargetRoot $TargetRoot -UnityPath $UnityPath -CscPath $CscPath -AssemblyName $AssemblyName -ListOnly:$ListOnly
