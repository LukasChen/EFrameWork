param(
    [string]$TargetRoot = (Get-Location).Path,
    [string]$UnityPath,
    [string]$CscPath,
    [string[]]$AssemblyName = @(),
    [switch]$ListOnly
)

$ErrorActionPreference = "Stop"

function Resolve-UnityEditor {
    param([string]$RequestedPath)

    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        if (-not (Test-Path -LiteralPath $RequestedPath)) {
            throw "Unity executable not found: $RequestedPath"
        }

        return (Resolve-Path -LiteralPath $RequestedPath).Path
    }

    foreach ($environmentName in @("UNITY_EXE", "UNITY_PATH")) {
        $candidate = [Environment]::GetEnvironmentVariable($environmentName)
        if (-not [string]::IsNullOrWhiteSpace($candidate) -and (Test-Path -LiteralPath $candidate)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
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

    return $null
}

function Resolve-CSharpCompiler {
    param(
        [string]$RequestedCscPath,
        [string]$RequestedUnityPath
    )

    if (-not [string]::IsNullOrWhiteSpace($RequestedCscPath)) {
        if (-not (Test-Path -LiteralPath $RequestedCscPath)) {
            throw "C# compiler not found: $RequestedCscPath"
        }

        return [pscustomobject]@{
            FilePath = (Resolve-Path -LiteralPath $RequestedCscPath).Path
            PrefixArguments = @()
            DisplayName = (Resolve-Path -LiteralPath $RequestedCscPath).Path
        }
    }

    $environmentCsc = [Environment]::GetEnvironmentVariable("UNITY_CSC")
    if (-not [string]::IsNullOrWhiteSpace($environmentCsc) -and (Test-Path -LiteralPath $environmentCsc)) {
        return [pscustomobject]@{
            FilePath = (Resolve-Path -LiteralPath $environmentCsc).Path
            PrefixArguments = @()
            DisplayName = (Resolve-Path -LiteralPath $environmentCsc).Path
        }
    }

    $unityEditor = Resolve-UnityEditor -RequestedPath $RequestedUnityPath
    if ($unityEditor) {
        $unityEditorDirectory = Split-Path -Parent $unityEditor
        $dotnetPath = Join-Path $unityEditorDirectory "Data/NetCoreRuntime/dotnet.exe"
        $dotnetCscPath = Join-Path $unityEditorDirectory "Data/DotNetSdkRoslyn/csc.dll"
        if ((Test-Path -LiteralPath $dotnetPath) -and (Test-Path -LiteralPath $dotnetCscPath)) {
            return [pscustomobject]@{
                FilePath = (Resolve-Path -LiteralPath $dotnetPath).Path
                PrefixArguments = @((Resolve-Path -LiteralPath $dotnetCscPath).Path)
                DisplayName = "$((Resolve-Path -LiteralPath $dotnetPath).Path) $((Resolve-Path -LiteralPath $dotnetCscPath).Path)"
            }
        }

        $candidateNames = @(
            "Data/Tools/Roslyn/csc.exe",
            "Data/Tools/Roslyn/csc",
            "Data/MonoBleedingEdge/lib/mono/msbuild/Current/bin/Roslyn/csc.exe",
            "Data/MonoBleedingEdge/lib/mono/4.5/csc.exe"
        )

        foreach ($candidateName in $candidateNames) {
            $candidate = Join-Path $unityEditorDirectory $candidateName
            if (Test-Path -LiteralPath $candidate) {
                return [pscustomobject]@{
                    FilePath = (Resolve-Path -LiteralPath $candidate).Path
                    PrefixArguments = @()
                    DisplayName = (Resolve-Path -LiteralPath $candidate).Path
                }
            }
        }
    }

    throw "Unity Roslyn compiler not found. Pass -CscPath, pass -UnityPath, or set UNITY_CSC / UNITY_EXE."
}

function Get-BeeResponseFiles {
    param(
        [string]$ProjectRoot,
        [string[]]$AssemblyNames
    )

    $beeArtifactsRoot = Join-Path $ProjectRoot "Library/Bee/artifacts"
    if (-not (Test-Path -LiteralPath $beeArtifactsRoot)) {
        throw "Unity Bee artifacts were not found at $beeArtifactsRoot. Open the project in Unity or run a Unity batch import once before this check."
    }

    $responseFiles = @(Get-ChildItem -LiteralPath $beeArtifactsRoot -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.Extension -eq ".rsp" })
    if ($AssemblyNames -and $AssemblyNames.Count -gt 0) {
        $wanted = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach ($assembly in $AssemblyNames) {
            [void]$wanted.Add($assembly)
        }

        $responseFiles = @($responseFiles | Where-Object { $wanted.Contains($_.BaseName) })
    }

    return @($responseFiles | Sort-Object FullName)
}

