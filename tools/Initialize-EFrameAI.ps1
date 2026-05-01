param(
    [string]$TargetRoot = (Get-Location).Path,
    [switch]$Force,
    [switch]$StatusOnly
)

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$frameworkRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path
$resolvedTargetRoot = (Resolve-Path $TargetRoot).Path
$sourceRoot = Join-Path $frameworkRoot ".github"
$destinationRoot = Join-Path $resolvedTargetRoot ".github"
$manifestName = "eframe-ai.manifest.json"
$sourceManifestPath = Join-Path $sourceRoot $manifestName
$destinationManifestPath = Join-Path $destinationRoot $manifestName
$aiDocumentNames = @(
    "AGENTS.md",
    "EFRAME_AI_API_INDEX.md",
    "EFRAME_AI_ARCHITECTURE.md",
    "EFRAME_AI_SETUP.md",
    "EFRAME_AI_RELEASE_CHECKLIST.md"
)

if (-not (Test-Path $sourceRoot)) {
    throw "Source .github folder not found: $sourceRoot"
}

if (-not (Test-Path $sourceManifestPath)) {
    throw "AI manifest not found: $sourceManifestPath"
}

function Read-ManifestVersion {
    param(
        [string]$ManifestPath
    )

    if (-not (Test-Path $ManifestPath)) {
        return $null
    }

    $manifest = Get-Content -Path $ManifestPath -Raw | ConvertFrom-Json
    return $manifest.version
}

