param(
    [string]$SourceRoot,
    [string]$TargetRoot,
    [switch]$CheckOnly
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$frameworkRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path

if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $SourceRoot = Join-Path $frameworkRoot "test-fixtures/EFrameBasicTemplate/Assets"
}

if ([string]::IsNullOrWhiteSpace($TargetRoot)) {
    $TargetRoot = Join-Path $frameworkRoot "packages/com.eframework.core/Editor/Templates/Basic/Assets"
}

if (-not (Test-Path -LiteralPath $SourceRoot)) {
    throw "Basic source template not found: $SourceRoot"
}

$sourceFullPath = (Resolve-Path -LiteralPath $SourceRoot).Path
$targetFullPath = if (Test-Path -LiteralPath $TargetRoot) {
    (Resolve-Path -LiteralPath $TargetRoot).Path
}
else {
    [System.IO.Path]::GetFullPath($TargetRoot)
}

if (-not $sourceFullPath.StartsWith($frameworkRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Source must stay inside the framework workspace: $sourceFullPath"
}

if (-not $targetFullPath.StartsWith($frameworkRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Target must stay inside the framework workspace: $targetFullPath"
}

$excludedDirectoryNames = @(
    "Library",
    "Temp",
    "Logs",
    "UserSettings",
    "Obj",
    "obj",
    "bin",
    "TestResults"
)

$excludedFileExtensions = @(
    ".csproj",
    ".sln",
    ".slnx",
    ".user",
    ".pidb",
    ".booproj"
)

function Convert-ToRepoRelativePath {
    param(
        [string]$FullPath,
        [string]$RootPath
    )

    return $FullPath.Substring($RootPath.Length + 1).Replace("\", "/")
}

function Test-SkippedPath {
    param(
        [string]$RelativePath
    )

    $segments = $RelativePath -split "/"
    foreach ($segment in $segments) {
        if ($segment -in $excludedDirectoryNames) {
            return $true
        }
    }

    $extension = [System.IO.Path]::GetExtension($RelativePath)
    return $extension -in $excludedFileExtensions
}

function Convert-ToTemplateRelativePath {
    param(
        [string]$RelativePath
    )

    if ($RelativePath.EndsWith(".cs.meta", [System.StringComparison]::OrdinalIgnoreCase)) {
        return $RelativePath.Substring(0, $RelativePath.Length - ".meta".Length) + ".txt.meta"
    }

    if ($RelativePath.EndsWith(".cs", [System.StringComparison]::OrdinalIgnoreCase)) {
        return "$RelativePath.txt"
    }

    return $RelativePath
}

function Get-SourceEntries {
    $entries = New-Object System.Collections.Generic.List[object]
    $sourceFiles = Get-ChildItem -LiteralPath $sourceFullPath -Recurse -File

    foreach ($sourceFile in $sourceFiles) {
        $relativePath = Convert-ToRepoRelativePath -FullPath $sourceFile.FullName -RootPath $sourceFullPath
        if (Test-SkippedPath -RelativePath $relativePath) {
            continue
        }

        $templateRelativePath = Convert-ToTemplateRelativePath -RelativePath $relativePath
        $entries.Add([pscustomobject]@{
            SourcePath = $sourceFile.FullName
            SourceRelativePath = $relativePath
            TemplateRelativePath = $templateRelativePath
        })
    }

    return $entries
}

function Get-FileHashText {
    param(
        [string]$Path
    )

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

$entries = @(Get-SourceEntries)
$sourceRootMetaPath = "$sourceFullPath.meta"
$targetRootMetaPath = "$targetFullPath.meta"

if ($CheckOnly) {
    $errors = New-Object System.Collections.Generic.List[string]
    $expectedPaths = @($entries | ForEach-Object { $_.TemplateRelativePath } | Sort-Object -Unique)

    if (-not (Test-Path -LiteralPath $targetFullPath)) {
        throw "Basic template target not found: $targetFullPath"
    }

    $actualPaths = @(Get-ChildItem -LiteralPath $targetFullPath -Recurse -File |
        ForEach-Object { Convert-ToRepoRelativePath -FullPath $_.FullName -RootPath $targetFullPath } |
        Sort-Object -Unique)

    foreach ($missing in @($expectedPaths | Where-Object { $_ -notin $actualPaths })) {
        $errors.Add("Missing template file: $missing")
    }

    foreach ($extra in @($actualPaths | Where-Object { $_ -notin $expectedPaths })) {
        $errors.Add("Extra template file: $extra")
    }

    foreach ($entry in $entries) {
        $targetFile = Join-Path $targetFullPath ($entry.TemplateRelativePath -replace "/", [System.IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path -LiteralPath $targetFile)) {
            continue
        }

        $sourceHash = Get-FileHashText -Path $entry.SourcePath
        $targetHash = Get-FileHashText -Path $targetFile
        if ($sourceHash -ne $targetHash) {
            $errors.Add("Template file differs: $($entry.TemplateRelativePath)")
        }
    }

    if (Test-Path -LiteralPath $sourceRootMetaPath) {
        if (-not (Test-Path -LiteralPath $targetRootMetaPath)) {
            $errors.Add("Missing template root meta file: $targetRootMetaPath")
        }
        elseif ((Get-FileHashText -Path $sourceRootMetaPath) -ne (Get-FileHashText -Path $targetRootMetaPath)) {
            $errors.Add("Template root meta file differs: $targetRootMetaPath")
        }
    }

    if ($errors.Count -gt 0) {
        foreach ($errorMessage in $errors) {
            Write-Error $errorMessage
        }

        throw "Basic template is out of sync with the fixture source."
    }

    Write-Host "Basic template is in sync with the fixture source."
    return
}

if (Test-Path -LiteralPath $targetFullPath) {
    Remove-Item -LiteralPath $targetFullPath -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $targetFullPath | Out-Null

if (Test-Path -LiteralPath $sourceRootMetaPath) {
    Copy-Item -LiteralPath $sourceRootMetaPath -Destination $targetRootMetaPath
}

foreach ($entry in $entries) {
    $targetFile = Join-Path $targetFullPath ($entry.TemplateRelativePath -replace "/", [System.IO.Path]::DirectorySeparatorChar)
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $targetFile) | Out-Null
    Copy-Item -LiteralPath $entry.SourcePath -Destination $targetFile
}

Write-Host "Synced Basic template from:"
Write-Host "  $sourceFullPath"
Write-Host "to:"
Write-Host "  $targetFullPath"
