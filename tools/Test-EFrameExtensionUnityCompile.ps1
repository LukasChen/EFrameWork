param(
    [string]$TargetRoot,
    [string]$UnityPath,
    [string[]]$PackageName = @(),
    [string[]]$AssemblyName = @(),
    [switch]$SkipUnityImport,
    [switch]$SkipRestoreImport,
    [switch]$KeepManifestChanges
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$frameworkRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path

if ([string]::IsNullOrWhiteSpace($TargetRoot)) {
    $TargetRoot = Join-Path $frameworkRoot "test-fixtures/EFrameConsumerUnity"
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

$extensionPackages = [ordered]@{
    "com.eframework.ui.virtual-list" = @{
        Path = "packages/com.eframework.ui.virtual-list"
        Assemblies = @("EFrame.UI.VirtualList")
    }
    "com.eframework.ui-extras" = @{
        Path = "packages/com.eframework.ui-extras"
        Assemblies = @("EFrame.UI.Extras")
    }
    "com.eframework.effects" = @{
        Path = "packages/com.eframework.effects"
        Assemblies = @("EFrame.Effects", "EFrame.Effects.Editor")
    }
    "com.eframework.debug-console" = @{
        Path = "packages/com.eframework.debug-console"
        Assemblies = @("IngameDebugConsole.Runtime", "IngameDebugConsole.Editor")
    }
}

if ($PackageName.Count -eq 0) {
    $PackageName = @($extensionPackages.Keys)
}
else {
    $PackageName = @($PackageName | ForEach-Object { $_ -split "," } | ForEach-Object { $_.Trim() } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

if ($AssemblyName.Count -gt 0) {
    $AssemblyName = @($AssemblyName | ForEach-Object { $_ -split "," } | ForEach-Object { $_.Trim() } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

$unknownPackages = $PackageName | Where-Object { -not $extensionPackages.Contains($_) }
if ($unknownPackages.Count -gt 0) {
    throw "Unknown extension package(s): $($unknownPackages -join ', ')"
}

$targetFullPath = (Resolve-Path -LiteralPath $TargetRoot).Path
$manifestPath = Join-Path $targetFullPath "Packages/manifest.json"
$lockPath = Join-Path $targetFullPath "Packages/packages-lock.json"

if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "Unity manifest not found: $manifestPath"
}

$selectedAssemblies = New-Object System.Collections.Generic.List[string]
if ($AssemblyName.Count -gt 0) {
    foreach ($assembly in $AssemblyName) {
        $selectedAssemblies.Add($assembly)
    }
}
else {
    foreach ($package in $PackageName) {
        foreach ($assembly in $extensionPackages[$package].Assemblies) {
            $selectedAssemblies.Add($assembly)
        }
    }
}

$backupRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("EFrameExtensionCompileManifest-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $backupRoot | Out-Null
$manifestBackup = Join-Path $backupRoot "manifest.json"
$lockBackup = Join-Path $backupRoot "packages-lock.json"
Copy-Item -LiteralPath $manifestPath -Destination $manifestBackup
$hadLock = Test-Path -LiteralPath $lockPath
if ($hadLock) {
    Copy-Item -LiteralPath $lockPath -Destination $lockBackup
}

$unityEditor = $null
if (-not $SkipUnityImport -or -not $SkipRestoreImport) {
    $unityEditor = Resolve-UnityEditor -RequestedPath $UnityPath
}

function Invoke-UnityImport {
    param([string]$Reason)

    $logPath = Join-Path ([System.IO.Path]::GetTempPath()) ("EFrameExtensionCompile-" + [guid]::NewGuid().ToString("N") + ".log")
    $arguments = @(
        "-batchmode",
        "-quit",
        "-projectPath", $targetFullPath,
        "-logFile", $logPath
    )

    Write-Host "$Reason import: $logPath"
    $process = Start-Process -FilePath $unityEditor -ArgumentList $arguments -Wait -PassThru -NoNewWindow
    $logText = ""
    if (Test-Path -LiteralPath $logPath) {
        $logText = Get-Content -LiteralPath $logPath -Raw
    }

    if ($process.ExitCode -ne 0 -or $logText -match "Aborting batchmode due to fatal error|Package Manager resolve failed|Scripts have compiler errors|error CS\d+") {
        throw "$Reason Unity import failed. See log: $logPath"
    }
}

try {
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $dependencies = $manifest.dependencies

    foreach ($package in $PackageName) {
        $relativePackagePath = $extensionPackages[$package].Path
        $absolutePackagePath = Join-Path $frameworkRoot $relativePackagePath
        if (-not (Test-Path -LiteralPath $absolutePackagePath)) {
            throw "Extension package directory not found: $absolutePackagePath"
        }

        $fileDependency = "file:../../../$relativePackagePath"
        $dependencies | Add-Member -NotePropertyName $package -NotePropertyValue $fileDependency -Force
    }

    $manifestJson = $manifest | ConvertTo-Json -Depth 20
    [System.IO.File]::WriteAllText($manifestPath, $manifestJson, [System.Text.UTF8Encoding]::new($false))

    Write-Host "Extension packages: $($PackageName -join ', ')"
    Write-Host "Assemblies: $($selectedAssemblies -join ', ')"

    if (-not $SkipUnityImport) {
        Invoke-UnityImport -Reason "Extension package"
    }

    $assemblyArray = $selectedAssemblies.ToArray()
    & (Join-Path $scriptRoot "Test-EFrameUnityCompile.ps1") -TargetRoot $targetFullPath -UnityPath $UnityPath -AssemblyName $assemblyArray
}
finally {
    if (-not $KeepManifestChanges) {
        Copy-Item -LiteralPath $manifestBackup -Destination $manifestPath -Force
        if ($hadLock) {
            Copy-Item -LiteralPath $lockBackup -Destination $lockPath -Force
        }
        elseif (Test-Path -LiteralPath $lockPath) {
            Remove-Item -LiteralPath $lockPath -Force
        }

        if (-not $SkipRestoreImport -and $unityEditor) {
            try {
                Invoke-UnityImport -Reason "Manifest restore"
            }
            catch {
                Write-Warning $_.Exception.Message
            }
        }
    }
}
