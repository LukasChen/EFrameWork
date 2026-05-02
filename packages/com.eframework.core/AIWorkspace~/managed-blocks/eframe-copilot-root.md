EFrame framework baseline:

- Follow the synced EFrame framework contract in `.github/instructions/eframe-instructions.md`.
- Use `.github/eframe/EFRAME_AI_API_INDEX.md` to look up stable EFrame APIs before generating or refactoring framework code.
- Use `.github/skills/eframe-*` for multi-step EFrame workflows such as feature bootstrap, UI, data tables, resources, and guideline audits.
- Keep Procedure state transitions, runtime UI, asset ids, and managed Addressables flow inside EFrame framework contracts instead of ad-hoc project conventions.
- In framework-aware runtime types, prefer injected `Context`; use `EFrame.UI`, `EFrame.Assets`, `EFrame.Data`, and other static shortcuts only for startup, static entry points, or non-injected code.
- Keep project-specific rules outside this managed block. EFrame sync updates only the marked block and EFrame managed `eframe-*` files.
