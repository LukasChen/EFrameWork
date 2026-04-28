# EFrameWork Core

EFrameWork Core is a reusable Unity game framework package extracted from `CozyBloomUnity/Assets/EFrameWork`.

## Contents

- `Runtime/`: framework runtime modules such as UI, audio, data storage, events, effects, assets, and utility services.
- `Editor/`: editor tooling for audio setup, UI binding, list/scroller helpers, and effect debug editors.
- `Plugins/`: remaining bundled third-party dependencies that still ship with this package.
- `Samples~/`: sample assets imported from the source project.

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
- Do not add another framework-local TextMeshPro copy unless you are migrating legacy assets intentionally

## Source

Initial extraction source:

```text
https://github.com/ethanhubin/CozyBloom/tree/main/CozyBloomUnity/Assets/EFrameWork
```