function Get-ManifestFileEntries {
    param(
        [string]$ManifestPath
    )

    if (-not (Test-Path $ManifestPath)) {
        return @()
    }

    $manifest = Get-Content -Path $ManifestPath -Raw | ConvertFrom-Json
    if (-not $manifest.files) {
        return @()
    }

    return @($manifest.files)
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

function Get-ManifestFileStatus {
    param(
        [string]$RootPath,
        [object[]]$Entries
    )

    $results = New-Object System.Collections.Generic.List[object]
    foreach ($entry in $Entries) {
        $relativePath = [string]$entry.path
        $expectedHash = ([string]$entry.sha256).ToLowerInvariant()
        $absolutePath = Join-Path $RootPath ($relativePath -replace "/", "\")

        if (-not (Test-Path $absolutePath)) {
            $results.Add([pscustomobject]@{
                Path = $relativePath
                Status = "Missing"
                Expected = $expectedHash
                Actual = $null
            })
            continue
        }

        $actualHash = Get-FileSha256 -Path $absolutePath
        $status = if ($actualHash -eq $expectedHash) { "OK" } else { "Mismatch" }
        $results.Add([pscustomobject]@{
            Path = $relativePath
            Status = $status
            Expected = $expectedHash
            Actual = $actualHash
        })
    }

    return $results.ToArray()
}

function Write-ManifestFileIntegrityReport {
    param(
        [string]$RootPath,
        [object[]]$Entries
    )

    Write-Host "Framework-managed file integrity:"
    if (-not $Entries -or $Entries.Count -eq 0) {
        Write-Warning "  Manifest has no file hash entries; version-only sync checks are less precise."
        return @()
    }

    $statuses = Get-ManifestFileStatus -RootPath $RootPath -Entries $Entries
    $problems = @($statuses | Where-Object { $_.Status -ne "OK" })

    if ($problems.Count -eq 0) {
        Write-Host "  All manifest-tracked files match the framework manifest."
        return @()
    }

    foreach ($problem in $problems) {
        Write-Warning "  $($problem.Status): $($problem.Path)"
    }

    return $problems
}

function Sync-File {
    param(
        [string]$SourcePath,
        [string]$DestinationPath,
        [switch]$Overwrite
    )

    if (-not (Test-Path $SourcePath)) {
        return
    }

    if ((Test-Path $DestinationPath) -and -not $Overwrite) {
        Write-Warning "Skip existing file: $DestinationPath (use -Force to overwrite)"
        return
    }

    $destinationParent = Split-Path -Parent $DestinationPath
    if (-not (Test-Path $destinationParent)) {
        New-Item -ItemType Directory -Path $destinationParent | Out-Null
    }

    Copy-Item -Path $SourcePath -Destination $DestinationPath -Force
    Write-Host "Synced file $DestinationPath"
}

function Sync-ManagedDirectoryItems {
    param(
        [string]$SourceDirectory,
        [string]$DestinationDirectory,
        [string]$ManagedPattern,
        [switch]$Overwrite
    )

    if (-not (Test-Path $SourceDirectory)) {
        return
    }

    if (-not (Test-Path $DestinationDirectory)) {
        New-Item -ItemType Directory -Path $DestinationDirectory | Out-Null
    }

    $sourceItems = Get-ChildItem -Path $SourceDirectory | Where-Object { $_.Name -like $ManagedPattern }
    $sourceItemNames = $sourceItems.Name

    if ($Overwrite) {
        $staleItems = Get-ChildItem -Path $DestinationDirectory | Where-Object {
            $_.Name -like $ManagedPattern -and $_.Name -notin $sourceItemNames
        }

        foreach ($staleItem in $staleItems) {
            Remove-Item -Path $staleItem.FullName -Recurse -Force
            Write-Host "Removed stale managed item $($staleItem.FullName)"
        }
    }

    foreach ($sourceItem in $sourceItems) {
        $destinationPath = Join-Path $DestinationDirectory $sourceItem.Name

        if ((Test-Path $destinationPath) -and -not $Overwrite) {
            Write-Warning "Skip existing managed item: $destinationPath (use -Force to overwrite)"
            continue
        }

        if (Test-Path $destinationPath) {
            Remove-Item -Path $destinationPath -Recurse -Force
        }

        Copy-Item -Path $sourceItem.FullName -Destination $destinationPath -Recurse -Force
        Write-Host "Synced managed item $destinationPath"
    }
}

function Get-ManagedItemNames {
    param(
        [string]$DirectoryPath,
        [string]$Pattern
    )

    if (-not (Test-Path $DirectoryPath)) {
        return @()
    }

    return @(Get-ChildItem -Path $DirectoryPath | Where-Object { $_.Name -like $Pattern } | Select-Object -ExpandProperty Name | Sort-Object)
}

function Write-List {
    param(
        [string]$Title,
        [string[]]$Items
    )

    Write-Host $Title
    if (-not $Items -or $Items.Count -eq 0) {
        Write-Host "  (none)"
        return
    }

    foreach ($item in $Items) {
        Write-Host "  - $item"
    }
}

function Get-StaleManagedItemNames {
    param(
        [string]$SourceDirectory,
        [string]$DestinationDirectory,
        [string]$ManagedPattern
    )

    $sourceNames = Get-ManagedItemNames -DirectoryPath $SourceDirectory -Pattern $ManagedPattern
    $destinationNames = Get-ManagedItemNames -DirectoryPath $DestinationDirectory -Pattern $ManagedPattern

    return @($destinationNames | Where-Object { $_ -notin $sourceNames })
}

function Write-SyncStatusReport {
    $sourceInstructionDirectory = Join-Path $sourceRoot "instructions"
    $targetInstructionDirectory = Join-Path $destinationRoot "instructions"
    $sourceSkillDirectory = Join-Path $sourceRoot "skills"
    $targetSkillDirectory = Join-Path $destinationRoot "skills"

    $managedInstructionNames = Get-ManagedItemNames -DirectoryPath $sourceInstructionDirectory -Pattern "eframe-*"
    $managedSkillNames = Get-ManagedItemNames -DirectoryPath $sourceSkillDirectory -Pattern "eframe-*"
    $projectInstructionNames = Get-ManagedItemNames -DirectoryPath $targetInstructionDirectory -Pattern "project-*"
    $projectSkillNames = Get-ManagedItemNames -DirectoryPath $targetSkillDirectory -Pattern "project-*"
    $staleInstructionNames = Get-StaleManagedItemNames -SourceDirectory $sourceInstructionDirectory -DestinationDirectory $targetInstructionDirectory -ManagedPattern "eframe-*"
    $staleSkillNames = Get-StaleManagedItemNames -SourceDirectory $sourceSkillDirectory -DestinationDirectory $targetSkillDirectory -ManagedPattern "eframe-*"

    Write-Host ""
    Write-Host "EFrame AI sync preview"
    Write-Host "Target root: $resolvedTargetRoot"
    Write-Host "Framework root: $frameworkRoot"
    Write-Host ""
    Write-List -Title "Framework-managed root files:" -Items @(
        "AGENTS.md",
        ".github/copilot-instructions.md",
        ".github/$manifestName"
    )
    Write-List -Title "Framework-managed instructions:" -Items $managedInstructionNames
    Write-List -Title "Framework-managed skills:" -Items $managedSkillNames
    Write-List -Title "Framework Codex/AI docs synced to project root:" -Items $aiDocumentNames
    Write-List -Title "Project instruction overlays preserved:" -Items $projectInstructionNames
    Write-List -Title "Project skill overlays preserved:" -Items $projectSkillNames
    Write-List -Title "Stale framework instruction items removed only with -Force:" -Items $staleInstructionNames
    Write-List -Title "Stale framework skill items removed only with -Force:" -Items $staleSkillNames
    Write-Host ""
}

$sourceVersion = Read-ManifestVersion -ManifestPath $sourceManifestPath
$targetVersion = Read-ManifestVersion -ManifestPath $destinationManifestPath
$sourceManifestFileEntries = Get-ManifestFileEntries -ManifestPath $sourceManifestPath

if ($sourceRoot -eq $destinationRoot) {
    if ($StatusOnly) {
        Write-Host "Framework AI version: $sourceVersion"
        Write-Host "Target AI version: $targetVersion"
        Write-Host "AI workspace config is up to date. Source and target are the same workspace."
        Write-SyncStatusReport
        Write-ManifestFileIntegrityReport -RootPath $resolvedTargetRoot -Entries $sourceManifestFileEntries | Out-Null
        return
    }

    if ($Force) {
        Write-Host "Source and target .github paths are identical. No sync needed."
        Write-Host "Current version: $sourceVersion"
        return
    }
}

if ($StatusOnly) {
    Write-Host "Framework AI version: $sourceVersion"
    Write-SyncStatusReport
    $integrityProblems = Write-ManifestFileIntegrityReport -RootPath $resolvedTargetRoot -Entries $sourceManifestFileEntries

    if (-not $targetVersion) {
        Write-Warning "Target project has no synced AI manifest yet. Run with -Force to initialize or update."
        return
    }

    Write-Host "Target AI version: $targetVersion"

    if ($sourceVersion -eq $targetVersion) {
        if ($integrityProblems.Count -gt 0) {
            Write-Warning "AI workspace manifest version matches, but one or more managed files differ. Run with -Force to restore framework-managed files."
            return
        }

        Write-Host "AI workspace config is up to date."
        return
    }

    Write-Warning "AI workspace config is out of date. Run with -Force to sync the latest instructions and skills."
    return
}

if (-not (Test-Path $destinationRoot)) {
    New-Item -ItemType Directory -Path $destinationRoot | Out-Null
}

Sync-File -SourcePath (Join-Path $sourceRoot "copilot-instructions.md") -DestinationPath (Join-Path $destinationRoot "copilot-instructions.md") -Overwrite:$Force
Sync-ManagedDirectoryItems -SourceDirectory (Join-Path $sourceRoot "instructions") -DestinationDirectory (Join-Path $destinationRoot "instructions") -ManagedPattern "eframe-*" -Overwrite:$Force
Sync-ManagedDirectoryItems -SourceDirectory (Join-Path $sourceRoot "skills") -DestinationDirectory (Join-Path $destinationRoot "skills") -ManagedPattern "eframe-*" -Overwrite:$Force

foreach ($documentName in $aiDocumentNames) {
    Sync-File -SourcePath (Join-Path $frameworkRoot $documentName) -DestinationPath (Join-Path $resolvedTargetRoot $documentName) -Overwrite:$Force
}

Sync-File -SourcePath $sourceManifestPath -DestinationPath $destinationManifestPath -Overwrite:$Force

Write-Host "EFrame AI workspace files are ready at $destinationRoot"
Write-Host "Synced version: $sourceVersion"
