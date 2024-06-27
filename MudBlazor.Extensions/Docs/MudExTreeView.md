# MudExTreeView Documentation

## Overview

`MudExTreeView<T>` is a Blazor component that dynamically renders a tree view based on the specified `ViewMode`. The actual component that gets rendered is determined by the `ViewMode`, and all parameters are passed down to this component. Note that if a parameter is not set, the underlying component might have different default values. Parameters that are unique to specific components (e.g., `MudExTreeViewBreadcrumb`) can still be set using the `Parameters` dictionary.

## Usage

Here is an example of how to use `MudExTreeView`:

```razor
<MudExTreeView @bind-SelectedNode="_selectedNode"
               Parameters="@(new Dictionary<string, object> { { nameof(MudExCardList<object>.HoverMode), MudExCardHoverMode.Simple } })"
               ViewMode="_treeViewMode"
               Items="@Entries">
</MudExTreeView>
```

In this example:
- `@bind-SelectedNode` binds the selected node to a field.
- `Parameters` dictionary is used to pass additional parameters.
- `ViewMode` specifies the rendering mode.
- `Items` provides the data for the tree view.

## Properties

### ViewMode

- **Type:** `TreeViewMode`
- **Default:** `TreeViewMode.Horizontal`
- **Description:** Controls how the tree view will be rendered. 

### Parameters

- **Type:** `IDictionary<string, object>`
- **Description:** Parameters for the component based on `ViewMode`. This allows passing additional parameters that might not be part of the common base class but are required by specific view modes.

## TreeViewMode Enum

The `TreeViewMode` enum defines the available rendering modes:

### Default

- **Description:** Renders the tree view in the default mode.
- **Component:** `MudExTreeViewDefault<>`

### Horizontal

- **Description:** Renders the tree view in a horizontal layout.
- **Component:** `MudExTreeViewHorizontal<>`

### Breadcrumb

- **Description:** Renders the tree view as a breadcrumb.
- **Component:** `MudExTreeViewBreadcrumb<>`

### List

- **Description:** Renders the tree view as a list.
- **Component:** `MudExTreeViewList<>`

### FlatList

- **Description:** Renders the tree view as a flat list.
- **Component:** `MudExTreeViewFlatList<>`

### CardGrid

- **Description:** Renders the tree view as a card grid.
- **Component:** `MudExTreeViewCardGrid<>`

## Dynamic Parameter Handling

The component dynamically handles parameters based on the specified `ViewMode`. Here's how it works:

1. **Determine Component Type:**
   The component type is determined based on the `ViewMode` using the `GetComponentForViewMode` method.
   
2. **Retrieve Parameters:**
   The `GetParameters` method collects parameters that are compatible with the determined component type. It ensures that only relevant parameters are passed and applies any additional parameters from the `Parameters` dictionary.

3. **Pass Parameters:**
   All collected parameters are passed to the underlying component, ensuring that both common and unique parameters are handled appropriately.

## Example

Let's say you want to render a tree view as a breadcrumb and need to pass a unique parameter to `MudExTreeViewBreadcrumb`. You can do this using the `Parameters` dictionary:

```razor
<MudExTreeView @bind-SelectedNode="_selectedNode"
               Parameters="@(new Dictionary<string, object> { { nameof(MudExTreeViewBreadcrumb<object>.SomeUniqueParameter), someValue } })"
               ViewMode="TreeViewMode.Breadcrumb"
               Items="@Entries">
</MudExTreeView>
```

In this example, `SomeUniqueParameter` is specific to the `MudExTreeViewBreadcrumb` component and is passed using the `Parameters` dictionary.

## Conclusion

`MudExTreeView<T>` provides a flexible way to render tree views in various modes. By utilizing the `ViewMode` and `Parameters`, you can customize the rendering and behavior to fit your specific needs. This dynamic approach ensures that the component adapts to different requirements without sacrificing usability.

but however if you want cou can also directly use the specific Components like 
[`MudExTreeViewBreadcrumb`](http://www.mudex.org/MudExTreeViewBreadcrumb), 
[`MudExTreeViewCardGrid`](http://www.mudex.org/MudExTreeViewCardGrid), 
[`MudExTreeViewDefault`](http://www.mudex.org/MudExTreeViewDefault), 
[`MudExTreeViewFlatList`](http://www.mudex.org/MudExTreeViewFlatList), 
[`MudExTreeViewHorizontal`](http://www.mudex.org/MudExTreeViewHorizontal), 
[`MudExTreeViewList`](http://www.mudex.org/MudExTreeViewList) 
instead of using `MudExTreeView` and `ViewMode`.