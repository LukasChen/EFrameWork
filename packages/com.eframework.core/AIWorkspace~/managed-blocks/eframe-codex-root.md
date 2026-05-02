## EFrame Framework Rules

This project uses EFrame. Treat this block as the framework-supplied baseline; keep project-specific AI rules outside this managed block.

- Follow the synced EFrame framework contract in `.github/instructions/eframe-instructions.md`.
- Use `.github/eframe/EFRAME_AI_API_INDEX.md` to look up stable EFrame APIs before generating or refactoring framework code.
- For feature scaffolding, UI work, data tables, resource flow, or EFrame audits, read the matching `.github/skills/eframe-*/SKILL.md` before editing.
- When the user asks a question or requests analysis, treat it as discussion first: do only read-only inspection needed to answer, then confirm the proposed approach before editing files, running initialization/install/refresh actions, or otherwise changing the project.
- Use `EFrameProcedure` and `EFrameProcedureComponent` for state transitions, preload work, and lifecycle orchestration.
- Route business UI through `EFrame.UI` and `UIControllerBase<TGeneratedView>`; generated View access classes come from the UI Binding tool, controllers use `CurrentView`, and normal business code should not own `IUIService`, `QUI`, or `UIViewHandle` directly.
- Runtime asset ids should come from generated `ResPath.Generated`; editor `AssetReference` fields may exist for authoring, but business logic should resolve to generated asset ids.
- Treat `Assets/App/Res`, `Assets/Scenes`, and `Assets/Modules` as EFrame managed Addressables resource areas and let EFrame editor automation maintain groups, addresses, labels, and generated paths.
- Do not edit EFrame managed instruction or skill files to express project rules. Put project rules anywhere the project owns outside this managed block.
