# Grid and Layout Components

Advanced grid and layout components for creating flexible and responsive layouts.

## MudExGrid

Enhanced grid component with advanced features like drag-and-drop, sorting, and filtering.

### Basic Usage

```razor
<MudExGrid Items="@items" />
```

### Features

- Drag and drop row reordering
- Column sorting and filtering
- Inline editing
- Custom cell templates
- Pagination support
- Export to Excel/CSV
- Responsive design
- Grouping and aggregation

### Example

```razor
@code {
    private List<Person> items = new();
}

<MudExGrid Items="@items" 
           T="Person"
           Sortable="true"
           Filterable="true"
           AllowDragDrop="true">
    <Columns>
        <MudExGridColumn Property="p => p.Name" Title="Name" />
        <MudExGridColumn Property="p => p.Email" Title="Email" />
        <MudExGridColumn Property="p => p.Age" Title="Age" />
    </Columns>
</MudExGrid>
```

### Drag and Drop Example

Enable drag and drop for row reordering:

```razor
<MudExGrid Items="@items" 
           T="Person"
           AllowDragDrop="true"
           OnRowDropped="@OnRowDropped">
</MudExGrid>

@code {
    private async Task OnRowDropped(MudExGridRowDroppedEventArgs<Person> args)
    {
        // Handle row reordering
        items.Remove(args.Item);
        items.Insert(args.NewIndex, args.Item);
    }
}
```

[View Live Demo](https://www.mudex.org/mud-ex-grid-enhanced)

## MudExDockLayout

Flexible dock layout system for creating resizable, dockable panel layouts.

### Basic Usage

```razor
<MudExDockLayout>
    <MudExDockItem Dock="DockPosition.Left" Title="Sidebar">
        <!-- Sidebar content -->
    </MudExDockItem>
    <MudExDockItem Dock="DockPosition.Fill" Title="Main">
        <!-- Main content -->
    </MudExDockItem>
</MudExDockLayout>
```

### Features

- Multiple dock positions (Left, Right, Top, Bottom, Fill)
- Resizable panels
- Collapsible panels
- Tabs for multiple panels in same position
- Drag and drop panel reorganization
- State persistence
- Responsive design

### Example

```razor
<MudExDockLayout Height="600px" SaveState="true">
    <MudExDockItem Dock="DockPosition.Left" 
                   Title="Explorer" 
                   Width="250px"
                   Collapsible="true">
        <MudList>
            <MudListItem Icon="@Icons.Material.Filled.Folder">
                Documents
            </MudListItem>
            <MudListItem Icon="@Icons.Material.Filled.Folder">
                Projects
            </MudListItem>
        </MudList>
    </MudExDockItem>
    
    <MudExDockItem Dock="DockPosition.Bottom" 
                   Title="Output" 
                   Height="200px"
                   Collapsible="true">
        <MudText>Output console...</MudText>
    </MudExDockItem>
    
    <MudExDockItem Dock="DockPosition.Fill" Title="Editor">
        <!-- Main editor content -->
    </MudExDockItem>
</MudExDockLayout>
```

[View Live Demo](https://www.mudex.org/dock-layout)

## MudExSplitPanel

Split panel component for creating resizable divided areas.

### Basic Usage

```razor
<MudExSplitPanel>
    <LeftPanel>
        <!-- Left content -->
    </LeftPanel>
    <RightPanel>
        <!-- Right content -->
    </RightPanel>
</MudExSplitPanel>
```

### Features

- Horizontal or vertical split
- Resizable divider
- Minimum and maximum sizes
- Collapsible panels
- Multiple nested splits
- Responsive design

### Example

```razor
<MudExSplitPanel Orientation="SplitOrientation.Horizontal" 
                 LeftSize="300px"
                 MinLeftSize="200px"
                 MaxLeftSize="500px">
    <LeftPanel>
        <MudPaper Class="pa-4" Elevation="0">
            <MudText Typo="Typo.h6">Navigation</MudText>
            <!-- Navigation content -->
        </MudPaper>
    </LeftPanel>
    <RightPanel>
        <MudPaper Class="pa-4" Elevation="0">
            <MudText Typo="Typo.h6">Content</MudText>
            <!-- Main content -->
        </MudPaper>
    </RightPanel>
</MudExSplitPanel>
```

## MudExSplitter

Splitter component for manually adjustable panel sizes.

### Basic Usage

```razor
<MudExSplitter>
    <MudExSplitPanelItem>Panel 1</MudExSplitPanelItem>
    <MudExSplitPanelItem>Panel 2</MudExSplitPanelItem>
    <MudExSplitPanelItem>Panel 3</MudExSplitPanelItem>
</MudExSplitter>
```

### Features

- Multiple panels support
- Adjustable dividers
- Horizontal or vertical orientation
- Proportional or fixed sizing
- Collapsible panels

## MudExDivider

Enhanced divider component with additional styling options.

### Basic Usage

```razor
<MudExDivider />
```

### Features

- Vertical or horizontal orientation
- Custom colors and thickness
- Text labels
- Icons
- Dashed or dotted styles

### Example

```razor
<MudExDivider Label="Section 1" />

<MudExDivider Orientation="Orientation.Vertical" 
              Color="Color.Primary" 
              Thickness="2" />

<MudExDivider DividerType="DividerType.Dashed" 
              Label="OR" 
              Icon="@Icons.Material.Filled.MoreHoriz" />
```

## Best Practices

- Use `MudExGrid` for tabular data with advanced features
- Use `MudExDockLayout` for complex application layouts
- Use `MudExSplitPanel` for simple two-panel layouts
- Set appropriate minimum sizes to prevent panels from becoming too small
- Save layout state for better user experience
- Test responsive behavior on different screen sizes

## See Also

- [Other Components](other-components.md)
- [CSS Builder](../utilities/css-builder.md)
- [Size Utilities](../utilities/size-utils.md)
