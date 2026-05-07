# EFrame UI Extras

Optional EFrame UI extension package for reusable controls that are useful in projects but are not part of the core UI host/binding/controller chain.

## Contents

- `Components/CheckableButton`: single UGUI Button with checked/toggle semantics.
- `Components/Tabbar`: TabBar selector built from `CheckableButton` items.
- `Components/EmptyRayCasterGraphic`: invisible raycast target graphic.
- `Interaction/UIInteractionGuard`: prevents repeated Button/Toggle interaction through cooldown or one-shot locking.
- `Interaction/UIInteractionReporter`: dispatches `UIInteractionEvent` for Button and Toggle analytics hooks.
- `Interaction/UIInteractionFeedback`: handles normal, hover, focus, pressed, checked, disabled, and enable-time visual feedback.
- `Graphics/UISmoothFill`: smooth `Image.fillAmount` transitions.
- `Graphics/UIRoundedRectImage`: rounded rectangle mesh effect for UGUI Images.
- `VirtualList/QVirtualListView`: variable-size single-axis virtual list.
- `VirtualList/QVirtualGridView`: fixed-size virtual grid with optional auto wrap.
- `VirtualList/QVirtualListItem`: pooled item component.
- `VirtualList/IQVirtualItemAdapter` / `IQVirtualListAdapter`: adapter contracts for binding visible items.

`QVirtualGridView` can keep its configured scroll direction and enable auto wrap on the cross axis. In vertical mode, auto wrap calculates the column count from viewport width; in horizontal mode, it calculates the row count from viewport height.

Virtual item templates may be inactive. Pooled instances are activated internally when first cloned or reused. If an item template is authored as a child of the view hierarchy, keep the source template inactive or outside the viewport.

Runtime namespaces:

```csharp
using EFramework.Extensions.UI.Extras.Components;
using EFramework.Extensions.UI.Extras.Graphics;
using EFramework.Extensions.UI.Extras.Interaction;
using EFramework.Extensions.UI.VirtualList;
```

## Dependencies

- `com.eframework.core`
- `com.unity.ugui`
