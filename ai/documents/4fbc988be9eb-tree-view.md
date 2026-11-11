# MudExTreeView & MudExTreeViewBreadcrumb

Enhanced tree view components with advanced features like breadcrumb navigation, search, and flexible selection modes.

## MudExTreeView Features

- ✅ **Hierarchical Display** - Display nested data structures
- ✅ **Multiple Selection Modes** - Single, multiple, or checkbox selection
- ✅ **Search & Filter** - Built-in search functionality
- ✅ **Breadcrumb Integration** - Automatic breadcrumb navigation
- ✅ **Customizable** - Flexible templates and styling

## Basic Usage

```razor
<MudExTreeView Items="@treeItems" />

@code {
    private List<TreeItem> treeItems = new()
    {
        new TreeItem { Text = "Folder 1" },
        new TreeItem { Text = "Folder 2" }
    };
}
```

## MudExTreeViewBreadcrumb

Display breadcrumb navigation for tree structures:

```razor
<MudExTreeViewBreadcrumb Items="@breadcrumbItems" />
```

## Live Demo

[Tree View Demo](https://www.mudex.org/tree-view)
