# Utilities Overview

MudBlazor.Extensions provides various utility classes to simplify common tasks.

## Available Utilities

- **[Color Utilities](color-utils.md)** - Color manipulation and conversion
- **[CSS Builder](css-builder.md)** - Dynamic CSS class building
- **[Style Builder](style-builder.md)** - Dynamic inline style building
- **[Size Utilities](size-utils.md)** - Size calculations and conversions

## Quick Examples

### CSS Builder

```csharp
var css = MudExCssBuilder.Default
    .AddClass("my-class")
    .AddClass("active", when: isActive)
    .Build();
```

### Style Builder

```csharp
var style = MudExStyleBuilder.Default
    .AddStyle("color", primaryColor)
    .AddStyle("background", bgColor, when: hasBackground)
    .Build();
```

### Color Utilities

```csharp
var hex = MudExColorUtils.ToHex(color);
var rgb = MudExColorUtils.ToRgb(hexColor);
```
