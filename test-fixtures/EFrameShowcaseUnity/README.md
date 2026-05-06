# EFrame Showcase Unity Fixture

This fixture is a Unity project for maintaining and debugging the Extension Showcase module.

Open this folder directly in Unity:

```text
test-fixtures/EFrameShowcaseUnity
```

The editable source module lives at:

```text
Assets/Modules/EFrameExtensionShowcase/
```

Edit and debug the module here as normal Unity `.cs` files with any module resources. Before releasing the package template, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ../../tools/Sync-EFrameShowcaseTemplate.ps1
```

The sync script copies the fixture module into `packages/com.eframework.core/Editor/Templates/Modules/EFrameExtensionShowcase`, renaming `.cs` files to `.cs.txt` for package template installation.

Addressables uses `Default Build Script` for player content. For normal editor iteration, keep the Addressables Play Mode Script on `Use Asset Database (fastest)`. If you switch Play Mode Script to `Use Existing Build`, build Addressables player content from the Addressables Groups window before entering Play Mode.
