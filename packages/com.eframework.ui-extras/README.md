# EFrame UI Extras

Optional EFrame UI extension package for reusable helper components that are useful in projects but are not part of the core UI host/binding/controller chain.

## Contents

- `Components/CheckableButton`: single UGUI Button with checked, focused, pressed, and disabled visual states.
- `Components/Tabbar`: TabBar selector built from `CheckableButton` items.
- `Components/EmptyRayCasterGraphic`: invisible raycast target graphic.
- `UIHelper`: button/toggle helpers, TextMeshPro curve helpers, rounded image effect, smooth fill, and UIBuilder utilities.
- `UIAnimation`: reusable enable/fade/move/window open animation behaviours.

Runtime namespaces:

```csharp
using EFramework.Extensions.UI.Extras.Components;
using EFramework.Extensions.UI.Extras.UIHelper;
using EFramework.Extensions.UI.Extras.UIAnimation;
```

## Dependencies

- `com.eframework.core`
- `com.unity.ugui`
