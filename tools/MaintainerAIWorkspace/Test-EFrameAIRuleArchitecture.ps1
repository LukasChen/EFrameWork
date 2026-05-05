param(
    [string]$Root = $PSScriptRoot
)

$ErrorActionPreference = "Stop"

$resolvedRoot = (Resolve-Path -LiteralPath $Root).Path
$maintainerRootFromRepo = Join-Path $resolvedRoot "tools/MaintainerAIWorkspace"
$rulesRootFromCurrent = Join-Path $resolvedRoot "rules"

if (Test-Path -LiteralPath $rulesRootFromCurrent) {
    $maintainerRoot = $resolvedRoot
}
elseif (Test-Path -LiteralPath $maintainerRootFromRepo) {
    $maintainerRoot = $maintainerRootFromRepo
}
else {
    $maintainerRoot = $resolvedRoot
}

$rulesRoot = Join-Path $maintainerRoot "rules"
$registryPath = Join-Path $rulesRoot "registry.yaml"
$setsPath = Join-Path $rulesRoot "rule-sets.yaml"
$viewsPath = Join-Path $rulesRoot "rule-views.yaml"
$loadingPath = Join-Path $rulesRoot "loading-policy.yaml"

$errors = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

function Add-Error {
    param([string]$Message)
    $script:errors.Add($Message)
}

function Add-Warning {
    param([string]$Message)
    $script:warnings.Add($Message)
}

function Require-File {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        Add-Error "Missing required file: $Path"
        return $false
    }
    return $true
}

function Get-RegexValues {
    param(
        [string]$Path,
        [string]$Pattern,
        [int]$Group = 1
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return @()
    }

    return @(
        Select-String -LiteralPath $Path -Pattern $Pattern | ForEach-Object {
            $match = [regex]::Match($_.Line, $Pattern)
            if ($match.Success) {
                $match.Groups[$Group].Value
            }
        }
    )
}

function Get-YamlChildKeys {
    param(
        [string]$Path,
        [string]$Section
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return @()
    }

    $values = New-Object System.Collections.Generic.List[string]
    $inSection = $false
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ($line -match "^$([regex]::Escape($Section)):\s*$") {
            $inSection = $true
            continue
        }

        if ($inSection -and $line -match "^\S") {
            break
        }

        if ($inSection) {
            $match = [regex]::Match($line, "^\s{2}([A-Za-z0-9_.-]+):(?:\s.*)?$")
            if ($match.Success) {
                $values.Add($match.Groups[1].Value)
            }
        }
    }

    return @($values)
}

function Assert-NoDuplicates {
    param(
        [string[]]$Values,
        [string]$Label
    )

    $duplicates = @($Values | Group-Object | Where-Object { $_.Count -gt 1 })
    foreach ($duplicate in $duplicates) {
        Add-Error "Duplicate $Label '$($duplicate.Name)' appears $($duplicate.Count) times."
    }
}

function Assert-AllKnown {
    param(
        [string[]]$Refs,
        [string[]]$Known,
        [string]$Label
    )

    $missing = @($Refs | Where-Object { $_ -notin $Known } | Sort-Object -Unique)
    foreach ($item in $missing) {
        Add-Error "Unknown $Label reference: $item"
    }
}

Require-File $registryPath | Out-Null
Require-File $setsPath | Out-Null
Require-File $viewsPath | Out-Null
Require-File $loadingPath | Out-Null

$ruleIds = Get-RegexValues -Path $registryPath -Pattern '^\s{2}- id:\s*([A-Z][0-9][0-9])\s*$'
$setIds = Get-YamlChildKeys -Path $setsPath -Section "sets"
$viewIds = Get-YamlChildKeys -Path $viewsPath -Section "views"
$scenarioIds = Get-YamlChildKeys -Path $loadingPath -Section "scenarios"

Assert-NoDuplicates -Values $ruleIds -Label "rule id"
Assert-NoDuplicates -Values $setIds -Label "rule set id"
Assert-NoDuplicates -Values $viewIds -Label "rule view id"
Assert-NoDuplicates -Values $scenarioIds -Label "loading scenario id"

if ($ruleIds.Count -eq 0) {
    Add-Error "No rule IDs found in $registryPath."
}

if ($setIds.Count -eq 0) {
    Add-Error "No rule set IDs found in $setsPath."
}

