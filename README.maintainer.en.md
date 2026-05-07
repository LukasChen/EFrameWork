# EFrame Maintainer README

[Back to user README](README.en.md) | [中文](README.maintainer.md)

This document is for EFrame framework maintainers. It centralizes repository maintenance, template sync, AI contracts, release checks, and version boundaries. Business project users should start from [README.en.md](README.en.md).

## Maintainer-Facing Repository Structure

- [packages/com.eframework.core/AIWorkspace~](packages/com.eframework.core/AIWorkspace~): Package-shipped AI collaboration source, including `eframe-*` instructions and skills, managed blocks, manifest, and AI support docs.
- [packages/com.eframework.core/Documentation~/maintainer](packages/com.eframework.core/Documentation~/maintainer): Human-facing maintainer docs.
- [test-fixtures](test-fixtures): Unity fixtures used to validate the Basic template, Showcase module, and package consumer paths.
- [tools](tools): Framework repository maintenance scripts and wrappers around package scripts. Business projects should prefer package-local `Tools~/`.
- [tools/MaintainerAIWorkspace](tools/MaintainerAIWorkspace): Framework maintainer-only AI workspace. It is not synced into business projects.
- [AGENTS.md](AGENTS.md), [CLAUDE.md](CLAUDE.md), [.github/copilot-instructions.md](.github/copilot-instructions.md): AI client entries for this framework repository. They only route to the right contracts and do not duplicate business rules.

## Standard Maintenance Flow

For every maintenance change, identify the impact area before deciding which contracts need updates:

1. Classify the change as a bug fix, docs correction, template adjustment, runtime/editor behavior change, AI sync layer change, or formal release.
2. After changing implementation, templates, or docs, check whether the recommended business-project usage changed. If it did, update the matching instruction, skill, or support doc under `packages/com.eframework.core/AIWorkspace~/`.
3. If the change touches Basic, Showcase, AI sync, cold start, resource directories, UI runtime contract, manifest, or release rules, complete the matching checks from [EFRAME_AI_RELEASE_CHECKLIST.md](tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md).
4. If the change affects how business-project AI chooses EFrame workflows, run prompt-routing regression with [EFRAME_AI_PROMPT_ROUTING_AUDIT.md](packages/com.eframework.core/Documentation~/maintainer/EFRAME_AI_PROMPT_ROUTING_AUDIT.md).
5. Update the root [CHANGELOG.md](CHANGELOG.md). If package code or package-local docs changed, also update [packages/com.eframework.core/CHANGELOG.md](packages/com.eframework.core/CHANGELOG.md).
6. Run the checks that match the impact area, and record the verification result in the PR or release notes.

Common check commands:

```powershell
.\tools\Test-EFrameAIRelease.ps1
.\tools\Test-EFrameConsumer.ps1
.\tools\Sync-EFrameBasicTemplate.ps1 -CheckOnly
.\tools\Sync-EFrameShowcaseTemplate.ps1 -CheckOnly
```

## Bug Patch Flow

Use this for defects in an already published version when the fix does not introduce breaking API changes.

1. Reproduce the issue and record the trigger, affected package, Unity version, and business-project behavior.
2. Make the smallest practical fix, without unrelated refactors.
3. If the bug exposes a docs or AI guidance gap, update the user docs, `eframe-*` instruction/skill, or API index. Maintainer-only notes belong in maintainer docs or a `maintainer-*` skill.
4. Add or update the test, fixture check, or manual verification step that covers the problem.
5. Update the changelog. For a fix to a published stable version, usually bump the patch version, for example `0.7.5 -> 0.7.6`.
6. Run the checks that match the impact area. At minimum, run the relevant release checklist checks; if the fix affects package consumption, run `Test-EFrameConsumer.ps1`.
7. When the fix needs to be published externally, validate a preview entry before the formal release.

## Preview Validation Flow

Preview validation lets a real business project test the package that is about to be released. It is not the formal release tag.

1. Confirm the version, fix scope, risk areas, and required business-project validation scenarios.
2. Create an immutable validation entry. Prefer a `preview/<package>-<version>-rc.N` branch, or record a specific commit SHA.
3. Import the preview entry in a real business project through a Unity Package Manager Git URL:

```text
https://github.com/ethanhubin/EFrame.git?path=/packages/com.eframework.core#preview/core-0.7.6-rc.1
https://github.com/ethanhubin/EFrame.git?path=/packages/com.eframework.core#<commit-sha>
```

4. Open Unity in the business project and verify package import, script compilation, the initialization window, and the affected Basic or module scenes.
5. If the change affects AI sync, run the business-project updater:

```powershell
.\tools\Sync-EFrameAIFromFramework.ps1 -Clients all -StatusOnly
.\tools\Sync-EFrameAIFromFramework.ps1 -Clients all -Force
```

6. Record the preview result, including the preview entry, validation project, passed checks, found issues, and whether another `rc.N` is required.
7. Do not use the formal `vX.Y.Z` tag, `main`, or a moving temporary branch as the preview validation entry.

