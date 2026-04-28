# EFrameWork Initial Extraction

Date: 2026-04-28

## Source

Extracted from local project:

```text
C:/Users/Ethan/Desktop/Work/project/CozyBloom/CozyBloomUnity/Assets/EFrameWork
```

Public source reference:

```text
https://github.com/ethanhubin/CozyBloom/tree/main/CozyBloomUnity/Assets/EFrameWork
```

## Target Layout

The framework was extracted as a Unity Package Manager package:

```text
packages/com.eframework.core/
  Runtime/
  Editor/
  Plugins/
  Samples~/
  package.json
  README.md
  CHANGELOG.md
  THIRD_PARTY_NOTICES.md
```

## Runtime Modules Observed

- Asset
- Audio
- Base
- CoroutineModule
- DataStorage
- Effect
- Event
- GMTools
- UI
- Utils
- Vibration

## Bundled Dependencies Preserved

The source `Runtime/EFrameWork.asmdef` uses GUID references to bundled plugin assemblies, so the initial extraction preserves the plugin folders and their `.meta` files.

Bundled plugin areas include:

- Demigiant / DOTween / DOTweenPro
- NiceVibrations
- TextMesh Pro support assets
- UGF
- UniTask
- Unity Ingame Debug Console
- JsonNet-Lite

## UPM Adjustments

- `Samples` was moved to `Samples~` for Unity Package Manager conventions.
- `Editor/EFrameAudioSetup.cs` was updated so its mixer template source path points to `Packages/com.eframework.core/Editor/Settings/EFrameAudioMixerSettings.mixer`.
- Root repo manifests were updated to list `com.eframework.core` as the first framework package.

## Follow-Up Work

- Validate package import in a clean Unity project.
- Decide whether bundled third-party plugins should remain vendored or become external package dependencies.
- Normalize license notices before public redistribution.
- Split optional modules into additional packages only after the core package compiles cleanly.
