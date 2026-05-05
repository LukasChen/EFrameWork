# EFrame Data Table Checklist

## Required Shape

- Serializable data payload
- `DataTable<TData>` table wrapper
- Parameterless constructor
- Stable `StorageKey`
- Registration through `Context.Data.RegisterTable<T>()` inside framework-aware code, or `EFrame.Data.RegisterTable<T>()` at startup/non-injected entry points

## Mutation Rules

- Use table-owned properties or methods
- Mark dirty only when values change
- Keep raw `Data` out of external mutable access
- Keep saves explicit when business needs immediate persistence

## Migration And Diagnostics

- Add `CurrentVersion` when schema evolves
- Implement `Migrate(data, fromVersion)` for old files
- Read `LastLoadResult.Status` / `ReasonCode` for backup, migration, or default fallback handling
- Read `LastSaveResult` to distinguish written, skipped, and failed saves
