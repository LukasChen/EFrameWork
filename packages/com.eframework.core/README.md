<p align="center">
  <img src="Editor/ProjectBootstrap/Assets/EFrameLogo.png" alt="EFrame logo" width="160">
</p>

# EFrame Core

EFrame Core is a reusable Unity game framework package extracted from `CozyBloomUnity/Assets/EFrame`.

## Contents

- `Runtime/`: stable framework runtime modules such as UI host/binding/controller/handle/transition/layout/QScroller, basic audio, data storage, events, coroutines, assets, and utility services.
- `Editor/`: editor tooling for project bootstrap, Addressables, audio setup, UI binding, and scroller helpers.
- `AIWorkspace~/`: package-shipped AI rules, skills, managed blocks, and manifest used by EFrame AI sync.
- `Tools~/`: package-shipped PowerShell tools for AI sync, cold start, updater installation, and project health checks.
- `Plugins/`: remaining bundled third-party dependencies that still ship with this package.

## Architecture Layers

EFrame is organized as three layers:

- **Core**: stable infrastructure and the minimal initialization path in `com.eframework.core`.
- **Extension**: optional packages such as virtual list, UI extras, effects, and debug console.
- **Samples**: fixture-backed templates and optional module entry points that help a project adopt Core or try extensions without making them mandatory dependencies.

Core ships a **Basic** template as the minimal runnable project skeleton. It is the maintained form of the current initialization template and creates `StartUp.unity`, `ProcedureLauncher`, `ProcedureHome`, `HomeView`, the Home UI prefab, audio setup, Addressables groups, and generated `ResPath` setup without depending on optional extension packages.

The editable Basic source is a full Unity fixture at `test-fixtures/EFrameBasicTemplate`; its `Assets` folder is synced into `Editor/Templates/Basic` with `tools/Sync-EFrameBasicTemplate.ps1`. Use `EFrame Tools/项目初始化向导` and choose `Initialize / Repair Project` rather than importing folders manually.

The **Extension Showcase** is an optional Project Module installed to `Assets/Modules/EFrameExtensionShowcase/`. It is copied from `Editor/Templates/Modules/EFrameExtensionShowcase`, can be installed or refreshed from the initialization window, and can be set as the StartUp Procedure entrance. Framework maintainers edit the runnable source module in `test-fixtures/EFrameShowcaseUnity` and sync it back to the package template with `tools/Sync-EFrameShowcaseTemplate.ps1`. Installing Showcase also installs the focused extension packages it demonstrates. Its prefab-backed runtime UI keeps only UI Virtual List and Debug Console demo entries; UI Virtual List opens the module-owned `QVirtualListShowcaseWindow.prefab`.

The Simple Game Demo direction is intentionally outside this core repository and should be handled later as a separate git repository.

Unity package dependencies include Addressables, UGUI, Input System, Universal RP, and Unity Newtonsoft.Json. TextMeshPro functionality is supplied through UGUI. Core includes a lightweight fallback tween backend, while DOTween is an optional project-installed adapter target because DOTween Free is not a Unity registry dependency.

Complex UI widgets, UI helper/animation components, presentation effects, debug tools, event-driven audio authoring, haptics adapters, and third-party integrations should live in optional extension packages. Virtual list/grid controls are provided by `com.eframework.ui.virtual-list`, helper UI controls by `com.eframework.ui-extras`, presentation effects by `com.eframework.effects`, and the runtime debug console by `com.eframework.debug-console`.

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

Core audio is limited to `IAudioService` / `AudioManager`: Music/SFX playback, mixer volume controls, `AudioClipAsset` loading, SFX pooling, debouncing, and music crossfade. Event-to-audio mapping and haptics are no longer core services.

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

## Tween Backend And DOTween Adapter

`com.eframework.core` uses `EFrameTween` for framework-owned runtime transitions, scroll movement, and audio fades. The default backend is a package-owned fallback implementation, so Basic projects run without DOTween.

DOTween remains supported as an optional adapter:

- This package does not bundle DOTween, DOTweenPro, or DemiLib
- Do not add an unresolvable DOTween package name to `dependencies`; Unity package dependencies must resolve through the Unity registry, a configured scoped registry, or an explicit package source in the consuming project
- Keep DOTween as a normal project plugin under `Assets`
- Use `EFrame Tools/项目初始化向导` -> `Extensions` -> `DOTween Adapter` -> `Apply` to enable the adapter
- Enabling the adapter adds the `EFRAME_USE_DOTWEEN` scripting define and initializes `Assets/Resources/DOTweenSettings.asset` when DOTween is present
- Disable the adapter before removing DOTween from a project
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
