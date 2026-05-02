# StartUp Scene Setup

Create a startup scene named StartUp.unity under Assets/Scenes and keep the root structure minimal:

`	ext
StartUp
|- Boot
|  |- EFrameProcedureComponent
|  |- EFrameComponent
|- Main Camera
`

Required setup:

1. Add EFrameProcedureComponent and EFrameComponent to Boot
2. Assign the same EFrameProcedureComponent instance to EFrameComponent.m_procedureComponent
3. Set Main Camera and UI camera references on EFrameComponent
4. Register these procedures in EFrameProcedureComponent:
    - $RootNamespace.Procedure.ProcedureLauncher
    - $RootNamespace.Procedure.ProcedureHome
    - $RootNamespace.Modules.SampleModule.Procedure.ProcedureSampleModuleEntry
5. Set the entrance procedure type name to $RootNamespace.Procedure.ProcedureLauncher
6. In Unity Editor, run Copy Sample UI Prefab Templates once so HomeView and SampleModuleMainView prefabs exist.

Generated placeholder code:

- Assets/App/Runtime/Common/ResPath.cs (namespace anchor; generated resource constants live in ResPath.Generated)
- Assets/App/Runtime/Generated/Res/ResPath.Generated.cs (generated after Addressables groups are ready)
- Assets/App/Runtime/Procedure/ProcedureLauncher.cs
- Assets/App/Runtime/Procedure/ProcedureHome.cs
- Assets/App/Runtime/UI/Views/HomeView.cs
- Assets/App/Runtime/UI/Controllers/HomeViewController.cs
- Assets/App/Res/UI/Panels/Home/HomeView.prefab (copied from the package template in Unity Editor)
- Assets/App/Res/Bootstrap/README.md
- Assets/Modules/SampleModule/README.md
- Assets/Modules/SampleModule/Runtime/Procedure/ProcedureSampleModuleEntry.cs
- Assets/Modules/SampleModule/Runtime/UI/Views/SampleModuleMainView.cs
- Assets/Modules/SampleModule/Runtime/UI/Controllers/SampleModuleMainViewController.cs
- Assets/Modules/SampleModule/Res/UI/Panels/SampleModuleMain/SampleModuleMainView.prefab (copied from the package template in Unity Editor)

Next refactors:

- Replace the sample prefab visuals with your project's final art style while keeping QUIBinding names stable
- Put startup-only resources under Assets/App/Res/Bootstrap
- Use SampleModule as the reference example when creating your first real module
- Use generated constants under ResPath.Generated.* for runtime resource references
