# EFrame Basic

Basic is the minimal runnable EFrame project skeleton used by the project initialization flow.

Install it from Unity with `EFrame Tools/项目初始化向导` and choose `Install Basic Sample / Import Basic Template`, or run `Full Initialize Project` for the complete cold-start chain. The machine template source lives under `Editor/Templates/Basic` so the initializer can copy it directly into a consuming project.

Basic installs the standard startup shape:

- `Assets/Scenes/StartUp.unity`
- `Assets/App/Runtime/Procedure/ProcedureLauncher.cs`
- `Assets/App/Runtime/Procedure/ProcedureHome.cs`
- `Assets/App/Runtime/UI/Views/HomeView.cs`
- `Assets/App/Runtime/UI/Controllers/HomeViewController.cs`
- `Assets/App/Res/UI/Panels/Home/HomeView.prefab`
- `Assets/Modules/SampleModule/...`
- Addressables, UI sorting layer, audio, DOTween settings, and generated `ResPath` setup

Basic intentionally depends only on `com.eframework.core` and core-standard Unity package dependencies. Optional EFrame extension packages are demonstrated by the separate Extension Showcase module.
