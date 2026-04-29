param(
    [switch]$FailOnWarning
)

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$frameworkRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path
$manifestPath = ".github/eframe-ai.manifest.json"

Push-Location $frameworkRoot
try {
    $gitRoot = (& git rev-parse --show-toplevel 2>$null)
    if (-not $gitRoot) {
        throw "This check must run inside a git worktree."
    }

    $changedPaths = @()
    $statusLines = @(& git status --porcelain --untracked-files=all)
    foreach ($line in $statusLines) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.Length -lt 4) {
            continue
        }

        $path = $line.Substring(3).Trim()
        if ($path -match " -> ") {
            $path = ($path -split " -> ")[-1]
        }

        $changedPaths += ($path -replace "\\", "/")
    }

    $aiImpactPatterns = @(
        "^\.github/copilot-instructions\.md$",
        "^\.github/instructions/eframe-.*\.instructions\.md$",
        "^\.github/skills/eframe-.*",
        "^tools/Initialize-EFrameAI\.ps1$",
        "^tools/New-EFrameProjectAIOverlay\.ps1$",
        "^tools/Install-EFrameAIProjectUpdater\.ps1$",
        "^tools/Initialize-EFrameColdStart\.ps1$",
        "^tools/Initialize-EFrameBootstrapCode\.ps1$",
        "^tools/Test-EFrameAIRelease\.ps1$",
        "^packages/com\.eframework\.core/Editor/EFrameProjectInitializationWindow\.cs$",
        "^README\.md$",
        "^EFRAME_AI_ARCHITECTURE\.md$",
        "^EFRAME_AI_SETUP\.md$",
        "^EFRAME_AI_RELEASE_CHECKLIST\.md$",
        "^UNITY_DIRECTORY_STRUCTURE\.md$",
        "^RESPATH_CONVENTION\.md$"
    )

    $aiImpactChanges = @($changedPaths | Where-Object {
        $path = $_
        $aiImpactPatterns | Where-Object { $path -match $_ } | Select-Object -First 1
    } | Sort-Object -Unique)

    $manifestChanged = $changedPaths -contains $manifestPath

    $errors = New-Object System.Collections.Generic.List[string]
    $warnings = New-Object System.Collections.Generic.List[string]

    if ($aiImpactChanges.Count -gt 0 -and -not $manifestChanged) {
        $errors.Add("AI-impacting files changed, but $manifestPath is not changed. Bump the manifest version.")
    }

    $frameworkProjectInstructions = @(Get-ChildItem -Path ".github/instructions" -Filter "project-*.instructions.md" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name)
    if ($frameworkProjectInstructions.Count -gt 0) {
        $errors.Add("Framework repo contains project-owned instruction overlays: $($frameworkProjectInstructions -join ', ')")
    }

    $frameworkProjectSkills = @(Get-ChildItem -Path ".github/skills" -Directory -Filter "project-*" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name)
    if ($frameworkProjectSkills.Count -gt 0) {
        $errors.Add("Framework repo contains project-owned skills: $($frameworkProjectSkills -join ', ')")
    }

    $eframeInstructions = @(Get-ChildItem -Path ".github/instructions" -Filter "eframe-*.instructions.md" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name)
    $eframeSkills = @(Get-ChildItem -Path ".github/skills" -Directory -Filter "eframe-*" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name)
    if ($eframeInstructions.Count -eq 0) {
        $warnings.Add("No framework-managed eframe instruction files were found.")
    }
    if ($eframeSkills.Count -eq 0) {
        $warnings.Add("No framework-managed eframe skills were found.")
    }

    Write-Host "EFrame AI release check"
    Write-Host "Framework root: $frameworkRoot"
    Write-Host "Changed AI-impacting files: $($aiImpactChanges.Count)"
    foreach ($path in $aiImpactChanges) {
        Write-Host "  - $path"
    }
    Write-Host "Manifest changed: $manifestChanged"
    Write-Host "Framework instructions: $($eframeInstructions.Count)"
    Write-Host "Framework skills: $($eframeSkills.Count)"

    foreach ($warning in $warnings) {
        Write-Warning $warning
    }

    foreach ($errorMessage in $errors) {
        Write-Error $errorMessage
    }

    if ($errors.Count -gt 0 -or ($FailOnWarning -and $warnings.Count -gt 0)) {
        throw "EFrame AI release check failed."
    }

    Write-Host "EFrame AI release check passed."
}
finally {
    Pop-Location
}