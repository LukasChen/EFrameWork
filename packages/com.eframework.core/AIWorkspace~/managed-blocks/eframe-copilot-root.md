EFrame framework baseline:

- Follow the synced EFrame framework contract in `.github/instructions/eframe-instructions.md`.
- Use `.github/eframe/EFRAME_AI_API_INDEX.md` to look up stable EFrame APIs before generating or refactoring framework code.
- Use `.github/skills/eframe-*` for multi-step EFrame workflows such as feature bootstrap, UI, data tables, resources, directory decisions, and guideline audits.
- When the user asks a question or requests analysis, treat it as discussion first: do only read-only inspection needed to answer, then confirm the proposed approach before editing files, running initialization/install/refresh actions, or otherwise changing the project.
- Procedure, Context, UI, data, resource, directory, and Unity serialized editing rules are governed by the synced instruction and specialized EFrame skills; do not redefine those details in this root entry.
- Keep project-specific rules outside this managed block. EFrame sync updates only the marked block and EFrame managed `eframe-*` files.
