---
name: eframe-resource-flow
description: 'Set up, refactor, or audit EFrame resource loading through Addressables, ResPath, ResPath.Generated, AssetReference, App/Res and Modules/Res directories, async asset handles, and release/dispose paths. Use when adding prefabs, UI assets, scene assets, audio, or module resources.'
argument-hint: 'Describe the resource, expected directory, loading caller, and whether Addressables/ResPath generation is involved.'
user-invocable: true
---

# EFrame Resource Flow

## Workflow

1. Put resources under a stable mapped directory, usually `Assets/App/Res/...`, `Assets/Scenes`, or `Assets/Modules/<Name>/Res/...`.
2. Prefer `ResPath.Generated`, stable `ResPath` entries, or `AssetReference` over hard-coded address strings. Add a directory to the ResPath selection only when its assets are code-driven load entry points.
3. Load runtime assets through `EFrame.Current.Assets.LoadAsync(...)` or `InstantiateAsync(...)`.
4. Store and dispose returned handles according to the owner lifecycle: Procedure, controller, service, or spawned runtime object.
5. Avoid `WaitForCompletion` and synchronous Addressables paths unless there is a documented reason.
6. Keep framework-managed Audio Resources assets under `Assets/Resources/Audio` and route mixer/event-config paths through `AudioResourcePaths`.
7. Treat Addressables entries for managed directories as framework-owned. Do not manually create or rename entries for assets under `Assets/App/Res`, `Assets/Scenes`, or `Assets/Modules`.
8. Let the editor auto-sync managed resources after import, move, or delete; use `EFrame Tools/Addressables/Sync Groups And Generate ResPath` to choose ResPath source directories, inspect the managed resource report window, or force a repair/check.
9. Expect player builds to run an EFrame Addressables preflight that syncs groups, validates generated addresses, and fails the build if selected `ResPath.Generated` entries or managed entries are stale.
10. When editor tooling changes generated paths or Addressables grouping, update the corresponding bootstrap/generation workflow rather than patching business code around it.
11. For UI resources, align prefab names, controller names, and generated binding namespaces with the UI contract.

## Output Checks

- Confirm every repeated address string has a central path source.
- Confirm loaded instances/assets have a release path.
- Confirm module resources can move with their module without rewriting callers.
- For detailed review, read [resource-checklist](./references/resource-checklist.md).
