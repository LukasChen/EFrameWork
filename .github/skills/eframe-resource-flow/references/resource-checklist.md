# EFrame Resource Flow Checklist

## Directory Mapping

- App shared resources: `Assets/App/Res/...`
- Scene files: `Assets/Scenes` or `Assets/Modules/<Name>/Scenes`
- Scene-dependent assets: `Assets/App/Res/SceneAssets`
- Module resources: `Assets/Modules/<Name>/Res/...`

## Loading

- Prefer `ResPath.Generated`
- Use stable handwritten `ResPath` constants only when generation is not available
- Use `AssetReference` when inspector-authored references are more appropriate
- Load through `EFrame.Current.Assets`

## Release

- Procedure-owned assets release on `OnLeave`
- UI-owned assets release when controller/handle closes or disposes
- Spawned instance handles release when the runtime object is destroyed or returned to pool
