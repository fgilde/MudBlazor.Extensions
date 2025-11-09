# Components Overview

MudBlazor.Extensions provides a comprehensive set of components that extend and enhance the MudBlazor component library. These components are designed to simplify common tasks and provide advanced functionality out of the box.

## Component Categories

### Data Editing Components

<div class="grid cards" markdown>

-   **[MudExObjectEdit](object-edit.md)**
    
    Automatically generate UI for object editing with built-in validation support. Supports DataAnnotations and Fluent validation.

-   **[MudExStructuredDataEditor](structured-data-editor.md)**
    
    Edit structured data like JSON, XML, or YAML with automatic UI generation and validation.

</div>

### File Management Components

<div class="grid cards" markdown>

-   **[MudExFileDisplay](file-display.md)**
    
    Display various file types with automatic format detection and preview capabilities.

-   **[MudExUploadEdit](upload-edit.md)**
    
    Advanced file upload with drag & drop, cloud integration, and extensive customization options.

</div>

### Navigation Components

<div class="grid cards" markdown>

-   **[MudExTreeView](tree-view.md)**
    
    Enhanced tree view with breadcrumb navigation, search, and advanced selection modes.

-   **[MudExTreeViewBreadcrumb](tree-view.md#breadcrumb)**
    
    Breadcrumb navigation component for tree structures.

</div>

### Picker Components

<div class="grid cards" markdown>

-   **[MudExColorPicker](pickers.md#mudexcolorpicker)**
    
    Enhanced color picker with various formats and presets.

-   **[MudExIconPicker](pickers.md#mudexiconpicker)**
    
    Icon selection from MudBlazor icons library with search.

-   **[MudExFontSelect](pickers.md#mudexfontselect)**
    
    Font selection with preview and Google Fonts integration.

-   **[MudExEnumSelect](pickers.md#mudexenumselect)**
    
    Automatic enum selection with display name support.

</div>

### Layout Components

<div class="grid cards" markdown>

-   **[MudExGrid](grid-layout.md#mudexgrid)**
    
    Advanced grid with drag-drop, sorting, and filtering.

-   **[MudExDockLayout](grid-layout.md#mudexdocklayout)**
    
    Flexible dock layout with resizable, dockable panels.

-   **[MudExSplitPanel](grid-layout.md#mudexsplitpanel)**
    
    Split panel for creating resizable divided areas.

</div>

### Display and Media Components

<div class="grid cards" markdown>

-   **[MudExImageViewer](display-media.md#mudeximageviewer)**
    
    Advanced image viewer with zoom, pan, and rotation.

-   **[MudExCodeView](display-media.md#mudexcodeview)**
    
    Code display with syntax highlighting.

-   **[MudExMarkdown](display-media.md#mudexmarkdown)**
    
    Render GitHub-flavored markdown content.

-   **[MudExGravatarCard](display-media.md#mudexgravatarcard)**
    
    User card with Gravatar integration.

</div>

### Form and Input Components

<div class="grid cards" markdown>

-   **[MudExTextField](form-inputs.md#mudextextfield)**
    
    Enhanced text field with advanced validation.

-   **[MudExSelect](form-inputs.md#mudexselect)**
    
    Enhanced select with search and grouping.

-   **[MudExChipSelect](form-inputs.md#mudexchipselect)**
    
    Chip-based multiple selection component.

-   **[MudExColorEdit](form-inputs.md#mudexcoloredit)**
    
    Color editing with visual picker integration.

</div>

### Utility Components

<div class="grid cards" markdown>

-   **[MudExAppLoader](app-loader.md)**
    
    Application loading indicator with customizable appearance and behavior.

-   **[MudExSvg](svg.md)**
    
    SVG component for rendering and manipulating SVG graphics.

-   **[Other Components](other-components.md)**
    
    Additional utility components like lists, cards, and more.

</div>

## All Components List

| Component | Description | Documentation |
|-----------|-------------|---------------|
| `MudExObjectEdit` | Auto-generate forms from objects | [Object Edit](object-edit.md) |
| `MudExStructuredDataEditor` | Edit JSON/XML/YAML data | [Structured Data](structured-data-editor.md) |
| `MudExFileDisplay` | Display file contents | [File Display](file-display.md) |
| `MudExUploadEdit` | Advanced file upload | [Upload Edit](upload-edit.md) |
| `MudExTreeView` | Enhanced tree view | [Tree View](tree-view.md) |
| `MudExColorPicker` | Color selection picker | [Pickers](pickers.md#mudexcolorpicker) |
| `MudExIconPicker` | Icon selection picker | [Pickers](pickers.md#mudexiconpicker) |
| `MudExFontSelect` | Font selection | [Pickers](pickers.md#mudexfontselect) |
| `MudExEnumSelect` | Enum selection | [Pickers](pickers.md#mudexenumselect) |
| `MudExGrid` | Advanced data grid | [Grid & Layout](grid-layout.md#mudexgrid) |
| `MudExDockLayout` | Dockable layout system | [Grid & Layout](grid-layout.md#mudexdocklayout) |
| `MudExSplitPanel` | Split panel layout | [Grid & Layout](grid-layout.md#mudexsplitpanel) |
| `MudExImageViewer` | Image viewer with zoom | [Display & Media](display-media.md#mudeximageviewer) |
| `MudExCodeView` | Code syntax highlighting | [Display & Media](display-media.md#mudexcodeview) |
| `MudExMarkdown` | Markdown renderer | [Display & Media](display-media.md#mudexmarkdown) |
| `MudExGravatarCard` | User card with Gravatar | [Display & Media](display-media.md#mudexgravatarcard) |
| `MudExTextField` | Enhanced text field | [Form Inputs](form-inputs.md#mudextextfield) |
| `MudExSelect` | Enhanced select | [Form Inputs](form-inputs.md#mudexselect) |
| `MudExChipSelect` | Chip-based selection | [Form Inputs](form-inputs.md#mudexchipselect) |
| `MudExHtmlEdit` | HTML editor | [Form Inputs](form-inputs.md#mudexhtmledit) |
| `MudExList` | Enhanced list | [Other](other-components.md#mudexlist) |
| `MudExCardList` | Card-based list | [Other](other-components.md#mudexcardlist) |
| `MudExPopover` | Popover component | [Other](other-components.md#mudexpopover) |
| `MudExAppLoader` | Application loader | [App Loader](app-loader.md) |
| `MudExSvg` | SVG graphics | [SVG](svg.md) |

## Quick Start

To use any component, first ensure you have the proper imports in your `_Imports.razor`:

```csharp
@using MudBlazor.Extensions
@using MudBlazor.Extensions.Components
@using MudBlazor.Extensions.Components.ObjectEdit
```

Then simply add the component to your page:

```razor
<MudExObjectEditForm Value="@myModel" OnValidSubmit="@HandleSubmit" />
```

## Common Features

Most MudBlazor.Extensions components share these features:

### Theming Support

All components respect MudBlazor's theming system and automatically adapt to light/dark mode:

```csharp
// Components automatically use theme colors
<MudExUploadEdit Color="Color.Primary" />
```

### Validation

Data editing components support multiple validation approaches:

- DataAnnotations
- FluentValidation
- Custom validation logic

### Accessibility

Components are built with accessibility in mind:

- Proper ARIA attributes
- Keyboard navigation support
- Screen reader compatibility

### Responsive Design

All components are responsive and work across different screen sizes:

```razor
<MudExObjectEdit WrapInMudGrid="true" />
```

## Component Usage Patterns

### Basic Usage

Most components can be used with minimal configuration:

```razor
<MudExFileDisplay FileName="document.pdf" Url="@fileUrl" />
```

### Advanced Configuration

Components provide extensive customization options:

```razor
<MudExUploadEdit 
    AllowMultiple="true"
    MaxFileSize="10485760"
    AllowedExtensions="@(new[] { ".pdf", ".docx", ".jpg" })"
    AllowDropBox="true"
    AllowGoogleDrive="true"
    @bind-Files="selectedFiles" />
```

### Dialog Integration

Many components have dialog variants for modal interactions:

```csharp
await dialogService.EditObject(myObject, "Edit Object");
await MudExFileDisplayDialog.Show(dialogService, file);
```

## Performance Considerations

MudBlazor.Extensions components are optimized for performance:

- **Virtual scrolling** for large lists
- **Lazy loading** for heavy content
- **Efficient rendering** to minimize re-renders
- **Background processing** for intensive operations

## Browser Compatibility

Components are tested and supported on:

- ✅ Chrome (latest)
- ✅ Firefox (latest)
- ✅ Edge (latest)
- ✅ Safari (latest)

## Next Steps

Explore individual component documentation to learn more:

1. Start with [Object Edit](object-edit.md) for form generation
2. Try [Upload Edit](upload-edit.md) for file handling
3. Explore [Dialog Extensions](../extensions/dialog-extensions.md) for enhanced dialogs

## Need Help?

- 📖 Check component-specific documentation
- 💡 View examples in the [live demo](https://mudex.azurewebsites.net/)
- 🐛 Report issues on [GitHub](https://github.com/fgilde/MudBlazor.Extensions/issues)
- 💬 Ask questions in [discussions](https://github.com/fgilde/MudBlazor.Extensions/discussions)
