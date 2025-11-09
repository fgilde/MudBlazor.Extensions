# Other Components

Additional utility components provided by MudBlazor.Extensions.

!!! info "Component Categories"
    For detailed documentation on specific component types, see:
    
    - [Picker Components](pickers.md) - Color, Icon, Font, Enum pickers
    - [Grid and Layout](grid-layout.md) - Grid, Dock Layout, Split Panels
    - [Display and Media](display-media.md) - Image Viewer, Code View, Markdown
    - [Form Inputs](form-inputs.md) - Text Fields, Checkboxes, Selects

## MudExList

Enhanced list component with additional features.

### Basic Usage

```razor
<MudExList>
    <MudExListItem>Item 1</MudExListItem>
    <MudExListItem>Item 2</MudExListItem>
    <MudExListItem>Item 3</MudExListItem>
</MudExList>
```

### Features

- Nested lists
- Icons and avatars
- Click events
- Selection support
- Virtualization for large lists

## MudExCardList

Card-based list component for displaying items as cards.

### Basic Usage

```razor
<MudExCardList Items="@items" T="MyItem">
    <ItemTemplate Context="item">
        <MudCard>
            <MudCardContent>@item.Name</MudCardContent>
        </MudCard>
    </ItemTemplate>
</MudExCardList>
```

### Features

- Grid or flex layout
- Responsive columns
- Custom card templates
- Sorting and filtering

## MudExGroupBox

Group box component for organizing related content.

### Basic Usage

```razor
<MudExGroupBox Title="Settings">
    <!-- Content -->
</MudExGroupBox>
```

### Features

- Collapsible content
- Custom header
- Border styling
- Icon support

## MudExPopover

Popover component for displaying content on hover or click.

### Basic Usage

```razor
<MudExPopover>
    <Activator>
        <MudButton>Show Popover</MudButton>
    </Activator>
    <Content>
        <MudText>Popover content</MudText>
    </Content>
</MudExPopover>
```

### Features

- Multiple trigger modes (hover, click, focus)
- Positioning options
- Arrow indicator
- Auto-close on outside click

## MudExToggleableSearch

Toggleable search component for space-efficient search functionality.

### Basic Usage

```razor
<MudExToggleableSearch @bind-Value="@searchText" OnSearch="@HandleSearch" />
```

### Features

- Expandable search input
- Icon animation
- Clear button
- Debounced search

## MudExSlideBar

Slide-in panel component for navigation or additional content.

### Basic Usage

```razor
<MudExSlideBar @bind-Open="@isOpen" Position="SlideBarPosition.Left">
    <MudText>Slide bar content</MudText>
</MudExSlideBar>
```

### Features

- Multiple positions (left, right, top, bottom)
- Overlay support
- Swipe gestures
- Animated transitions

## MudExTaskBar

Task bar component for application-level navigation.

### Basic Usage

```razor
<MudExTaskBar>
    <MudExTaskBarItem Icon="@Icons.Material.Filled.Home" Text="Home" />
    <MudExTaskBarItem Icon="@Icons.Material.Filled.Settings" Text="Settings" />
</MudExTaskBar>
```

### Features

- Icon and text display
- Active state indication
- Click events
- Responsive design

## MudExVirtualize

Virtualization component for efficient rendering of large lists.

### Basic Usage

```razor
<MudExVirtualize Items="@largeList" Context="item">
    <MudExVirtualItem>
        <MudText>@item.Name</MudText>
    </MudExVirtualItem>
</MudExVirtualize>
```

### Features

- Efficient rendering of large datasets
- Smooth scrolling
- Dynamic item heights
- Placeholder support

## MudExComponentPropertyGrid

Property grid component for editing component properties.

### Basic Usage

```razor
<MudExComponentPropertyGrid Component="@myComponent" />
```

### Features

- Automatic property discovery
- Type-appropriate editors
- Property categorization
- Read-only mode

## MudExApiView

API documentation viewer component.

### Basic Usage

```razor
<MudExApiView ApiUrl="https://api.example.com/swagger.json" />
```

### Features

- Swagger/OpenAPI support
- Interactive API testing
- Request/response examples
- Authentication support

## MudExSpeechToTextButton

Speech recognition button for voice input.

### Basic Usage

```razor
<MudExSpeechToTextButton OnTextRecognized="@HandleText" />
```

### Features

- Browser speech recognition
- Language selection
- Continuous or single recognition
- Visual feedback

## MudExCaptureButton

Button component for capturing photos/videos from camera.

### Basic Usage

```razor
<MudExCaptureButton OnCapture="@HandleCapture" />
```

### Features

- Camera access
- Photo or video capture
- Preview before submit
- Multiple capture modes

## MudExColorBubble

Color bubble display component.

### Basic Usage

```razor
<MudExColorBubble Color="@color" Size="Size.Large" />
```

### Features

- Visual color representation
- Multiple sizes
- Border options
- Click events

## Cloud File Pickers

Components for integrating cloud storage providers.

### MudExDropBoxFilePicker

```razor
<MudExDropBoxFilePicker OnFileSelected="@HandleFile" AppKey="@appKey" />
```

### MudExGoogleFilePicker

```razor
<MudExGoogleFilePicker OnFileSelected="@HandleFile" ClientId="@clientId" />
```

### MudExOneDriveFilePicker

```razor
<MudExOneDriveFilePicker OnFileSelected="@HandleFile" ClientId="@clientId" />
```

### Features

- OAuth authentication
- File browsing
- Multiple file selection
- Download support

## Best Practices

- Use appropriate components for the use case
- Consider mobile responsiveness
- Test accessibility features
- Leverage virtualization for large datasets
- Follow MudBlazor theming conventions

## See Also

- [Picker Components](pickers.md)
- [Grid and Layout](grid-layout.md)
- [Display and Media](display-media.md)
- [Form Inputs](form-inputs.md)
- [Extensions](../extensions/index.md)