if ($viewIds.Count -eq 0) {
    Add-Error "No rule view IDs found in $viewsPath."
}

if ($scenarioIds.Count -eq 0) {
    Add-Error "No loading scenario IDs found in $loadingPath."
}

$setRuleRefs = Get-RegexValues -Path $setsPath -Pattern '^\s{6}-\s*([A-Z][0-9][0-9])\s*$'
Assert-AllKnown -Refs $setRuleRefs -Known $ruleIds -Label "rule"

$viewSetRefs = Get-RegexValues -Path $viewsPath -Pattern '^\s{6}- set:\s*([a-z0-9.-]+)\s*$'
Assert-AllKnown -Refs $viewSetRefs -Known $setIds -Label "rule set"

$viewFullRuleRefs = Get-RegexValues -Path $viewsPath -Pattern '^\s{6}-\s*([A-Z][0-9][0-9])\s*$'
Assert-AllKnown -Refs $viewFullRuleRefs -Known $ruleIds -Label "view full rule"

$loadingViewRefs = Get-RegexValues -Path $loadingPath -Pattern '^\s{6}-\s*((?:instruction|support-doc|skill|managed-block|maintainer)\.[a-z0-9.-]+)\s*$'
Assert-AllKnown -Refs $loadingViewRefs -Known $viewIds -Label "rule view"

$loadingConditionalViewRefs = Get-RegexValues -Path $loadingPath -Pattern '^\s{6}((?:instruction|support-doc|skill|managed-block|maintainer)\.[a-z0-9.-]+):\s*$'
Assert-AllKnown -Refs $loadingConditionalViewRefs -Known $viewIds -Label "conditional rule view"

$loadingSetOrViewRefs = Get-RegexValues -Path $loadingPath -Pattern '^\s{4}-\s*([a-z0-9.-]+)\s*$' | Where-Object { $_ -match '^(?:governance|startup|ui|resources|directory|data|editor|workflow|maintainer|platform|instruction|support-doc|skill|managed-block)\.' }
Assert-AllKnown -Refs $loadingSetOrViewRefs -Known (@($setIds) + @($viewIds)) -Label "rule set or view"

$registryDomains = Get-YamlChildKeys -Path $registryPath -Section "domains"
$ruleDomains = Get-RegexValues -Path $registryPath -Pattern '^\s{4}domain:\s*([a-z][a-z0-9-]*)\s*$'
Assert-AllKnown -Refs $ruleDomains -Known $registryDomains -Label "registry domain"

$ruleLayers = Get-RegexValues -Path $registryPath -Pattern '^\s{4}layer:\s*([a-z-]+)\s*$'
$knownLayers = @("governance", "business-contract", "workflow", "reference", "validation", "maintainer-only", "platform-output")
Assert-AllKnown -Refs $ruleLayers -Known $knownLayers -Label "rule layer"

$ruleStatuses = Get-RegexValues -Path $registryPath -Pattern '^\s{4}status:\s*([a-z-]+)\s*$'
$knownStatuses = @("draft", "candidate", "stable", "deprecated", "removed")
Assert-AllKnown -Refs $ruleStatuses -Known $knownStatuses -Label "rule status"

$businessViews = @($viewIds | Where-Object { $_ -like "skill.*" -or $_ -like "instruction.*" -or $_ -like "support-doc.*" -or $_ -like "managed-block.*" })
foreach ($view in $businessViews) {
    if ($view -match "release|maintainer") {
        Add-Warning "Business-facing view name contains maintainer/release term: $view"
    }
}

Write-Host "EFrame AI rule architecture check"
Write-Host "Maintainer root: $maintainerRoot"
Write-Host "Rules: $($ruleIds.Count)"
Write-Host "Rule sets: $($setIds.Count)"
Write-Host "Rule views: $($viewIds.Count)"
Write-Host "Loading scenarios: $($scenarioIds.Count)"
Write-Host "Rule refs from sets: $($setRuleRefs.Count)"
Write-Host "Set refs from views: $($viewSetRefs.Count)"
Write-Host "View refs from loading policy: $($loadingViewRefs.Count)"

foreach ($warning in $warnings) {
    Write-Warning $warning
}

if ($errors.Count -gt 0) {
    foreach ($ruleError in $errors) {
        Write-Error $ruleError
    }
    throw "EFrame AI rule architecture check failed."
}

Write-Host "EFrame AI rule architecture check passed."
