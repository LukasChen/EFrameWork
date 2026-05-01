param(
    [string]$TargetRoot = (Get-Location).Path,
    [string]$FrameworkRoot,
    [switch]$FailOnWarning
)

$resolvedTargetRoot = (Resolve-Path $TargetRoot).Path
$manifestRelativePath = ".github/eframe-ai.manifest.json"
$targetManifestPath = Join-Path $resolvedTargetRoot ($manifestRelativePath -replace "/", "\")

$errors = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

function Read-JsonFile {
    param(
        [string]$Path
    )

    if (-not (Test-Path $Path)) {
        return $null
    }

    try {
        return Get-Content -Path $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        return $null
    }
}

function Get-FileSha256 {
    param(
        [string]$Path
    )

    if (-not (Test-Path $Path)) {
        return $null
    }

    return (Get-FileHash -Path $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Normalize-ManagedBlockText {
    param(
        [string]$Text
    )

    if ($null -eq $Text) {
        $Text = ""
    }

    $normalized = $Text -replace "`r`n", "`n"
    $normalized = $normalized -replace "`r", "`n"
    return $normalized.TrimEnd() + "`n"
}

function Get-TextSha256 {
    param(
        [string]$Text
    )

    $normalized = Normalize-ManagedBlockText -Text $Text
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($normalized)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hashBytes = $sha.ComputeHash($bytes)
        return ([System.BitConverter]::ToString($hashBytes) -replace "-", "").ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
    }
}

function Get-ManagedBlockBody {
    param(
        [string]$Text,
        [string]$BlockId
    )

    if ($null -eq $Text) {
        return $null
    }

    $beginMarker = "<!-- BEGIN EFRAMEWORK AI MANAGED BLOCK: $BlockId -->"
    $endMarker = "<!-- END EFRAMEWORK AI MANAGED BLOCK: $BlockId -->"
    $beginIndex = $Text.IndexOf($beginMarker, [System.StringComparison]::Ordinal)
    if ($beginIndex -lt 0) {
        return $null
    }

    $contentStart = $beginIndex + $beginMarker.Length
    if ($contentStart -lt $Text.Length -and $Text[$contentStart] -eq "`r") {
        $contentStart++
    }
    if ($contentStart -lt $Text.Length -and $Text[$contentStart] -eq "`n") {
        $contentStart++
    }

    $endIndex = $Text.IndexOf($endMarker, $contentStart, [System.StringComparison]::Ordinal)
    if ($endIndex -lt 0) {
        return $null
    }

    return $Text.Substring($contentStart, $endIndex - $contentStart)
}

function Get-RelativeManagedNames {
    param(
        [string]$DirectoryPath,
        [string]$Pattern
    )

    if (-not (Test-Path $DirectoryPath)) {
        return @()
    }

    return @(Get-ChildItem -Path $DirectoryPath | Where-Object { $_.Name -like $Pattern } | Select-Object -ExpandProperty Name | Sort-Object)
}

Write-Host "EFrame AI project health check"
Write-Host "Target root: $resolvedTargetRoot"

$targetManifest = Read-JsonFile -Path $targetManifestPath
$targetClients = @("codex", "copilot", "claude-code")
if (-not $targetManifest) {
    $errors.Add("Target project is missing a readable $manifestRelativePath. Run Initialize-EFrameAI.ps1 or the project updater with -Force.")
}
else {
    Write-Host "Target AI manifest version: $($targetManifest.version)"
    if ($targetManifest.clients -and $targetManifest.clients.Count -gt 0) {
        $targetClients = @($targetManifest.clients | ForEach-Object { [string]$_ })
        Write-Host "Target AI clients: $($targetClients -join ', ')"
    }

    if (-not $targetManifest.files -or $targetManifest.files.Count -eq 0) {
        $warnings.Add("Target manifest has no file hash entries. Re-sync from a newer EFrameWork framework checkout.")
    }
    else {
        foreach ($entry in $targetManifest.files) {
            $relativePath = [string]$entry.path
            $expectedHash = ([string]$entry.sha256).ToLowerInvariant()
            $targetPath = Join-Path $resolvedTargetRoot ($relativePath -replace "/", "\")
            $kind = [string]$entry.kind

            if (-not (Test-Path $targetPath)) {
                $errors.Add("Manifest-tracked file is missing: $relativePath")
                continue
            }

            if ($kind -eq "managedBlock") {
                $blockBody = Get-ManagedBlockBody -Text (Get-Content -Path $targetPath -Raw -Encoding UTF8) -BlockId ([string]$entry.blockId)
                if ($null -eq $blockBody) {
                    $errors.Add("Manifest-tracked managed block is missing: $relativePath [$($entry.blockId)]")
                    continue
                }

                $actualHash = Get-TextSha256 -Text $blockBody
            }
            else {
                $actualHash = Get-FileSha256 -Path $targetPath
            }

            if ($actualHash -ne $expectedHash) {
                $errors.Add("Manifest-tracked item differs from synced framework version: $relativePath")
            }
        }
    }
}

if ($FrameworkRoot) {
    $resolvedFrameworkRoot = (Resolve-Path $FrameworkRoot).Path
    $frameworkManifestPath = Join-Path $resolvedFrameworkRoot ($manifestRelativePath -replace "/", "\")
    $frameworkManifest = Read-JsonFile -Path $frameworkManifestPath

    Write-Host "Framework root: $resolvedFrameworkRoot"
    if (-not $frameworkManifest) {
        $warnings.Add("Framework manifest is not readable at $frameworkManifestPath; skipped version comparison.")
    }
    elseif ($targetManifest -and $frameworkManifest.version -ne $targetManifest.version) {
        $warnings.Add("Target AI manifest version '$($targetManifest.version)' differs from framework version '$($frameworkManifest.version)'. Run the project updater with -StatusOnly / -Force.")
    }
}

$instructionsDirectory = Join-Path $resolvedTargetRoot ".github\instructions"
$skillsDirectory = Join-Path $resolvedTargetRoot ".github\skills"
$eframeInstructions = Get-RelativeManagedNames -DirectoryPath $instructionsDirectory -Pattern "eframe-*"
$eframeSkills = Get-RelativeManagedNames -DirectoryPath $skillsDirectory -Pattern "eframe-*"

if ($targetClients -contains "codex" -and -not (Test-Path (Join-Path $resolvedTargetRoot "AGENTS.md"))) {
    $errors.Add("Codex client is selected, but AGENTS.md is missing.")
}

if ($targetClients -contains "claude-code" -and -not (Test-Path (Join-Path $resolvedTargetRoot "CLAUDE.md"))) {
    $errors.Add("Claude Code client is selected, but CLAUDE.md is missing.")
}

if ($targetClients -contains "copilot") {
    if ($eframeInstructions.Count -eq 0) {
        $errors.Add("Copilot client is selected, but no synced eframe-* instruction files were found.")
    }
    if ($eframeSkills.Count -eq 0) {
        $errors.Add("Copilot client is selected, but no synced eframe-* skills were found.")
    }
}

$runtimeSearchRoots = @(
    (Join-Path $resolvedTargetRoot "Assets\App\Runtime"),
    (Join-Path $resolvedTargetRoot "Assets\Modules"),
    (Join-Path $resolvedTargetRoot "Assets\Scripts")
) | Where-Object { Test-Path $_ }

foreach ($root in $runtimeSearchRoots) {
    $runtimeFiles = @(Get-ChildItem -Path $root -Recurse -Filter "*.cs" -File -ErrorAction SilentlyContinue)

    $waitForCompletionHits = @($runtimeFiles | Select-String -Pattern "WaitForCompletion\s*\(" -ErrorAction SilentlyContinue)
    foreach ($hit in $waitForCompletionHits) {
        $relativePath = $hit.Path.Substring($resolvedTargetRoot.Length).TrimStart("\") -replace "\\", "/"
        $warnings.Add("Synchronous Addressables wait found at ${relativePath}:$($hit.LineNumber). Confirm it is diagnostic-only or explicitly justified.")
    }

    $manualCanvasHits = @($runtimeFiles | Select-String -Pattern "new\s+GameObject\s*\(.*Canvas|AddComponent\s*<\s*Canvas\s*>" -ErrorAction SilentlyContinue)
    foreach ($hit in $manualCanvasHits) {
        $relativePath = $hit.Path.Substring($resolvedTargetRoot.Length).TrimStart("\") -replace "\\", "/"
        $warnings.Add("Manual runtime Canvas creation found at ${relativePath}:$($hit.LineNumber). EFrame UI should normally go through QUI/UIController.")
    }
}

Write-Host "Framework instructions: $($eframeInstructions.Count)"
Write-Host "Framework skills: $($eframeSkills.Count)"

foreach ($warning in $warnings) {
    Write-Warning $warning
}

foreach ($errorMessage in $errors) {
    Write-Error $errorMessage
}

if ($errors.Count -gt 0 -or ($FailOnWarning -and $warnings.Count -gt 0)) {
    throw "EFrame AI project health check failed."
}

Write-Host "EFrame AI project health check passed."
