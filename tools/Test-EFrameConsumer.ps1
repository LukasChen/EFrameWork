param(
    [string]$ProjectPath,
    [string]$UnityPath,
    [string]$LogPath,
    [string]$TestResultsPath,
    [string[]]$TestPlatforms = @("EditMode"),
    [switch]$SkipColdStart,
    [string]$RootNamespace = "EFrameConsumerFixture"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$frameworkRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $frameworkRoot "test-fixtures/EFrameConsumerUnity"
}

if ([string]::IsNullOrWhiteSpace($LogPath)) {
    $LogPath = Join-Path ([System.IO.Path]::GetTempPath()) "EFrameConsumerUnity.log"
}

if ([string]::IsNullOrWhiteSpace($TestResultsPath)) {
    $TestResultsPath = Join-Path ([System.IO.Path]::GetTempPath()) "EFrameConsumerUnity.EditModeResults.xml"
}

function Resolve-UnityEditor {
    param([string]$RequestedPath)

    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        if (-not (Test-Path -LiteralPath $RequestedPath)) {
            throw "Unity executable not found: $RequestedPath"
        }

        return (Resolve-Path -LiteralPath $RequestedPath).Path
    }

    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_EXE) -and (Test-Path -LiteralPath $env:UNITY_EXE)) {
        return (Resolve-Path -LiteralPath $env:UNITY_EXE).Path
    }

    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_PATH) -and (Test-Path -LiteralPath $env:UNITY_PATH)) {
        return (Resolve-Path -LiteralPath $env:UNITY_PATH).Path
    }

    $hubRoot = "C:\Program Files\Unity\Hub\Editor"
    if (Test-Path -LiteralPath $hubRoot) {
        $candidate = Get-ChildItem -LiteralPath $hubRoot -Directory |
            Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName "Editor/Unity.exe" } |
            Where-Object { Test-Path -LiteralPath $_ } |
            Select-Object -First 1

        if ($candidate) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    throw "Unity executable not found. Pass -UnityPath or set UNITY_EXE."
}

$projectFullPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$unityEditor = Resolve-UnityEditor -RequestedPath $UnityPath

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $LogPath) | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $TestResultsPath) | Out-Null

Write-Host "Unity: $unityEditor"
Write-Host "Consumer project: $projectFullPath"
Write-Host "Log: $LogPath"
Write-Host "Results: $TestResultsPath"
Write-Host "Cold start: $(-not $SkipColdStart)"

$compilerErrorPattern = "(?m)(error CS\d+|Scripts have compiler errors|Aborting batchmode due to fatal error|Package Manager resolve failed|An error occurred while resolving packages)"

function Invoke-UnityBatch {
    param(
        [string[]]$Arguments,
        [string]$FailureMessage
    )

    if (Test-Path -LiteralPath $LogPath) {
        Remove-Item -LiteralPath $LogPath -Force
    }

    $process = Start-Process -FilePath $unityEditor -ArgumentList $Arguments -Wait -PassThru -NoNewWindow
    $unityExitCode = $process.ExitCode

    $logText = ""
    if (Test-Path -LiteralPath $LogPath) {
        $logText = Get-Content -LiteralPath $LogPath -Raw
    }

    if ($logText -match $compilerErrorPattern) {
        Write-Error "$FailureMessage See log: $LogPath"
    }

    if ($unityExitCode -ne 0) {
        Write-Error "Unity exited with code $unityExitCode. See log: $LogPath"
    }
}

function Get-PlatformResultsPath {
    param([string]$Platform)

    $directory = Split-Path -Parent $TestResultsPath
    $fileName = [System.IO.Path]::GetFileNameWithoutExtension($TestResultsPath)
    $extension = [System.IO.Path]::GetExtension($TestResultsPath)
    return Join-Path $directory "$fileName.$Platform$extension"
}

function Assert-TestResultsPassed {
    param(
        [string]$Platform,
        [string]$ResultsPath
    )

    if (-not (Test-Path -LiteralPath $ResultsPath)) {
        Write-Error "Unity did not produce $Platform test results: $ResultsPath"
    }

    [xml]$results = Get-Content -LiteralPath $ResultsPath -Raw
    $failed = [int]$results."test-run".failed
    $inconclusive = [int]$results."test-run".inconclusive

    if ($failed -gt 0 -or $inconclusive -gt 0) {
        Write-Error "EFrame consumer fixture $Platform tests failed. Failed: $failed; Inconclusive: $inconclusive. Results: $ResultsPath"
    }
}

function Invoke-EFrameColdStart {
    if ($SkipColdStart) {
        return
    }

    $coldStartScript = Join-Path $projectFullPath "Packages/com.eframework.core/Tools~/Initialize-EFrameColdStart.ps1"
    if (-not (Test-Path -LiteralPath $coldStartScript)) {
        $coldStartScript = Join-Path $frameworkRoot "packages/com.eframework.core/Tools~/Initialize-EFrameColdStart.ps1"
    }

    if (-not (Test-Path -LiteralPath $coldStartScript)) {
        throw "Initialize-EFrameColdStart.ps1 not found."
    }

    Write-Host "Running EFrame cold start: $coldStartScript"
    & $coldStartScript -TargetRoot $projectFullPath -RootNamespace $RootNamespace -Force

    $requiredPaths = @(
        "Assets/App/Runtime/Common/ResPath.cs",
        "Assets/App/Runtime/Generated/Res/ResPath.Generated.cs",
        "Assets/App/Runtime/Procedure/ProcedureLauncher.cs",
        "Assets/App/Runtime/Procedure/ProcedureHome.cs",
        "Assets/App/Runtime/UI/Views/HomeView.cs",
        "Assets/App/Runtime/UI/Controllers/HomeViewController.cs",
        "Assets/Resources/Audio/AudioEventConfig.asset",
        "Assets/Scenes/StartUp_SETUP.md"
    )

    foreach ($relativePath in $requiredPaths) {
        $absolutePath = Join-Path $projectFullPath $relativePath
        if (-not (Test-Path -LiteralPath $absolutePath)) {
            throw "Cold start did not generate expected file: $relativePath"
        }
    }
}

Invoke-EFrameColdStart

$importArguments = @(
    "-batchmode",
    "-quit",
    "-projectPath", $projectFullPath,
    "-logFile", $LogPath
)

Invoke-UnityBatch -Arguments $importArguments -FailureMessage "EFrame consumer fixture import failed."

foreach ($platform in $TestPlatforms) {
    $platformResultsPath = Get-PlatformResultsPath -Platform $platform
    if (Test-Path -LiteralPath $platformResultsPath) {
        Remove-Item -LiteralPath $platformResultsPath -Force
    }

    $testArguments = @(
        "-batchmode",
        "-projectPath", $projectFullPath,
        "-runTests",
        "-testPlatform", $platform,
        "-testResults", $platformResultsPath,
        "-logFile", $LogPath
    )

    Invoke-UnityBatch -Arguments $testArguments -FailureMessage "EFrame consumer fixture $platform tests failed before results were produced."
    Assert-TestResultsPassed -Platform $platform -ResultsPath $platformResultsPath
}

Write-Host "EFrame consumer fixture passed."
