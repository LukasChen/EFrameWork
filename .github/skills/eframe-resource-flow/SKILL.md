---
name: eframe-resource-flow
description: 'Set up, refactor, or audit EFrame resource loading through Addressables, generated ResPath.Generated ids, editor-authored AssetReference fields, App/Res and Modules/Res directories, async asset handles, and release/dispose paths. Use when adding prefabs, UI assets, scene assets, audio, or module resources.'
argument-hint: 'Describe the resource, expected directory, loading caller, and whether Addressables/ResPath generation is involved.'
user-invocable: true
capabilities:
  - eframe.resources.addressables
  - eframe.resources.respath
  - eframe.assets.preload
  - eframe.assets.release
owns:
  - managed resource directory placement
  - generated ResPath and assetId flow
  - async asset handle ownership
delegatesTo:
  - eframe-ui-feature
  - eframe-guideline-audit
outputs:
  - centralized resource id plan
  - preload and release lifecycle mapping
  - Addressables automation alignment
forbiddenPatterns:
  - manually editing managed Addressables entries
  - runtime business logic passing AssetReference objects downstream
  - undocumented WaitForCompletion in hot paths
---

# EFrame Resource Flow

## Workflow

1. Put resources under a stable mapped directory, usually `Assets/App/Res/...`, `Assets/Scenes`, or `Assets/Modules/<Name>/Res/...`.
2. Use `ResPath.Generated` for runtime resource ids. `AssetReference` may be used for editor authoring fields, but runtime business calls should receive generated asset ids instead of raw Addressables strings.
3. For Procedure-owned hot paths, preload assets through `OnPreloadAsync(IAssetPreloadScope assets, ProcedureEnterContext context)`, then instantiate with `Context.Assets.Instantiate(...)` or `Context.Assets.GetFromPool(...)`.
4. Use `EFrame.Current.Assets.LoadAsync(...)` or `InstantiateAsync(...)` only when a caller explicitly owns an async handle/instance lifecycle outside the Procedure preload path.
5. Store and dispose returned handles according to the owner lifecycle: Procedure, controller, service, or spawned runtime object.
6. Avoid `WaitForCompletion` and synchronous Addressables paths unless there is a documented reason.
7. Keep framework-managed Audio Resources assets under `Assets/Resources/Audio` and route mixer/event-config paths through `AudioResourcePaths`.
8. Treat Addressables entries for managed directories as framework-owned. Do not manually create or rename entries for assets under `Assets/App/Res`, `Assets/Scenes`, or `Assets/Modules`.
9. Let the editor auto-sync managed resources after import, move, or delete; use `EFrame Tools/Addressables/Sync Groups And Generate ResPath` to choose ResPath source directory trees, inspect the managed resource report window, or force a repair/check.
10. Expect player builds to run an EFrame Addressables preflight that syncs groups, validates generated addresses, and fails the build if selected `ResPath.Generated` entries or managed entries drift.
11. When editor tooling changes generated paths or Addressables grouping, update the corresponding bootstrap/generation workflow rather than patching business code around it.
12. For UI resources, align prefab names, controller names, and generated binding namespaces with the UI contract.

## Output Checks

- Confirm runtime callers use generated `ResPath.Generated` ids instead of repeated address strings.
- Confirm loaded instances/assets have a release path.
- Confirm module resources can move with their module without rewriting callers.
- For detailed review, read [resource-checklist](./references/resource-checklist.md).
