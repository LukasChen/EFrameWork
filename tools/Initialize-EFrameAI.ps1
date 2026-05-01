param(
    [string]$TargetRoot = (Get-Location).Path,
    [string[]]$Clients = @("all"),
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
    "EFRAME_AI_API_INDEX.md",
    "EFRAME_AI_ARCHITECTURE.md",
    "EFRAME_AI_SETUP.md",
    "EFRAME_AI_RELEASE_CHECKLIST.md"
)

$validClients = @("codex", "copilot", "claude-code")
$managedBlockBeginPrefix = "<!-- BEGIN EFRAMEWORK AI MANAGED BLOCK:"
$managedBlockEndPrefix = "<!-- END EFRAMEWORK AI MANAGED BLOCK:"
$managedRootBlocks = @(
    [pscustomobject]@{
        Client = "codex"
        TargetPath = "AGENTS.md"
        SourcePath = ".github/managed-blocks/eframe-codex-root.md"
        BlockId = "eframework-codex-root"
        Title = "Project Codex instructions"
    },
    [pscustomobject]@{
        Client = "copilot"
        TargetPath = ".github/copilot-instructions.md"
        SourcePath = ".github/managed-blocks/eframe-copilot-root.md"
        BlockId = "eframework-copilot-root"
        Title = "Project Copilot instructions"
    },
    [pscustomobject]@{
        Client = "claude-code"
        TargetPath = "CLAUDE.md"
        SourcePath = ".github/managed-blocks/eframe-claude-root.md"
        BlockId = "eframework-claude-root"
        Title = "Project Claude Code instructions"
    }
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

function Resolve-AIClients {
    param(
        [string[]]$RequestedClients
    )

    if (-not $RequestedClients -or $RequestedClients.Count -eq 0) {
        return @("codex", "copilot", "claude-code")
    }

    $resolved = New-Object System.Collections.Generic.List[string]
    foreach ($client in $RequestedClients) {
        if ([string]::IsNullOrWhiteSpace($client)) {
            continue
        }

        foreach ($part in ($client -split ",")) {
            $normalized = $part.Trim().ToLowerInvariant()
            if ([string]::IsNullOrWhiteSpace($normalized)) {
                continue
            }

            if ($normalized -eq "all") {
                foreach ($validClient in $validClients) {
                    if (-not $resolved.Contains($validClient)) {
                        $resolved.Add($validClient)
                    }
                }
                continue
            }

            if ($normalized -eq "claude" -or $normalized -eq "claudecode") {
                $normalized = "claude-code"
            }

            if ($normalized -notin $validClients) {
                throw "Unsupported AI client '$part'. Valid clients: all, $($validClients -join ', ')."
            }

            if (-not $resolved.Contains($normalized)) {
                $resolved.Add($normalized)
            }
        }
    }

    if ($resolved.Count -eq 0) {
        return @("codex", "copilot", "claude-code")
    }

    return $resolved.ToArray()
}

function Test-AIClientSelected {
    param(
        [string[]]$SelectedClients,
        [string]$Client
    )

    return $SelectedClients -contains $Client
}

function Get-SelectedManagedRootBlocks {
    param(
        [string[]]$SelectedClients
    )

    return @($managedRootBlocks | Where-Object {
        Test-AIClientSelected -SelectedClients $SelectedClients -Client $_.Client
    })
}

function Get-ManagedRootBlockTargetPaths {
    param(
        [string[]]$SelectedClients
    )

    return @((Get-SelectedManagedRootBlocks -SelectedClients $SelectedClients) | ForEach-Object { $_.TargetPath })
}

function Test-ManifestEntrySelected {
    param(
        [object]$Entry,
        [string[]]$SelectedClients
    )

    $relativePath = [string]$Entry.path
    if ($relativePath -in $aiDocumentNames) {
        return $true
    }

    if ($relativePath -eq "AGENTS.md") {
        return Test-AIClientSelected -SelectedClients $SelectedClients -Client "codex"
    }

    if ($relativePath -eq "CLAUDE.md") {
        return Test-AIClientSelected -SelectedClients $SelectedClients -Client "claude-code"
    }

    if ($relativePath -eq ".github/copilot-instructions.md" -or $relativePath -like ".github/instructions/eframe-*" -or $relativePath -like ".github/skills/eframe-*") {
        return Test-AIClientSelected -SelectedClients $SelectedClients -Client "copilot"
    }

    return $false
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

function Get-SelectedManifestFileEntries {
    param(
        [string]$ManifestPath,
        [string[]]$SelectedClients
    )

    $entries = Get-ManifestFileEntries -ManifestPath $ManifestPath
    return @($entries | Where-Object { Test-ManifestEntrySelected -Entry $_ -SelectedClients $SelectedClients })
}

function Write-FilteredManifest {
    param(
        [string]$SourceManifestPath,
        [string]$DestinationManifestPath,
        [string[]]$SelectedClients
    )

    $sourceManifest = Get-Content -Path $SourceManifestPath -Raw | ConvertFrom-Json
    $selectedEntries = Get-SelectedManifestFileEntries -ManifestPath $SourceManifestPath -SelectedClients $SelectedClients
    $manifest = [ordered]@{
        name = $sourceManifest.name
        version = $sourceManifest.version
        updatedAt = $sourceManifest.updatedAt
        notes = $sourceManifest.notes
        clients = $SelectedClients
        files = $selectedEntries
    }

    $destinationParent = Split-Path -Parent $DestinationManifestPath
    if (-not (Test-Path $destinationParent)) {
        New-Item -ItemType Directory -Path $destinationParent | Out-Null
    }

    $manifest | ConvertTo-Json -Depth 8 | Set-Content -Path $DestinationManifestPath -Encoding UTF8
    Write-Host "Synced filtered manifest $DestinationManifestPath"
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

function Get-ManagedBlockMarkers {
    param(
        [string]$BlockId
    )

    return [pscustomobject]@{
        Begin = "$managedBlockBeginPrefix $BlockId -->"
        End = "$managedBlockEndPrefix $BlockId -->"
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

    $markers = Get-ManagedBlockMarkers -BlockId $BlockId
    $beginIndex = $Text.IndexOf($markers.Begin, [System.StringComparison]::Ordinal)
    if ($beginIndex -lt 0) {
        return $null
    }

    $contentStart = $beginIndex + $markers.Begin.Length
    if ($contentStart -lt $Text.Length -and $Text[$contentStart] -eq "`r") {
        $contentStart++
    }
    if ($contentStart -lt $Text.Length -and $Text[$contentStart] -eq "`n") {
        $contentStart++
    }

    $endIndex = $Text.IndexOf($markers.End, $contentStart, [System.StringComparison]::Ordinal)
    if ($endIndex -lt 0) {
        return $null
    }

    return $Text.Substring($contentStart, $endIndex - $contentStart)
}

function New-ManagedBlockText {
    param(
        [string]$SourcePath,
        [string]$BlockId
    )

    $body = Get-Content -Path $SourcePath -Raw -Encoding UTF8
    $markers = Get-ManagedBlockMarkers -BlockId $BlockId
    $normalizedBody = Normalize-ManagedBlockText -Text $body
    return "$($markers.Begin)`n$normalizedBody$($markers.End)`n"
}

function New-ProjectInstructionDocument {
    param(
        [object]$BlockSpec,
        [string]$ManagedBlockText
    )

    return "# $($BlockSpec.Title)`n`nThis file belongs to this project. Keep project-specific AI rules outside the EFrameWork managed block; EFrame sync only updates the marked block below.`n`n$ManagedBlockText"
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
        $kind = [string]$entry.kind

        if ($kind -eq "managedBlock") {
            if ($RootPath -eq $frameworkRoot -and $entry.sourcePath) {
                $sourcePath = Join-Path $frameworkRoot (([string]$entry.sourcePath) -replace "/", "\")
                if (-not (Test-Path $sourcePath)) {
                    $results.Add([pscustomobject]@{
                        Path = $relativePath
                        Status = "MissingSourceBlock"
                        Expected = $expectedHash
                        Actual = $null
                    })
                    continue
                }

                $actualHash = Get-TextSha256 -Text (Get-Content -Path $sourcePath -Raw -Encoding UTF8)
                $status = if ($actualHash -eq $expectedHash) { "OK" } else { "Mismatch" }
                $results.Add([pscustomobject]@{
                    Path = $relativePath
                    Status = $status
                    Expected = $expectedHash
                    Actual = $actualHash
                })
                continue
            }

            if (-not (Test-Path $absolutePath)) {
                $results.Add([pscustomobject]@{
                    Path = $relativePath
                    Status = "Missing"
                    Expected = $expectedHash
                    Actual = $null
                })
                continue
            }

            $documentText = Get-Content -Path $absolutePath -Raw -Encoding UTF8
            $blockBody = Get-ManagedBlockBody -Text $documentText -BlockId ([string]$entry.blockId)
            if ($null -eq $blockBody) {
                $results.Add([pscustomobject]@{
                    Path = $relativePath
                    Status = "MissingBlock"
                    Expected = $expectedHash
                    Actual = $null
                })
                continue
            }

            $actualHash = Get-TextSha256 -Text $blockBody
            $status = if ($actualHash -eq $expectedHash) { "OK" } else { "Mismatch" }
            $results.Add([pscustomobject]@{
                Path = $relativePath
                Status = $status
                Expected = $expectedHash
                Actual = $actualHash
            })
            continue
        }

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

function Sync-ManagedBlock {
    param(
        [object]$BlockSpec,
        [switch]$Overwrite
    )

    $sourcePath = Join-Path $frameworkRoot ($BlockSpec.SourcePath -replace "/", "\")
    $destinationPath = Join-Path $resolvedTargetRoot ($BlockSpec.TargetPath -replace "/", "\")
    if (-not (Test-Path $sourcePath)) {
        throw "Managed block source not found: $sourcePath"
    }

    $managedBlockText = New-ManagedBlockText -SourcePath $sourcePath -BlockId $BlockSpec.BlockId
    $destinationParent = Split-Path -Parent $destinationPath
    if ($destinationParent -and -not (Test-Path $destinationParent)) {
        New-Item -ItemType Directory -Path $destinationParent | Out-Null
    }

    if (-not (Test-Path $destinationPath)) {
        New-ProjectInstructionDocument -BlockSpec $BlockSpec -ManagedBlockText $managedBlockText |
            Set-Content -Path $destinationPath -Encoding UTF8
        Write-Host "Created project-owned instruction with EFrame managed block $destinationPath"
        return
    }

    $existingText = Get-Content -Path $destinationPath -Raw -Encoding UTF8
    $markers = Get-ManagedBlockMarkers -BlockId $BlockSpec.BlockId
    $beginCount = ([regex]::Matches($existingText, [regex]::Escape($markers.Begin))).Count
    $endCount = ([regex]::Matches($existingText, [regex]::Escape($markers.End))).Count

    if ($beginCount -eq 0 -and $endCount -eq 0) {
        if (-not $Overwrite) {
            Write-Warning "Project instruction has no EFrame managed block: $destinationPath (use -Force to inject)"
            return
        }

        $separator = if ($existingText.Trim().Length -eq 0) { "" } else { "`n`n" }
        ($existingText.TrimEnd() + $separator + $managedBlockText) | Set-Content -Path $destinationPath -Encoding UTF8
        Write-Host "Injected EFrame managed block into project-owned instruction $destinationPath"
        return
    }

    if ($beginCount -ne 1 -or $endCount -ne 1) {
        Write-Warning "Project instruction has duplicate or broken EFrame managed block markers: $destinationPath"
        return
    }

    $pattern = "(?s)" + [regex]::Escape($markers.Begin) + ".*?" + [regex]::Escape($markers.End)
    $replacement = $managedBlockText.TrimEnd()
    $updatedText = [regex]::Replace($existingText, $pattern, [System.Text.RegularExpressions.MatchEvaluator]{ param($match) $replacement }, 1)
    $updatedText | Set-Content -Path $destinationPath -Encoding UTF8
    Write-Host "Updated EFrame managed block in project-owned instruction $destinationPath"
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
    param(
        [string[]]$SelectedClients
    )

    $sourceInstructionDirectory = Join-Path $sourceRoot "instructions"
    $targetInstructionDirectory = Join-Path $destinationRoot "instructions"
    $sourceSkillDirectory = Join-Path $sourceRoot "skills"
    $targetSkillDirectory = Join-Path $destinationRoot "skills"

    $managedInstructionNames = Get-ManagedItemNames -DirectoryPath $sourceInstructionDirectory -Pattern "eframe-*"
    $managedSkillNames = Get-ManagedItemNames -DirectoryPath $sourceSkillDirectory -Pattern "eframe-*"
    $staleInstructionNames = Get-StaleManagedItemNames -SourceDirectory $sourceInstructionDirectory -DestinationDirectory $targetInstructionDirectory -ManagedPattern "eframe-*"
    $staleSkillNames = Get-StaleManagedItemNames -SourceDirectory $sourceSkillDirectory -DestinationDirectory $targetSkillDirectory -ManagedPattern "eframe-*"

    Write-Host ""
    Write-Host "EFrame AI sync preview"
    Write-Host "Target root: $resolvedTargetRoot"
    Write-Host "Framework root: $frameworkRoot"
    Write-Host "Selected AI clients: $($SelectedClients -join ', ')"
    Write-Host ""
    $rootFiles = @(".github/$manifestName") + $aiDocumentNames
    Write-List -Title "Framework-managed root files:" -Items @($rootFiles | Sort-Object -Unique)
    Write-List -Title "Project-owned root instruction files receiving EFrame managed blocks:" -Items (Get-ManagedRootBlockTargetPaths -SelectedClients $SelectedClients)
    if (Test-AIClientSelected -SelectedClients $SelectedClients -Client "copilot") {
        Write-List -Title "Framework-managed instructions:" -Items $managedInstructionNames
        Write-List -Title "Framework-managed skills:" -Items $managedSkillNames
    }
    else {
        Write-List -Title "Framework-managed instructions:" -Items @()
        Write-List -Title "Framework-managed skills:" -Items @()
    }
    Write-List -Title "Framework Codex/AI docs synced to project root:" -Items $aiDocumentNames
    Write-List -Title "Stale framework instruction items removed only with -Force:" -Items $staleInstructionNames
    Write-List -Title "Stale framework skill items removed only with -Force:" -Items $staleSkillNames
    Write-Host ""
}

$sourceVersion = Read-ManifestVersion -ManifestPath $sourceManifestPath
$targetVersion = Read-ManifestVersion -ManifestPath $destinationManifestPath
$selectedClients = Resolve-AIClients -RequestedClients $Clients
$sourceManifestFileEntries = Get-SelectedManifestFileEntries -ManifestPath $sourceManifestPath -SelectedClients $selectedClients

if ($sourceRoot -eq $destinationRoot) {
    if ($StatusOnly) {
        Write-Host "Framework AI version: $sourceVersion"
        Write-Host "Target AI version: $targetVersion"
        Write-Host "AI workspace config is up to date. Source and target are the same workspace."
        Write-SyncStatusReport -SelectedClients $selectedClients
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
    Write-SyncStatusReport -SelectedClients $selectedClients
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

if (Test-AIClientSelected -SelectedClients $selectedClients -Client "copilot") {
    Sync-ManagedDirectoryItems -SourceDirectory (Join-Path $sourceRoot "instructions") -DestinationDirectory (Join-Path $destinationRoot "instructions") -ManagedPattern "eframe-*" -Overwrite:$Force
    Sync-ManagedDirectoryItems -SourceDirectory (Join-Path $sourceRoot "skills") -DestinationDirectory (Join-Path $destinationRoot "skills") -ManagedPattern "eframe-*" -Overwrite:$Force
}

foreach ($documentName in $aiDocumentNames) {
    Sync-File -SourcePath (Join-Path $frameworkRoot $documentName) -DestinationPath (Join-Path $resolvedTargetRoot $documentName) -Overwrite:$Force
}

foreach ($blockSpec in (Get-SelectedManagedRootBlocks -SelectedClients $selectedClients)) {
    Sync-ManagedBlock -BlockSpec $blockSpec -Overwrite:$Force
}

Write-FilteredManifest -SourceManifestPath $sourceManifestPath -DestinationManifestPath $destinationManifestPath -SelectedClients $selectedClients

Write-Host "EFrame AI workspace files are ready at $destinationRoot"
Write-Host "Synced version: $sourceVersion"
Write-Host "Synced clients: $($selectedClients -join ', ')"
