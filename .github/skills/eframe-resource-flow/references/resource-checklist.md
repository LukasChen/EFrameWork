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
- Only direct child files of selected directories should generate `ResPath.Generated`; subdirectories must be selected explicitly
- Player builds should fail if managed entries, selected generated addresses, or `ResPath.Generated` are stale
- Use `EFrame Tools/Addressables/Sync Groups And Generate ResPath` to choose ResPath directories, inspect the report window, or run a repair/manual verification action

## Loading

- Prefer `ResPath.Generated`
- Use stable handwritten `ResPath` constants only when generation is not available
- Use `AssetReference` when inspector-authored references are more appropriate
- Load through `EFrame.Current.Assets`

## Release

- Procedure-owned assets release on `OnLeave`
- UI-owned assets release when controller/handle closes or disposes
- Spawned instance handles release when the runtime object is destroyed or returned to pool
