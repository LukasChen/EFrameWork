# EFrameWork Framework Rules

This project uses EFrameWork. Treat this block as the framework-supplied baseline; keep project-specific Claude Code rules outside this managed block.

- Read `.github/instructions/eframe-instructions.md` for the synced EFrame framework contract.
- Read the relevant `.github/skills/eframe-*/SKILL.md` before multi-step EFrame feature, UI, data, resource, or audit work.
- Use `EFrameProcedure` and `EFrameProcedureComponent` for state transitions, preload work, and lifecycle orchestration.
- Route UI through `EFrame.Current.UI` / `IUIService`, `UIControllerBase`, `UIViewHandle`, and `QUI`.
- Use generated `ResPath.Generated` asset ids for runtime resource loading.
- Let EFrame editor automation maintain managed Addressables groups, addresses, labels, and generated paths.
- Do not edit EFrame managed instruction or skill files to express project rules. Put project rules anywhere the project owns outside this managed block.
