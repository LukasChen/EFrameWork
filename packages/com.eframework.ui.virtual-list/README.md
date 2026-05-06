# EFrame UI Virtual List

Optional EFrame UI package for pooled virtual collection controls.

## Contents

- `QVirtualListView`: variable-size single-axis virtual list.
- `QVirtualGridView`: fixed-size virtual grid.
- `QVirtualListItem`: pooled item component.
- `IQVirtualItemAdapter` / `IQVirtualListAdapter`: adapter contracts for binding visible items.

`QVirtualGridView` can keep its configured scroll direction and enable auto wrap on the cross axis. In vertical mode, auto wrap calculates the column count from viewport width; in horizontal mode, it calculates the row count from viewport height.

## Usage Notes

- Item templates may be inactive. Pooled instances are activated internally when first cloned or reused.
- If an item template is authored as a child of the view hierarchy, keep the source template inactive or outside the viewport. The controls manage pooled instances, not the visibility of the source template object returned by the adapter.
- `SetAdapter(...)` can be called while the view is inactive or before the first layout pass. The controls defer visible-item refresh briefly when the viewport has not received a valid size yet.
- Call `RefreshLayout()` after external layout changes that alter the viewport size without triggering Unity's normal RectTransform dimension callbacks.

Runtime namespace:

```csharp
using EFramework.Extensions.UI.VirtualList;
```

## Dependencies

- `com.eframework.core`
- `com.unity.ugui`
