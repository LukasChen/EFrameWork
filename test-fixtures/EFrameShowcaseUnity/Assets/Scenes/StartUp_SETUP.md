# StartUp Scene Setup

Create a startup scene named StartUp.unity under Assets/Scenes and keep the root structure minimal:

    StartUp
    |- Boot
    |  |- EFrameProcedureComponent
    |  |- EFrameComponent
    |- Main Camera

Required setup:

1. Add EFrameProcedureComponent and EFrameComponent to Boot
2. Assign the same EFrameProcedureComponent instance to EFrameComponent.m_procedureComponent
3. Set Main Camera and UI camera references on EFrameComponent
4. Register these procedures in EFrameProcedureComponent:
    - GameApp.Procedure.ProcedureLauncher
    - GameApp.Procedure.ProcedureHome
    - GameApp.Modules.EFrameExtensionShowcase.Procedure.ProcedureEFrameExtensionShowcaseEntry
5. Set the entrance procedure type name to GameApp.Modules.EFrameExtensionShowcase.Procedure.ProcedureEFrameExtensionShowcaseEntry for the showcase fixture.
6. Keep HomeView and EFrameExtensionShowcaseView prefabs in the fixture source so startup UI is copied and debugged as Unity assets.

Generated placeholder code:

- Assets/App/Runtime/Generated/Res/ResPath.Generated.cs (generated after Addressables groups are ready)
- Assets/App/Runtime/Procedure/ProcedureLauncher.cs
- Assets/App/Runtime/Procedure/ProcedureHome.cs
- Assets/App/Runtime/UI/Views/HomeView.cs
- Assets/App/Runtime/UI/Controllers/HomeViewController.cs
- Assets/App/Res/UI/Panels/Home/HomeView.prefab
- Assets/App/Res/Bootstrap/README.md
- Assets/Modules/EFrameExtensionShowcase/README.md
- Assets/Modules/EFrameExtensionShowcase/Runtime/Procedure/ProcedureEFrameExtensionShowcaseEntry.cs
- Assets/Modules/EFrameExtensionShowcase/Runtime/UI/EFrameExtensionShowcaseView.cs
- Assets/Modules/EFrameExtensionShowcase/Runtime/UI/EFrameExtensionShowcaseController.cs
- Assets/Modules/EFrameExtensionShowcase/Runtime/Demos/EFrameExtensionShowcaseDemo.cs
- Assets/Modules/EFrameExtensionShowcase/Runtime/Demos/EFrameExtensionShowcaseRegistry.cs
- Assets/Modules/EFrameExtensionShowcase/Res/UI/Panels/EFrameExtensionShowcase/EFrameExtensionShowcaseView.prefab

Next refactors:

- Replace showcase prefab visuals with final demo art style while keeping QUIBinding names stable
- Put startup-only resources under Assets/App/Res/Bootstrap
- Use EFrameExtensionShowcase as the reference example for optional extension demo modules
- Use generated constants under ResPath.Generated.* for runtime resource references
