# EFrame Framework Rules

This project uses EFrame. Treat this block as the framework-supplied baseline; keep project-specific Claude Code rules outside this managed block.

- Read `.github/instructions/eframe-instructions.md` for the synced EFrame framework contract.
- Use `.github/eframe/EFRAME_AI_API_INDEX.md` to look up stable EFrame APIs before generating or refactoring framework code.
- Read the relevant `.github/skills/eframe-*/SKILL.md` before multi-step EFrame feature, UI, data, resource, or audit work.
- Use `EFrameProcedure` and `EFrameProcedureComponent` for state transitions, preload work, and lifecycle orchestration.
- Route business UI through `EFrame.UI` and `UIControllerBase<TGeneratedView>`; generated View access classes come from the UI Binding tool, controllers use `CurrentView`, and normal business code should not own `IUIService`, `QUI`, or `UIViewHandle` directly.
- Use generated `ResPath.Generated` asset ids for runtime resource loading.
- Let EFrame editor automation maintain managed Addressables groups, addresses, labels, and generated paths.
- Do not edit EFrame managed instruction or skill files to express project rules. Put project rules anywhere the project owns outside this managed block.
