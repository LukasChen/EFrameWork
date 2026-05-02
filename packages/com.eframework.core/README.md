# EFrame Core

EFrame Core is a reusable Unity game framework package extracted from `CozyBloomUnity/Assets/EFrame`.

## Contents

- `Runtime/`: framework runtime modules such as UI, audio, data storage, events, effects, assets, and utility services.
- `Editor/`: editor tooling for audio setup, UI binding, list/scroller helpers, and effect debug editors.
- `AIWorkspace~/`: package-shipped AI rules, skills, managed blocks, and manifest used by EFrame AI sync.
- `Tools~/`: package-shipped PowerShell tools for AI sync, cold start, updater installation, and project health checks.
- `Plugins/`: remaining bundled third-party dependencies that still ship with this package.
- `Samples~/`: sample assets imported from the source project.

Unity package dependencies include Addressables, UGUI, Input System, Universal RP, and Unity Newtonsoft.Json. TextMeshPro functionality is supplied through UGUI.

Bundled plugin versions:

- UniTask `2.5.10`

## Runtime Access Model

Human-facing docs live in `Documentation~/`. AI-facing sync contracts live in `AIWorkspace~/`.

To sync EFrame AI guidance into a consuming project, run `Tools~/Initialize-EFrameAI.ps1` from this package root or use `EFrame Tools/AI/Sync Workspace` inside Unity.

For the framework-level public API reference, see [../../API_REFERENCE.md](../../API_REFERENCE.md).

For the browsable HTML docs site, see [Documentation~/index.html](Documentation~/index.html).

EFrame exposes lightweight static shortcuts for common runtime services. `EFrame.Current` still returns the active `EFrameContext` when the full service bundle is needed.

```csharp
EFrame.UI.SetInteractive(false);
EFrame.Audio.PlaySfx(clickClip);
var playerData = EFrame.Data.GetTable<PlayerDataTable>();
```

Framework-created views, controllers, and `EFrameBehaviour` components receive the active context automatically:

```csharp
public sealed class HomeController : UIControllerBase<HomeView>
{
    protected override string AssetPath => ResPath.Generated.UI.HomeView;

    protected override void OnViewOpened()
    {
        Context.Audio.PlaySfx("Audio/UI/Open");
    }
}
```

## Resource Lifetime

Resources placed under the EFrame managed directories are synchronized automatically:

- `Assets/App/Res/...`
- `Assets/Scenes/...`
- `Assets/Modules/<ModuleName>/Res/...`
- `Assets/Modules/<ModuleName>/Scenes/...`

Do not manually maintain Addressables entries for those assets. EFrame editor automation owns the group, address, and `eframe-managed` label. `ResPath.Generated` is generated from the directory subtrees selected in the managed resource report window, so it can stay focused on code-driven load entry points instead of every dependency asset. Builds run a preflight sync/validation pass before the player is created.

Use `EFrame Tools/Addressables/Sync Groups And Generate ResPath` to open the managed resource report window when you want to choose ResPath directories, inspect Addressables entries, generated ResPath members, sync status, and directory convention hints.

New resource service APIs are asynchronous and return explicit handles:

```csharp
var handle = await EFrame.Assets.LoadAsync<GameObject>("UI/HomeView");
try
{
    var prefab = handle.Asset;
}
finally
{
    handle.Dispose();
}

var instance = await EFrame.Assets.InstantiateAsync("Effects/CoinFly", parent);
instance.Dispose();
```

Use `EFrame.Assets` as the runtime resource entry outside framework-aware types.

## Install

Use Unity Package Manager with this Git repository, targeting this package path:

```text
https://github.com/ethanhubin/EFrame.git?path=/packages/com.eframework.core
```

## DOTween Requirement

`com.eframework.core` uses DOTween directly in runtime code. Install DOTween into the consuming project's `Assets` before using tween-enabled framework components.

- This package does not bundle DOTween, DOTweenPro, or DemiLib anymore
- Keep DOTween as a normal project plugin under `Assets`
- Use `EFrame Tools/项目初始化向导` to create or open `Assets/Resources/DOTweenSettings.asset`
- Do not expect DOTween Utility Panel module management to work against a package-local copy

## URP Requirement

`com.eframework.core` requires Universal RP. The runtime UI camera contract uses URP overlay-camera APIs to attach the persistent UI camera to the selected `EFrameSceneCamera`, so consuming projects should configure a URP pipeline asset before relying on scene-camera UI overlay binding.

## TextMeshPro Dependency

`com.eframework.core` uses TextMeshPro directly in runtime and editor code. In current Unity versions, TextMeshPro functionality is included in `com.unity.ugui`, so consumer projects should rely on the UGUI package instead of the deprecated standalone `com.unity.textmeshpro` package.

- This package no longer redistributes the old framework-local TextMeshPro plugin copy
- Keep TextMeshPro sourced from `com.unity.ugui`
- Do not add another framework-local TextMeshPro copy

## JSON Dependency

`com.eframework.core` uses Newtonsoft.Json in its data storage module. The package declares `com.unity.nuget.newtonsoft-json`, so Unity Package Manager installs the official Unity Newtonsoft.Json package automatically for consuming projects.

- This package does not redistribute JsonNet-Lite
- Keep Newtonsoft.Json sourced from `com.unity.nuget.newtonsoft-json`
- Do not add a second JsonNet-Lite or Newtonsoft.Json DLL copy under `Assets`

## Source

Initial extraction source:

```text
https://github.com/ethanhubin/CozyBloom/tree/main/CozyBloomUnity/Assets/EFrame
```
