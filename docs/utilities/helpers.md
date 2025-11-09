# Helper Utilities

Additional utility classes and helper methods for common tasks.

## MudExJsonHelper

JSON utility class for serialization, deserialization, and manipulation.

### Features

- JSON serialization/deserialization
- Pretty print formatting
- JSON path queries
- Schema validation
- Type-safe conversions
- Circular reference handling

### Basic Usage

```csharp
using MudBlazor.Extensions.Helper;

// Serialize object to JSON
var json = MudExJsonHelper.Serialize(myObject);

// Deserialize JSON to object
var obj = MudExJsonHelper.Deserialize<MyType>(json);

// Pretty print JSON
var formatted = MudExJsonHelper.PrettyPrint(json);
```

### Advanced Example

```csharp
// Serialize with custom options
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
var json = MudExJsonHelper.Serialize(myObject, options);

// Query JSON path
var value = MudExJsonHelper.GetValueByPath(json, "user.address.city");

// Validate JSON
var isValid = MudExJsonHelper.ValidateJson(json);
```

## MudExFonts

Font utility class for managing and working with fonts.

### Features

- System font detection
- Web font loading
- Font metrics calculation
- Google Fonts integration
- Font format conversion
- Font preview generation

### Basic Usage

```csharp
using MudBlazor.Extensions.Helper;

// Get available system fonts
var systemFonts = MudExFonts.GetSystemFonts();

// Load Google Font
await MudExFonts.LoadGoogleFont("Roboto");

// Check if font is available
var isAvailable = MudExFonts.IsFontAvailable("Arial");
```

### Example

```csharp
// Get font with specific weights
var fontFamily = await MudExFonts.GetFontFamily("Inter", new[] { 400, 600, 700 });

// Generate font preview
var previewUrl = MudExFonts.GenerateFontPreview("Roboto", "Hello World");
```

## MudExThemeHelper

Theme utility class for theme manipulation and conversion.

### Features

- Theme creation and modification
- Color scheme generation
- Light/dark theme conversion
- Theme export/import
- CSS variable generation
- Theme validation

### Basic Usage

```csharp
using MudBlazor.Extensions.Helper;

// Create theme from primary color
var theme = MudExThemeHelper.CreateTheme(primaryColor);

// Generate complementary colors
var palette = MudExThemeHelper.GeneratePalette(baseColor);

// Convert theme to CSS variables
var cssVars = MudExThemeHelper.ToCssVariables(theme);
```

### Advanced Example

```csharp
// Create custom theme
var theme = new MudTheme();
MudExThemeHelper.SetPrimaryColor(theme, "#1976D2");
MudExThemeHelper.SetSecondaryColor(theme, "#DC004E");

// Generate dark variant
var darkTheme = MudExThemeHelper.CreateDarkVariant(theme);

// Export theme
var themeJson = MudExThemeHelper.ExportTheme(theme);

// Import theme
var importedTheme = MudExThemeHelper.ImportTheme(themeJson);
```

## BrowserFileExt

Extension methods for IBrowserFile with additional functionality.

### Features

- File type detection
- Icon selection based on file type
- File size formatting
- Image resizing
- Base64 conversion
- File validation

### Basic Usage

```csharp
using MudBlazor.Extensions.Helper;

// Get icon for file
var icon = BrowserFileExt.IconForFile(file);
var iconByContentType = BrowserFileExt.IconForFile(file.ContentType);

// Format file size
var sizeString = file.FormatSize(); // e.g., "2.5 MB"

// Check if file is image
var isImage = file.IsImage();
```

### Advanced Example

```csharp
// Convert to base64 data URL
var dataUrl = await file.ToDataUrlAsync();

// Resize image
var resized = await file.ResizeImageAsync(800, 600);

// Validate file
var validation = file.Validate(
    maxSize: 10 * 1024 * 1024, // 10 MB
    allowedExtensions: new[] { ".jpg", ".png", ".gif" }
);

if (!validation.IsValid)
{
    Console.WriteLine(validation.ErrorMessage);
}
```

## EnumHelper

Helper class for working with enums.

### Features

- Get enum display name
- Get enum description
- Get enum values
- Parse enum from string
- Get enum icon
- Enum to list conversion

### Basic Usage

```csharp
using MudBlazor.Extensions.Helper;

// Get display name
var displayName = EnumHelper.GetDisplayName(MyEnum.Value);

// Get description
var description = EnumHelper.GetDescription(MyEnum.Value);

// Get all enum values
var values = EnumHelper.GetValues<MyEnum>();

// Parse from string
var parsed = EnumHelper.Parse<MyEnum>("Value");
```

### Example with Attributes

```csharp
public enum Status
{
    [Display(Name = "In Progress", Description = "Task is being worked on")]
    InProgress,
    
    [Display(Name = "Completed", Description = "Task is done")]
    Completed
}

// Usage
var name = EnumHelper.GetDisplayName(Status.InProgress); // "In Progress"
var desc = EnumHelper.GetDescription(Status.InProgress); // "Task is being worked on"
```

## ComponentHelper

Helper class for component-related operations.

### Features

- Component type discovery
- Parameter reflection
- Component instantiation
- Render fragment creation
- Component property access

### Basic Usage

```csharp
using MudBlazor.Extensions.Helper;

// Get component parameters
var parameters = ComponentHelper.GetParameters<MyComponent>();

// Create render fragment
var fragment = ComponentHelper.CreateRenderFragment<MyComponent>(parameters);

// Check if component has parameter
var hasParameter = ComponentHelper.HasParameter<MyComponent>("Value");
```

## LocalizationStringHelper

Helper for localization and string manipulation.

### Features

- String localization
- Culture-specific formatting
- Pluralization
- String interpolation
- Resource loading

### Basic Usage

```csharp
using MudBlazor.Extensions.Helper;

// Get localized string
var localized = LocalizationStringHelper.GetString("greeting.hello");

// Format with culture
var formatted = LocalizationStringHelper.Format("greeting.welcome", userName);

// Pluralize
var message = LocalizationStringHelper.Pluralize("item", count);
```

## Best Practices

- Use `MudExJsonHelper` for consistent JSON handling across the application
- Cache font information to reduce repeated queries
- Export and version themes for consistency across deployments
- Validate file types and sizes on both client and server
- Use enum attributes for better UI representation
- Consider culture-specific formatting for international applications

## See Also

- [Color Utilities](color-utils.md)
- [CSS Builder](css-builder.md)
- [Style Builder](style-builder.md)
- [Size Utilities](size-utils.md)
