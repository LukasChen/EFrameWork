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

To install a project-side updater script:

```powershell
.\tools\Install-EFrameAIProjectUpdater.ps1 -TargetRoot "D:\YourUnityProject"
```