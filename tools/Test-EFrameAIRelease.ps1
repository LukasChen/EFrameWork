param(
    [switch]$FailOnWarning
)

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$frameworkRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path
$manifestPath = ".github/eframe-ai.manifest.json"

function Get-ManifestVersion {
    param(
        [string]$JsonText
    )

    if ([string]::IsNullOrWhiteSpace($JsonText)) {
        return $null
    }

    try {
        return ($JsonText | ConvertFrom-Json).version
    }
    catch {
        return $null
    }
}

function Convert-ToVersion {
    param(
        [string]$VersionText
    )

    if ([string]::IsNullOrWhiteSpace($VersionText)) {
        return $null
    }

    $stablePart = ($VersionText -split "-", 2)[0]
    try {
        return [version]$stablePart
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

function Convert-ToRepoRelativePath {
    param(
        [string]$Path
    )

    $fullPath = (Resolve-Path $Path).Path
    return $fullPath.Substring($frameworkRoot.Length + 1).Replace("\", "/")
}

function Get-ExpectedManifestFilePaths {
    $paths = New-Object System.Collections.Generic.List[string]

    foreach ($rootFile in @(
        "AGENTS.md",
        "EFRAME_AI_API_INDEX.md",
        "EFRAME_AI_ARCHITECTURE.md",
        "EFRAME_AI_SETUP.md",
        "EFRAME_AI_RELEASE_CHECKLIST.md"
    )) {
        if (Test-Path $rootFile) {
            $paths.Add($rootFile)
        }
    }

    if (Test-Path ".github/copilot-instructions.md") {
        $paths.Add(".github/copilot-instructions.md")
    }

    if (Test-Path ".github/instructions") {
        foreach ($file in Get-ChildItem -Path ".github/instructions" -Filter "eframe-*.instructions.md" -File) {
            $paths.Add((Convert-ToRepoRelativePath -Path $file.FullName))
        }
    }

    if (Test-Path ".github/skills") {
        foreach ($skillDirectory in Get-ChildItem -Path ".github/skills" -Directory -Filter "eframe-*") {
            foreach ($file in Get-ChildItem -Path $skillDirectory.FullName -Recurse -File) {
                $paths.Add((Convert-ToRepoRelativePath -Path $file.FullName))
            }
        }
    }

    return @($paths | Sort-Object -Unique)
}

function Test-FrontMatterFile {
    param(
        [string]$Path,
        [bool]$RequireApplyTo
    )

    $issues = New-Object System.Collections.Generic.List[string]
    $lines = @(Get-Content -LiteralPath $Path -Encoding UTF8)
    if ($lines.Count -lt 3 -or $lines[0] -ne "---") {
        $issues.Add("$Path must start with YAML frontmatter.")
        return $issues
    }

    $closingIndex = -1
    for ($index = 1; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -eq "---") {
            $closingIndex = $index
            break
        }
    }

    if ($closingIndex -lt 0) {
        $issues.Add("$Path has no closing YAML frontmatter marker.")
        return $issues
    }

    $fieldNames = New-Object System.Collections.Generic.HashSet[string]
    for ($index = 1; $index -lt $closingIndex; $index++) {
        $line = $lines[$index]
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        if ($line -match "^\s+-\s+") {
            continue
        }

        if ($line -notmatch "^([A-Za-z][A-Za-z0-9_-]*):(\s.*)?$") {
            $issues.Add("$Path has a non-YAML frontmatter line: $line")
            continue
        }

        [void]$fieldNames.Add($matches[1])
    }

    foreach ($requiredField in @("name", "description")) {
        if (-not $fieldNames.Contains($requiredField)) {
            $issues.Add("$Path frontmatter is missing required field '$requiredField'.")
        }
    }

    if ($RequireApplyTo -and -not $fieldNames.Contains("applyTo")) {
        $issues.Add("$Path frontmatter is missing required field 'applyTo'.")
    }

    return $issues
}

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
        "^AGENTS\.md$",
        "^\.github/copilot-instructions\.md$",
        "^\.github/instructions/eframe-.*\.instructions\.md$",
        "^\.github/instructions/maintainer-.*\.instructions\.md$",
        "^\.github/skills/eframe-.*",
        "^\.github/skills/maintainer-.*",
        "^tools/Initialize-EFrameAI\.ps1$",
        "^tools/New-EFrameProjectAIOverlay\.ps1$",
        "^tools/Install-EFrameAIProjectUpdater\.ps1$",
        "^tools/Initialize-EFrameColdStart\.ps1$",
        "^tools/Initialize-EFrameBootstrapCode\.ps1$",
        "^tools/Test-EFrameAIProject\.ps1$",
        "^tools/Test-EFrameAIRelease\.ps1$",
        "^packages/com\.eframework\.core/Editor/EFrameProjectInitializationWindow\.cs$",
        "^README\.md$",
        "^EFRAME_AI_API_INDEX\.md$",
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

    if ($manifestChanged) {
        $currentManifestText = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8
        $currentManifestVersion = Get-ManifestVersion -JsonText $currentManifestText
        $headManifestText = (& git show "HEAD:$manifestPath" 2>$null)
        $headManifestVersion = Get-ManifestVersion -JsonText ($headManifestText -join "`n")

        if (-not $currentManifestVersion) {
            $errors.Add("$manifestPath does not contain a readable version.")
        }
        elseif ($headManifestVersion) {
            if ($currentManifestVersion -eq $headManifestVersion) {
                $errors.Add("$manifestPath changed, but version is still $currentManifestVersion.")
            }
            else {
                $currentComparableVersion = Convert-ToVersion -VersionText $currentManifestVersion
                $headComparableVersion = Convert-ToVersion -VersionText $headManifestVersion
                if ($currentComparableVersion -and $headComparableVersion -and $currentComparableVersion -le $headComparableVersion) {
                    $errors.Add("$manifestPath version must increase from $headManifestVersion to a newer value; current is $currentManifestVersion.")
                }
            }
        }
    }

    $manifestObject = $null
    try {
        $manifestObject = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        $errors.Add("$manifestPath is not valid JSON.")
    }

    if ($manifestObject) {
        $expectedManifestFilePaths = Get-ExpectedManifestFilePaths
        $manifestFileEntries = @($manifestObject.files)

        if ($manifestFileEntries.Count -eq 0) {
            $errors.Add("$manifestPath must contain file hash entries for synced AI files.")
        }
        else {
            $manifestPaths = @($manifestFileEntries | ForEach-Object { [string]$_.path } | Sort-Object -Unique)
            $missingManifestPaths = @($expectedManifestFilePaths | Where-Object { $_ -notin $manifestPaths })
            $extraManifestPaths = @($manifestPaths | Where-Object { $_ -notin $expectedManifestFilePaths })

            if ($missingManifestPaths.Count -gt 0) {
                $errors.Add("$manifestPath is missing synced file entries: $($missingManifestPaths -join ', ')")
            }

            if ($extraManifestPaths.Count -gt 0) {
                $errors.Add("$manifestPath contains non-synced file entries: $($extraManifestPaths -join ', ')")
            }

            foreach ($entry in $manifestFileEntries) {
                $entryPath = [string]$entry.path
                $entryHash = [string]$entry.sha256

                if ([string]::IsNullOrWhiteSpace($entryPath) -or [string]::IsNullOrWhiteSpace($entryHash)) {
                    $errors.Add("$manifestPath contains a file entry without path or sha256.")
                    continue
                }

                if ($entryPath -match "\\") {
                    $errors.Add("$manifestPath file entry must use '/' separators: $entryPath")
                    continue
                }

                if ($entryPath -match "maintainer-|project-") {
                    $errors.Add("$manifestPath must not track maintainer-only or project-owned files: $entryPath")
                    continue
                }

                if (-not (Test-Path $entryPath)) {
                    $errors.Add("$manifestPath tracks a missing file: $entryPath")
                    continue
                }

                $actualHash = Get-FileSha256 -Path $entryPath
                if ($actualHash -ne $entryHash.ToLowerInvariant()) {
                    $errors.Add("$manifestPath hash mismatch for $entryPath.")
                }
            }
        }
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
    $maintainerInstructions = @(Get-ChildItem -Path ".github/instructions" -Filter "maintainer-*.instructions.md" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name)
    $maintainerSkills = @(Get-ChildItem -Path ".github/skills" -Directory -Filter "maintainer-*" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name)
    if ($eframeInstructions.Count -eq 0) {
        $warnings.Add("No framework-managed eframe instruction files were found.")
    }
    if ($eframeSkills.Count -eq 0) {
        $warnings.Add("No framework-managed eframe skills were found.")
    }

    $instructionFiles = @(Get-ChildItem -Path ".github/instructions" -Filter "*.instructions.md" -ErrorAction SilentlyContinue)
    foreach ($instructionFile in $instructionFiles) {
        foreach ($issue in (Test-FrontMatterFile -Path $instructionFile.FullName -RequireApplyTo $true)) {
            $errors.Add($issue)
        }
    }

    $skillFiles = @(Get-ChildItem -Path ".github/skills" -Directory -ErrorAction SilentlyContinue | ForEach-Object {
        Join-Path $_.FullName "SKILL.md"
    } | Where-Object { Test-Path -LiteralPath $_ })
    foreach ($skillFile in $skillFiles) {
        foreach ($issue in (Test-FrontMatterFile -Path $skillFile -RequireApplyTo $false)) {
            $errors.Add($issue)
        }
    }

    foreach ($eframeSkillName in $eframeSkills) {
        $skillPath = Join-Path ".github/skills" (Join-Path $eframeSkillName "SKILL.md")
        $skillText = Get-Content -LiteralPath $skillPath -Raw -Encoding UTF8
        foreach ($requiredMetadataField in @("capabilities", "owns", "outputs", "forbiddenPatterns")) {
            if ($skillText -notmatch "(?m)^$requiredMetadataField\s*:") {
                $errors.Add("$skillPath frontmatter is missing machine-readable '$requiredMetadataField' metadata.")
            }
        }
    }

    foreach ($instructionName in $eframeInstructions) {
        $instructionPath = Join-Path ".github/instructions" $instructionName
        $nonEmptyLineCount = @((Get-Content -LiteralPath $instructionPath -Encoding UTF8) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
        if ($nonEmptyLineCount -gt 90) {
            $warnings.Add("$instructionName has $nonEmptyLineCount non-empty lines. Keep synced instructions short; move workflow details to skills.")
        }
    }

    $syncScriptText = Get-Content -LiteralPath "tools/Initialize-EFrameAI.ps1" -Raw -Encoding UTF8
    if ($syncScriptText -match "ManagedPattern\s+`"maintainer-\*`"" -or $syncScriptText -match "ManagedPattern\s+'maintainer-\*'") {
        $errors.Add("Initialize-EFrameAI.ps1 must not sync maintainer-* instructions or skills.")
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
    Write-Host "Maintainer instructions: $($maintainerInstructions.Count)"
    Write-Host "Maintainer skills: $($maintainerSkills.Count)"

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
