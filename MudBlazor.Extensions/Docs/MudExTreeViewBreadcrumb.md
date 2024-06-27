### MudExTreeViewBreadcrumb Documentation

## Overview

`MudExTreeViewBreadcrumb<T>` is a specialized Blazor component for rendering a tree view in breadcrumb style. It inherits from `MudExTreeViewBase<T>` and provides additional features like hierarchical menu representation and customizable menu properties.

## Usage

Here is an example of how to use `MudExTreeViewBreadcrumb`:

```razor
<MudExTreeViewBreadcrumb @bind-SelectedNode="_selectedNode"
                         MenuAnchorOrigin="Origin.BottomLeft"
                         Items="@Entries">
</MudExTreeViewBreadcrumb>
```

In this example:
- `@bind-SelectedNode` binds the selected node to a field.
- `Parameters` dictionary is used to pass additional parameters.
- `Items` provides the data for the tree view.

## Properties

### MenuAnchorOrigin

- **Type:** `Origin`
- **Default:** `Origin.BottomRight`
- **Description:** Sets the anchor origin point to determine where the popover will open from.

### MenuTransformOrigin

- **Type:** `Origin`
- **Default:** `Origin.TopLeft`
- **Description:** Sets the transform origin point for the popover.

### MenuMaxHeight

- **Type:** `int`
- **Default:** `500`
- **Description:** Max height of the menu. When this is reached, overflow will be scrollable.

## Conclusion

`MudExTreeViewBreadcrumb<T>` provides a flexible way to render tree views as breadcrumbs.
