# Form and Input Components

Enhanced form input components with additional features and validation support.

## MudExTextField

Enhanced text field with additional features beyond standard MudTextField.

### Basic Usage

```razor
<MudExTextField @bind-Value="@text" Label="Enter Text" />
```

### Features

- All standard MudTextField features
- Advanced validation
- Custom formatting
- Input masking
- Character counter
- Auto-complete suggestions
- Rich text formatting

### Example

```razor
@code {
    private string text = "";
}

<MudExTextField @bind-Value="@text" 
                Label="Name"
                Placeholder="Enter your name"
                Required="true"
                MaxLength="50"
                ShowCharacterCount="true" />
```

## MudExCheckBox

Enhanced checkbox component with additional styling options.

### Basic Usage

```razor
<MudExCheckBox @bind-Checked="@isChecked" Label="Accept Terms" />
```

### Features

- All standard checkbox features
- Custom icons
- Indeterminate state
- Read-only mode
- Rich label content
- Group validation

### Example

```razor
@code {
    private bool isChecked = false;
}

<MudExCheckBox @bind-Checked="@isChecked" 
               Label="I agree to the terms and conditions"
               Color="Color.Primary"
               Required="true" />
```

## MudExInput

Base input component for creating custom inputs.

### Features

- Generic value binding
- Validation support
- Error messages
- Helper text
- Icons and adornments
- Disabled and read-only states

### Example

```razor
<MudExInput @bind-Value="@value" 
            Label="Custom Input"
            HelperText="Enter a value"
            Adornment="Adornment.Start"
            AdornmentIcon="@Icons.Material.Filled.Search" />
```

## MudExSelect

Enhanced select component with additional features.

### Basic Usage

```razor
<MudExSelect @bind-Value="@selectedValue" Label="Select Option">
    <MudExSelectItem Value="1">Option 1</MudExSelectItem>
    <MudExSelectItem Value="2">Option 2</MudExSelectItem>
</MudExSelect>
```

### Features

- All standard select features
- Multi-select support
- Search/filter
- Custom item templates
- Grouping
- Async data loading
- Virtualization for large datasets

### Example with Groups

```razor
<MudExSelect @bind-Value="@selectedCountry" Label="Country">
    <MudExSelectItemGroup Text="North America">
        <MudExSelectItem Value="US">United States</MudExSelectItem>
        <MudExSelectItem Value="CA">Canada</MudExSelectItem>
    </MudExSelectItemGroup>
    <MudExSelectItemGroup Text="Europe">
        <MudExSelectItem Value="UK">United Kingdom</MudExSelectItem>
        <MudExSelectItem Value="DE">Germany</MudExSelectItem>
    </MudExSelectItemGroup>
</MudExSelect>
```

## MudExChipSelect

Chip-based selection component for multiple selections.

### Basic Usage

```razor
<MudExChipSelect @bind-SelectedValues="@selectedTags" />
```

### Features

- Multiple selection with chips
- Add custom values
- Remove selections
- Color coding
- Icons on chips
- Search and filter

### Example

```razor
@code {
    private HashSet<string> selectedTags = new();
    private List<string> availableTags = new() 
    { 
        "C#", "Blazor", "ASP.NET", "JavaScript", "TypeScript" 
    };
}

<MudExChipSelect @bind-SelectedValues="@selectedTags"
                 Items="@availableTags"
                 Label="Select Technologies"
                 AllowCustomValues="true"
                 MultiSelection="true" />
```

## MudExTagField

Tag field component for entering and managing tags.

### Basic Usage

```razor
<MudExTagField @bind-Values="@tags" />
```

### Features

- Add tags by typing and pressing Enter
- Remove tags with backspace or delete
- Auto-complete suggestions
- Validation per tag
- Maximum tag count
- Custom tag rendering

### Example

```razor
@code {
    private List<string> tags = new();
}

<MudExTagField @bind-Values="@tags"
               Label="Tags"
               Placeholder="Add tags..."
               MaxTags="5"
               AutoCompleteSource="@GetTagSuggestions" />
```

## MudExColorEdit

Color editing component with visual color picker integration.

### Basic Usage

```razor
<MudExColorEdit @bind-Value="@color" />
```

### Features

- Visual color picker
- HEX, RGB, HSL input
- Alpha channel support
- Color presets
- Recent colors
- Format conversion

### Example

```razor
@code {
    private MudExColor color = MudExColor.Primary;
}

<MudExColorEdit @bind-Value="@color"
                Label="Theme Color"
                ShowAlpha="true"
                ShowPresets="true" />
```

## MudExThemeEdit

Theme editing component for customizing application themes.

### Basic Usage

```razor
<MudExThemeEdit @bind-Theme="@theme" />
```

### Features

- Visual theme editor
- Primary, secondary, tertiary color selection
- Dark/light mode
- Typography settings
- Spacing configuration
- Export/import theme

### Example

```razor
@code {
    private MudTheme theme = new MudTheme();
}

<MudExThemeEdit @bind-Theme="@theme"
                ShowPreview="true"
                AllowExport="true" />
```

## MudExThemeSelect

Theme selection component with predefined themes.

### Basic Usage

```razor
<MudExThemeSelect @bind-SelectedTheme="@selectedTheme" />
```

### Features

- Browse predefined themes
- Theme preview
- Light/dark variants
- Custom theme support
- Apply theme instantly

## MudExHtmlEdit

Rich HTML editor component.

### Basic Usage

```razor
<MudExHtmlEdit @bind-Value="@htmlContent" />
```

### Features

- WYSIWYG editing
- Toolbar with formatting options
- Image upload
- Table support
- Code view
- HTML sanitization
- Markdown import/export

### Example

```razor
@code {
    private string htmlContent = "<p>Hello World</p>";
}

<MudExHtmlEdit @bind-Value="@htmlContent"
               Height="400px"
               ShowToolbar="true"
               AllowImageUpload="true" />
```

## MudExValidationWrapper

Validation wrapper component for custom validation logic.

### Features

- Custom validation rules
- Multiple validators
- Async validation
- Conditional validation
- Error message customization

### Example

```razor
<MudExValidationWrapper Validation="@(value => ValidateCustom(value))">
    <MudExTextField @bind-Value="@value" Label="Custom Validation" />
</MudExValidationWrapper>
```

## Best Practices

- Use appropriate input components for data types
- Provide clear labels and helper text
- Implement proper validation
- Use placeholders to guide users
- Consider accessibility (ARIA labels, keyboard navigation)
- Test with various input methods (keyboard, mouse, touch)
- Provide visual feedback for validation errors

## See Also

- [Picker Components](pickers.md)
- [Object Edit](object-edit.md)
- [Other Components](other-components.md)
