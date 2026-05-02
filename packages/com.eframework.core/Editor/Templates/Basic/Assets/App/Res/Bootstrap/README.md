# Bootstrap Resources

Put startup-critical resources here when they must be available before the main flow enters normal on-demand loading.

Recommended contents:

- loading screen prefabs
- startup config assets
- always-resident shared atlases used before the home page finishes loading

Addressables note:

- assets under `Assets/App/Res/Bootstrap` are mapped to the default `App Bootstrap Group`
- avoid putting feature-specific resources here; keep this folder small and stable