function Convert-ToProjectRelativePath {
    param(
        [string]$ProjectRoot,
        [string]$Path
    )

    $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
    if ($resolvedPath.StartsWith($ProjectRoot, [StringComparison]::OrdinalIgnoreCase)) {
        return $resolvedPath.Substring($ProjectRoot.Length).TrimStart("\", "/") -replace "\\", "/"
    }

    return $resolvedPath
}

function Invoke-BeeCompile {
    param(
        [object]$Compiler,
        [System.IO.FileInfo]$ResponseFile,
        [string]$ProjectRoot
    )

    $arguments = @($Compiler.PrefixArguments) + @("@$($ResponseFile.FullName)")
    $secondaryResponseFile = "$($ResponseFile.FullName)2"
    if (Test-Path -LiteralPath $secondaryResponseFile) {
        $arguments += "@$secondaryResponseFile"
    }

    Write-Host "Compiling $($ResponseFile.BaseName) via $(Convert-ToProjectRelativePath -ProjectRoot $ProjectRoot -Path $ResponseFile.FullName)"

    Push-Location $ProjectRoot
    try {
        $output = & ([string]$Compiler.FilePath) @arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }

    return [pscustomobject]@{
        Assembly = $ResponseFile.BaseName
        ResponseFile = $ResponseFile.FullName
        ExitCode = $exitCode
        Output = @($output | ForEach-Object { $_.ToString() })
    }
}

$resolvedTargetRoot = (Resolve-Path -LiteralPath $TargetRoot).Path
$responseFiles = Get-BeeResponseFiles -ProjectRoot $resolvedTargetRoot -AssemblyNames $AssemblyName

Write-Host "EFrame Unity compile check"
Write-Host "Target root: $resolvedTargetRoot"
Write-Host "Response files: $($responseFiles.Count)"

if ($responseFiles.Count -eq 0) {
    $assemblyFilter = if ($AssemblyName -and $AssemblyName.Count -gt 0) { " matching: $($AssemblyName -join ', ')" } else { "" }
    throw "No Unity Bee compiler response files found$assemblyFilter."
}

foreach ($responseFile in $responseFiles) {
    $relativePath = Convert-ToProjectRelativePath -ProjectRoot $resolvedTargetRoot -Path $responseFile.FullName
    $secondaryResponseFile = "$($responseFile.FullName)2"
    $hasSecondary = Test-Path -LiteralPath $secondaryResponseFile
    Write-Host "  - $($responseFile.BaseName): $relativePath$(if ($hasSecondary) { ' + .rsp2' } else { '' })"
}

if ($ListOnly) {
    Write-Host "ListOnly was set; skipped compiler execution."
    return
}

$compiler = Resolve-CSharpCompiler -RequestedCscPath $CscPath -RequestedUnityPath $UnityPath
Write-Host "Compiler: $($compiler.DisplayName)"

$failures = New-Object System.Collections.Generic.List[object]
foreach ($responseFile in $responseFiles) {
    $result = Invoke-BeeCompile -Compiler $compiler -ResponseFile $responseFile -ProjectRoot $resolvedTargetRoot
    if ($result.ExitCode -ne 0) {
        $failures.Add($result)
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Host "Unity Bee compile failed for $($failure.Assembly) using $(Convert-ToProjectRelativePath -ProjectRoot $resolvedTargetRoot -Path $failure.ResponseFile)."
        $diagnostics = @($failure.Output | Where-Object { $_ -match "(error CS\d+|fatal error|warning CS\d+)" })
        if ($diagnostics.Count -eq 0) {
            $diagnostics = @($failure.Output | Select-Object -Last 40)
        }

        foreach ($line in $diagnostics) {
            Write-Host $line
        }
    }

    throw "EFrame Unity compile check failed."
}

Write-Host "EFrame Unity compile check passed."
