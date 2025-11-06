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

### Utility Components

<div class="grid cards" markdown>

-   **[MudExAppLoader](app-loader.md)**
    
    Application loading indicator with customizable appearance and behavior.

-   **[MudExSvg](svg.md)**
    
    SVG component for rendering and manipulating SVG graphics.

</div>

## All Components List

| Component | Description | Category |
|-----------|-------------|----------|
| `MudExObjectEdit` | Auto-generate forms from objects | Data Editing |
| `MudExObjectEditForm` | Form wrapper for object editing | Data Editing |
| `MudExObjectEditDialog` | Dialog for object editing | Data Editing |
| `MudExStructuredDataEditor` | Edit JSON/XML/YAML data | Data Editing |
| `MudExFileDisplay` | Display file contents | File Management |
| `MudExFileDisplayZip` | Display ZIP archive contents | File Management |
| `MudExFileDisplayDialog` | Dialog for file display | File Management |
| `MudExUploadEdit` | Advanced file upload | File Management |
| `MudExTreeView` | Enhanced tree view | Navigation |
| `MudExTreeViewBreadcrumb` | Tree view breadcrumbs | Navigation |
| `MudExAppLoader` | Application loader | Utility |
| `MudExSvg` | SVG graphics component | Utility |
| `MudExDialog` | Enhanced dialog | Utility |
| `MudExTextField` | Enhanced text field | Forms |
| `MudExCultureSelect` | Culture selection | Forms |
| `MudExAudioPlayer` | Audio player | Media |
| `MudExSplitPanel` | Split panel layout | Layout |
| `MudExSplitter` | Content splitter | Layout |
| `MudExHtmlEdit` | HTML editor | Editing |

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
