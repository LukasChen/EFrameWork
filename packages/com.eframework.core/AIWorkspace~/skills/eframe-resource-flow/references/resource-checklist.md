# EFrame Resource Flow Checklist

## Directory Mapping

- App shared resources: `Assets/App/Res/...`
- Scene files: `Assets/Scenes` or `Assets/Modules/<Name>/Scenes`
- Scene-dependent assets: `Assets/App/Res/SceneAssets`
- Module resources: `Assets/Modules/<Name>/Res/...`

## Addressables Automation

- Managed directory Addressables entries are framework-owned
- Do not manually edit group, address, or label values for managed resources
- Import, move, and delete operations should auto-sync Addressables groups
- Selected directories generate `ResPath.Generated` entries for resources under their directory tree
- Player builds should fail if managed entries, selected generated addresses, or `ResPath.Generated` drift
- Use `EFrame Tools/Addressables/Sync Groups And Generate ResPath` to choose ResPath source directory trees, inspect the report window, or run a repair/manual verification action

## Loading

- Use `ResPath.Generated` for runtime resource ids
- Do not add handwritten resource path constants for managed resources
- Use `AssetReference` when inspector-authored references are more appropriate
- Load through `EFrame.Assets`

## Release

- Procedure-owned assets release on `OnLeave`
- UI-owned assets release when controller/handle closes or disposes
- Spawned instance handles release when the runtime object is destroyed or returned to pool
