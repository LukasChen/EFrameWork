# EFrame Basic Template Fixture

This fixture is the editable source for the package Basic initialization template. It is a complete Unity project so maintainers can open and debug the Basic startup path directly.

Edit runtime template files under:

```text
test-fixtures/EFrameBasicTemplate/Assets/
```

Then sync them into the package template with:

```powershell
tools/Sync-EFrameBasicTemplate.ps1
```

The sync script copies only the `Assets` folder into the package template and converts `.cs` / `.cs.meta` to `.cs.txt` / `.cs.txt.meta` so Unity does not compile template source files inside the package. `Packages` and `ProjectSettings` stay in the fixture for real Unity debugging.

Basic intentionally stops at `ProcedureHome` showing `HomeView`; it must not include `SampleModule` or optional extension package references.
