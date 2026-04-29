# EFrameWork Core

EFrameWork Core is a reusable Unity game framework package extracted from `CozyBloomUnity/Assets/EFrameWork`.

## Contents

- `Runtime/`: framework runtime modules such as UI, audio, data storage, events, effects, assets, and utility services.
- `Editor/`: editor tooling for audio setup, UI binding, list/scroller helpers, and effect debug editors.
- `Plugins/`: remaining bundled third-party dependencies that still ship with this package.
- `Samples~/`: sample assets imported from the source project.

Bundled plugin versions:

- UniTask `2.5.10`

## Runtime Access Model

For the current UI runtime structure and handle-based usage model, see [../../UI_FRAMEWORK_GUIDE.md](../../UI_FRAMEWORK_GUIDE.md).

EFrameWork now uses `EFrame.Current` as the only static runtime entry. The static class is only an entry point; services are owned by `EFrameContext`.

```csharp
EFrame.Current.UI.SetInteractive(false);
EFrame.Current.Audio.PlaySfx(clickClip);
var playerData = EFrame.Current.Data.GetTable<PlayerDataTable>();
```

Framework-created views, controllers, and `EFrameBehaviour` components receive the active context automatically:

```csharp
public sealed class HomeController : UIControllerBase<HomeView>
{
    protected override string AssetPath => ResPath.Generated.UI.HomeView;

    protected override void OnViewCreated()
    {
        Context.Audio.PlaySfx("Audio/UI/Open");
    }
}
```

## Resource Lifetime

New resource service APIs are asynchronous and return explicit handles:

```csharp
var handle = await EFrame.Current.Assets.LoadAsync<GameObject>("UI/HomeView");
try
{
    var prefab = handle.Asset;
}
finally
{
    handle.Dispose();
}

var instance = await EFrame.Current.Assets.InstantiateAsync("Effects/CoinFly", parent);
instance.Dispose();
```

Use `EFrame.Current.Assets` as the runtime resource entry.

## Install

Use Unity Package Manager with this Git repository, targeting this package path:

```text
https://github.com/ethanhubin/EFrameWork.git?path=/packages/com.eframework.core
```

## DOTween Requirement

`com.eframework.core` uses DOTween directly in runtime code. Install DOTween into the consuming project's `Assets` before using tween-enabled framework components.

- This package does not bundle DOTween, DOTweenPro, or DemiLib anymore
- Keep DOTween as a normal project plugin under `Assets`
- Use `EFrame Tools/项目初始化向导` to create or open `Assets/Resources/DOTweenSettings.asset`
- Do not expect DOTween Utility Panel module management to work against a package-local copy

## TextMeshPro Dependency

`com.eframework.core` uses TextMeshPro directly in runtime and editor code. The package now declares `com.unity.textmeshpro` as a Unity package dependency, so consumer projects should rely on the official Unity TextMeshPro package instead of a bundled copy.

- This package no longer redistributes the old framework-local TextMeshPro plugin copy
- Keep TextMeshPro sourced from `com.unity.textmeshpro`
- Do not add another framework-local TextMeshPro copy

## JSON Dependency

`com.eframework.core` uses Newtonsoft.Json in its data storage module. The package declares `com.unity.nuget.newtonsoft-json`, so Unity Package Manager installs the official Unity Newtonsoft.Json package automatically for consuming projects.

- This package does not redistribute JsonNet-Lite
- Keep Newtonsoft.Json sourced from `com.unity.nuget.newtonsoft-json`
- Do not add a second JsonNet-Lite or Newtonsoft.Json DLL copy under `Assets`

## Source

Initial extraction source:

```text
https://github.com/ethanhubin/CozyBloom/tree/main/CozyBloomUnity/Assets/EFrameWork
```
