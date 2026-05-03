# StartUp Scene Setup

The Basic template copies a ready-to-open `StartUp.unity` scene under `Assets/Scenes`.

Root structure:

    StartUp
    |- Boot
    |  |- EFrameProcedureComponent
    |  |- EFrameComponent
    |- Main Camera

Configured procedures:

- GameApp.Procedure.ProcedureLauncher
- GameApp.Procedure.ProcedureHome

The entrance procedure is `GameApp.Procedure.ProcedureLauncher`. It immediately enters `ProcedureHome`, preloads `HomeView`, and shows the initialization information screen.

Generated template files:

- Assets/Scenes/StartUp.unity
- Assets/App/Runtime/Generated/Res/ResPath.Generated.cs
- Assets/App/Runtime/Procedure/ProcedureLauncher.cs
- Assets/App/Runtime/Procedure/ProcedureHome.cs
- Assets/App/Runtime/UI/Views/HomeView.cs
- Assets/App/Runtime/UI/Controllers/HomeViewController.cs
- Assets/App/Res/UI/Panels/Home/HomeView.prefab
- Assets/App/Res/Bootstrap/README.md

After import, run `EFrame Tools/Addressables/Sync Groups And Generate ResPath` if Unity has not already synced managed resources.
