param(
    [string]$TargetRoot,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

if (-not $TargetRoot) {
    throw "TargetRoot is required."
}

if (-not (Test-Path $TargetRoot)) {
    throw "TargetRoot does not exist: $TargetRoot"
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$frameworkRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path
$resolvedTargetRoot = (Resolve-Path $TargetRoot).Path

$templateCandidates = @(
    (Join-Path $frameworkRoot "Editor/Templates/Basic/Assets"),
    (Join-Path $frameworkRoot "packages/com.eframework.core/Editor/Templates/Basic/Assets")
)

$templateRoot = $templateCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $templateRoot) {
    throw "Basic template source was not found under the EFrame package."
}

$templateRoot = (Resolve-Path -LiteralPath $templateRoot).Path
$targetAssetsRoot = Join-Path $resolvedTargetRoot "Assets"

function Convert-TemplateFileName {
    param(
        [string]$FileName
    )

    if ($FileName.EndsWith(".cs.txt.meta", [System.StringComparison]::OrdinalIgnoreCase)) {
        return $FileName.Substring(0, $FileName.Length - ".txt.meta".Length) + ".meta"
    }

    if ($FileName.EndsWith(".cs.txt", [System.StringComparison]::OrdinalIgnoreCase)) {
        return $FileName.Substring(0, $FileName.Length - ".txt".Length)
    }

    return $FileName
}

function Copy-BasicTemplateDirectory {
    param(
        [string]$SourceDirectory,
        [string]$TargetDirectory
    )

    if (-not (Test-Path -LiteralPath $TargetDirectory)) {
        New-Item -ItemType Directory -Path $TargetDirectory -Force | Out-Null
    }

    foreach ($sourceFile in Get-ChildItem -LiteralPath $SourceDirectory -File) {
        $targetFileName = Convert-TemplateFileName -FileName $sourceFile.Name
        $targetFile = Join-Path $TargetDirectory $targetFileName

        if ((Test-Path -LiteralPath $targetFile) -and -not $Force) {
            Write-Warning "Skip existing file: $targetFile (use -Force to overwrite)"
            continue
        }

        Copy-Item -LiteralPath $sourceFile.FullName -Destination $targetFile -Force:$Force
        Write-Host "Copied template file $targetFile"
    }

    foreach ($sourceChildDirectory in Get-ChildItem -LiteralPath $SourceDirectory -Directory) {
        Copy-BasicTemplateDirectory `
            -SourceDirectory $sourceChildDirectory.FullName `
            -TargetDirectory (Join-Path $TargetDirectory $sourceChildDirectory.Name)
    }
}

Copy-BasicTemplateDirectory -SourceDirectory $templateRoot -TargetDirectory $targetAssetsRoot

Write-Host "EFrame Basic template copy complete."
Write-Host "Template root: $templateRoot"
Write-Host "Target root: $targetAssetsRoot"
