EFrameWork framework baseline:

- Follow the synced EFrame framework contract in `.github/instructions/eframe-instructions.md`.
- Use `.github/eframe/EFRAME_AI_API_INDEX.md` to look up stable EFrame APIs before generating or refactoring framework code.
- Use `.github/skills/eframe-*` for multi-step EFrame workflows such as feature bootstrap, UI, data tables, resources, and guideline audits.
- Use `EFrameProcedure` / `EFrameProcedureComponent` for Procedure state transitions, preloading, and lifecycle orchestration.
- Route UI through `EFrame.Current.UI` / `IUIService`, `UIControllerBase`, `UIViewHandle`, and `QUI`; do not hand-build persistent top-level runtime Canvas or EventSystem objects.
- Use generated `ResPath.Generated` asset ids in runtime code. `AssetReference` fields are editor authoring inputs, not business-layer asset identifiers.
- Let EFrame editor automation manage Addressables entries under `Assets/App/Res`, `Assets/Scenes`, and `Assets/Modules`.
- Keep project-specific rules outside this managed block. EFrame sync updates only the marked block and EFrame managed `eframe-*` files.
