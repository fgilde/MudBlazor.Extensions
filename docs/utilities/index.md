# Utilities Overview

MudBlazor.Extensions provides various utility classes to simplify common tasks, from styling and color manipulation to JSON handling and theme management.

## Available Utilities

### Styling Utilities

- **[Color Utilities](color-utils.md)** - Color manipulation, conversion, and generation
- **[CSS Builder](css-builder.md)** - Dynamic CSS class building with conditional logic
- **[Style Builder](style-builder.md)** - Dynamic inline style building
- **[Size Utilities](size-utils.md)** - Size calculations, conversions, and parsing

### Helper Utilities

- **[Helper Utilities](helpers.md)** - Collection of helper classes for various tasks:
  - **MudExJsonHelper** - JSON serialization and manipulation
  - **MudExFonts** - Font management and loading
  - **MudExThemeHelper** - Theme creation and manipulation
  - **BrowserFileExt** - File handling extensions
  - **EnumHelper** - Enum utility methods
  - **ComponentHelper** - Component reflection and creation
  - **LocalizationStringHelper** - Localization support

## Quick Examples

### CSS Builder

Build CSS classes dynamically with conditional logic:

```csharp
var css = MudExCssBuilder.Default
    .AddClass("my-class")
    .AddClass("active", when: isActive)
    .AddClass("disabled", when: isDisabled)
    .Build();
```

[Learn more about CSS Builder →](css-builder.md)

### Style Builder

Create inline styles dynamically:

```csharp
var style = MudExStyleBuilder.Default
    .AddStyle("color", primaryColor)
    .AddStyle("background", bgColor, when: hasBackground)
    .AddStyle("font-size", "16px")
    .Build();
```

[Learn more about Style Builder →](style-builder.md)

### Color Utilities

Manipulate and convert colors:

```csharp
// Convert between formats
var hex = MudExColorUtils.ToHex(color);
var rgb = MudExColorUtils.ToRgb(hexColor);

// Lighten or darken colors
var lighter = MudExColorUtils.Lighten(color, 0.2);
var darker = MudExColorUtils.Darken(color, 0.2);
```

[Learn more about Color Utilities →](color-utils.md)

### JSON Helper

Work with JSON data:

```csharp
// Serialize/deserialize
var json = MudExJsonHelper.Serialize(myObject);
var obj = MudExJsonHelper.Deserialize<MyType>(json);

// Pretty print
var formatted = MudExJsonHelper.PrettyPrint(json);
```

[Learn more about Helper Utilities →](helpers.md)

### File Extensions

Handle browser files easily:

```csharp
// Get icon for file type
var icon = BrowserFileExt.IconForFile(file);

// Format file size
var sizeString = file.FormatSize(); // "2.5 MB"

// Convert to data URL
var dataUrl = await file.ToDataUrlAsync();
```

[Learn more about Helper Utilities →](helpers.md)

## Common Use Cases

### Dynamic Styling

Combine CSS and Style builders for dynamic component styling:

```csharp
var cssClass = MudExCssBuilder.Default
    .AddClass("card")
    .AddClass("card-elevated", when: Elevation > 0)
    .AddClass($"card-{Color.ToString().ToLower()}")
    .Build();

var style = MudExStyleBuilder.Default
    .AddStyle("width", $"{Width}px", when: Width > 0)
    .AddStyle("height", $"{Height}px", when: Height > 0)
    .Build();
```

### Theme Customization

Create and customize themes programmatically:

```csharp
var theme = MudExThemeHelper.CreateTheme(primaryColor);
MudExThemeHelper.SetSecondaryColor(theme, secondaryColor);
var cssVars = MudExThemeHelper.ToCssVariables(theme);
```

### File Type Detection

Detect and handle different file types:

```csharp
var icon = BrowserFileExt.IconForFile(file.ContentType);
if (file.IsImage())
{
    var dataUrl = await file.ToDataUrlAsync();
    // Display image
}
```

## Best Practices

- **CSS Builder**: Use for component classes that change based on state
- **Style Builder**: Use for inline styles that need dynamic values
- **Color Utilities**: Normalize color handling across your application
- **Size Utilities**: Use for responsive sizing calculations
- **JSON Helper**: Use for consistent JSON handling
- **Theme Helper**: Use for runtime theme customization
- **File Extensions**: Use for consistent file handling UI

## See Also

- [Components](../components/index.md) - Many components use these utilities internally
- [Extensions](../extensions/index.md) - Extension methods that leverage utilities
- [API Reference](../api/index.md) - Complete API documentation
