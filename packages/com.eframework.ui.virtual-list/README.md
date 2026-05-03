# EFrame UI Virtual List

Optional EFrame UI package for pooled virtual collection controls.

## Contents

- `QVirtualListView`: variable-size single-axis virtual list.
- `QVirtualGridView`: fixed-size virtual grid.
- `QVirtualListItem`: pooled item component.
- `IQVirtualItemAdapter` / `IQVirtualListAdapter`: adapter contracts for binding visible items.

`QVirtualGridView` can keep its configured scroll direction and enable auto wrap on the cross axis. In vertical mode, auto wrap calculates the column count from viewport width; in horizontal mode, it calculates the row count from viewport height.

Runtime namespace:

```csharp
using EFramework.Extensions.UI.VirtualList;
```

## Dependencies

- `com.eframework.core`
- `com.unity.ugui`
