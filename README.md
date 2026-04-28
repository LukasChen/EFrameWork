# EFrameWork

Unity game framework repository.

## AI Workspace Support

This repository now contains a reusable AI workspace layer for Unity projects that adopt EFrameWork.

- Framework AI rules live under `.github/`
- Sync and installer scripts live under `tools/`
- Setup and upgrade flow is documented in [EFRAME_AI_SETUP.md](EFRAME_AI_SETUP.md)

To sync the framework AI layer into a project root:

```powershell
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Force
```

To cold-start a new project in one command:

```powershell
.\tools\Initialize-EFrameColdStart.ps1 -TargetRoot "D:\YourUnityProject" -Force
```

To generate the minimal startup Procedure/bootstrap placeholder code only:

```powershell
.\tools\Initialize-EFrameBootstrapCode.ps1 -TargetRoot "D:\YourUnityProject" -RootNamespace "YourGame"
```

To install a project-side updater script:

```powershell
.\tools\Install-EFrameAIProjectUpdater.ps1 -TargetRoot "D:\YourUnityProject"
```

Inside Unity Editor, you can also open `EFrame Tools/项目初始化向导` to create `StartUp.unity`, the `Boot` object structure, and trigger AI/bootstrap initialization from a single window.

## DOTween Dependency

`com.eframework.core` uses DOTween directly in several runtime components. Consumer projects should install DOTween into the project `Assets` before using tween-enabled EFrameWork features.

- Recommended: install DOTween as a normal project plugin under `Assets`
- Then use `EFrame Tools/项目初始化向导` to create or open `Assets/Resources/DOTweenSettings.asset`
- Do not rely on configuring DOTween through a package-local copy

## TextMeshPro Dependency

`com.eframework.core` also uses TextMeshPro directly and now expects the official Unity package dependency `com.unity.textmeshpro` instead of a framework-bundled copy.