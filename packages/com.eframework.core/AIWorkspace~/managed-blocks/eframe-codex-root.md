## EFrame Framework Rules

This project uses EFrame. Treat this managed block as the framework-supplied routing baseline, not as a full rule source. Keep project-specific AI rules outside this block.

- Follow the synced EFrame framework contract in `.github/instructions/eframe-instructions.md`.
- Use `.github/eframe/EFRAME_AI_API_INDEX.md` to look up stable EFrame APIs before generating or refactoring framework code.
- When the user asks a question or requests analysis, treat it as discussion first: do only read-only inspection needed to answer, then confirm the proposed approach before editing files, running initialization/install/refresh actions, or otherwise changing the project.
- Do not edit EFrame managed instruction or skill files to express project rules. Put project rules anywhere the project owns outside this managed block.
