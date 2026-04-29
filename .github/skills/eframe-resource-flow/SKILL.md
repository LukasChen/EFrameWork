---
name: eframe-resource-flow
description: 'Set up, refactor, or audit EFrame resource loading through Addressables, ResPath, ResPath.Generated, AssetReference, App/Res and Modules/Res directories, async asset handles, and release/dispose paths. Use when adding prefabs, UI assets, scene assets, audio, or module resources.'
argument-hint: 'Describe the resource, expected directory, loading caller, and whether Addressables/ResPath generation is involved.'
user-invocable: true
---

# EFrame Resource Flow

## Workflow

1. Put resources under a stable mapped directory, usually `Assets/App/Res/...`, `Assets/Scenes`, or `Assets/Modules/<Name>/Res/...`.
2. Prefer `ResPath.Generated`, stable `ResPath` entries, or `AssetReference` over hard-coded address strings.
3. Load runtime assets through `EFrame.Current.Assets.LoadAsync(...)` or `InstantiateAsync(...)`.
4. Store and dispose returned handles according to the owner lifecycle: Procedure, controller, service, or spawned runtime object.
5. Avoid `WaitForCompletion` and synchronous Addressables paths unless there is a documented reason.
6. When editor tooling changes generated paths or Addressables grouping, update the corresponding bootstrap/generation workflow rather than patching business code around it.
7. For UI resources, align prefab names, controller names, and generated binding namespaces with the UI contract.

## Output Checks

- Confirm every repeated address string has a central path source.
- Confirm loaded instances/assets have a release path.
- Confirm module resources can move with their module without rewriting callers.
- For detailed review, read [resource-checklist](./references/resource-checklist.md).