## Formal Release Flow

Before a formal release, preview validation must be complete, and the maintainer must confirm the release scope, version, tag, target remote, and validation result.

1. Confirm that `packages/com.eframework.core/package.json` has the bumped version, or explicitly confirm that this change does not publish a package version.
2. Confirm that the root [CHANGELOG.md](CHANGELOG.md) and [packages/com.eframework.core/CHANGELOG.md](packages/com.eframework.core/CHANGELOG.md) record the change.
3. If synced AI files, managed blocks, support docs, or sync scripts changed, update the version, file list, and hashes in `packages/com.eframework.core/AIWorkspace~/eframe-ai.manifest.json`.
4. Run the blocking release checks:

```powershell
.\tools\Test-EFrameAIRelease.ps1
.\tools\Test-EFrameConsumer.ps1
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all -StatusOnly
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all -Force
```

5. Add Basic, Showcase, Unity compile, AI project, or PlayMode validation according to the impact area.
6. Summarize the commit scope, version, formal tag, preview validation result, and target remote, then wait for maintainer confirmation.
7. After confirmation, create the release commit, create the formal tag that matches the package version, for example `v0.7.6`, and push to the confirmed remote.
8. After release, run one sync or upgrade validation in a business project and confirm that the formal tag works through Unity Package Manager.

## Basic And Showcase Maintenance

To maintain the Basic template, edit [test-fixtures/EFrameBasicTemplate](test-fixtures/EFrameBasicTemplate), then sync it back into the package template:

```powershell
.\tools\Sync-EFrameBasicTemplate.ps1
.\tools\Sync-EFrameBasicTemplate.ps1 -CheckOnly
```

Extension Showcase is an optional business module installed to `Assets/Modules/EFrameExtensionShowcase/`. It demonstrates UI Virtual List and Debug Console. To maintain Showcase, edit [test-fixtures/EFrameShowcaseUnity](test-fixtures/EFrameShowcaseUnity), then sync it back into the package template:

```powershell
.\tools\Sync-EFrameShowcaseTemplate.ps1
.\tools\Sync-EFrameShowcaseTemplate.ps1 -CheckOnly
```

## AI Collaboration Maintenance Boundary

EFrame's AI collaboration layer is part of the framework release contract, not a separate documentation bundle.

- The business-project sync source lives in [packages/com.eframework.core/AIWorkspace~](packages/com.eframework.core/AIWorkspace~).
- Framework-managed files synced into business projects use the `eframe-*` prefix.
- Project entry files for Codex, GitHub Copilot, and Claude Code are owned by the business project; EFrame only updates the EFrame managed block inside those entries.
- AI-facing support docs live in `AIWorkspace~/support-docs/`; the current API index is [EFRAME_AI_API_INDEX.md](packages/com.eframework.core/AIWorkspace~/support-docs/EFRAME_AI_API_INDEX.md).
- Framework maintainer-only rules live in [tools/MaintainerAIWorkspace](tools/MaintainerAIWorkspace) and are not synced into business projects.

Check a synced project:

```powershell
.\tools\Test-EFrameAIProject.ps1 -TargetRoot "D:\YourUnityProject" -FrameworkRoot "."
```

## Maintenance Checks

Before release, run at least:

```powershell
.\tools\Test-EFrameAIRelease.ps1
```

Changes touching Basic, Showcase, AI sync, resource directories, startup flow, UI runtime contract, or manifest should also run the matching sync scripts and project checks according to their impact.

AI release and manifest rules are documented in [tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md](tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md).

## Maintainer Documentation

- AI architecture: [EFRAME_AI_ARCHITECTURE.md](packages/com.eframework.core/Documentation~/maintainer/EFRAME_AI_ARCHITECTURE.md)
- AI setup: [EFRAME_AI_SETUP.md](packages/com.eframework.core/Documentation~/maintainer/EFRAME_AI_SETUP.md)
- AI prompt routing audit: [EFRAME_AI_PROMPT_ROUTING_AUDIT.md](packages/com.eframework.core/Documentation~/maintainer/EFRAME_AI_PROMPT_ROUTING_AUDIT.md)
- AI release checklist: [EFRAME_AI_RELEASE_CHECKLIST.md](tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md)
- Maintainer AI contract skill: [maintainer-ai-contract/SKILL.md](tools/MaintainerAIWorkspace/skills/maintainer-ai-contract/SKILL.md)

## Versioning

EFrame uses semantic versioning. The formal starting version is `0.1.0`; the current release version is recorded in [packages/com.eframework.core/package.json](packages/com.eframework.core/package.json), and repository-level releases are recorded in [CHANGELOG.md](CHANGELOG.md).

Unity code, AI collaboration rules, bootstrap tools, sync scripts, and docs are treated as one EFrame release surface. `AIWorkspace~/eframe-ai.manifest.json` is only an internal marker used by business projects to detect synced file drift; it is not a separate product version.
