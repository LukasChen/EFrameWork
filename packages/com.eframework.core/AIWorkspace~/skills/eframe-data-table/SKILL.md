---
name: eframe-data-table
description: 'Create, refactor, or audit EFrame persistent data tables, StorageKey values, dirty tracking, save/load behavior, migrations, LastLoadResult, and LastSaveResult usage. Use when adding gameplay settings, player progress, inventory, options, other persistent runtime data, or when the task mentions 数据表接入, 存档接入, 设置数据, 玩家进度, or 数据迁移.'
argument-hint: 'Describe the data shape, storage key, load/save timing, and whether migration from old data is needed.'
user-invocable: true
capabilities:
  - eframe.data.table
  - eframe.data.persistence
  - eframe.data.migration
  - eframe.data.dirty-state
owns:
  - DataTable model and mutation boundary
  - stable StorageKey contract
  - load/save result interpretation
delegatesTo:
  - eframe-guideline-audit
outputs:
  - serializable data model
  - dirty-aware table API
  - migration and recovery notes
forbiddenPatterns:
  - deriving StorageKey from mutable class or namespace names
  - mutating raw data outside table-owned methods
  - parsing log text for load/save decisions
---

# EFrame Data Table

## Workflow

1. Model persistent state in a serializable data class and expose behavior through a `DataTable<TData>` table.
2. Give every table a stable, unique `StorageKey`. Never derive persistence identity from class name or namespace.
3. Keep a public parameterless constructor and register through the framework data service, preferably `Context.Data.RegisterTable<T>()` inside framework-aware code and `EFrame.Data.RegisterTable<T>()` only at startup or non-injected entry points.
4. Read tables through the framework data service, preferably `Context.Data.GetTable<T>()` inside framework-aware code; do not create a second business singleton for the same state.
5. Expose mutations through table properties or methods. Use `SetValue(...)`, `Mutate(...)`, or equivalent table-owned helpers so dirty state changes only when data really changes.
6. If schema can change, declare `CurrentVersion` and implement `Migrate(data, fromVersion)`.
7. Use `LastLoadResult` and `LastSaveResult` for UI prompts, telemetry, retries, and recovery decisions. Do not parse log messages or assume `Save()` always writes.
8. Let the framework save on pause/quit by default; call `Context.Data.SaveAll()` / `EFrame.Data.SaveAll()` or table-level `Save()` only at explicit business checkpoints.

## Output Checks

- Confirm no external code mutates the raw data object directly.
- Confirm duplicate or empty `StorageKey` values fail review.
- Confirm migration handles existing unversioned raw files as `version 0` when compatibility is needed.
- For detailed review, read [data-table-checklist](./references/data-table-checklist.md).
