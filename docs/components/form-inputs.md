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

Tag field component for entering and managing a list of values as chips.

### Basic Usage

```razor
<MudExTagField @bind-Values="@tags" Delimiters="@(new[] { ',', ' ' })" />
```

### Features

- Add tags by typing and pressing Enter, typing a delimiter, pasting a list or picking a suggestion
- Remove tags with the close icon or with Backspace on an empty field
- Suggestions in a dropdown below the field (`SearchFunc`, `MaxSuggestions`), already chosen values are hidden; arrow keys move the highlight, Enter or a click picks it
- Validation per tag (`ValidateChip`): invalid typed text is not added, stays in the field and shows the error; invalid values already in `Values` are shown as error colored chips
- Fixed tags (`IsChipClosable`): no close icon, Backspace does not remove them
- `CommitOnBlur` adds the typed text when the field loses focus (a suggestion list closing for lack of matches does not add anything); Escape discards the typed text while suggestions are used
- Duplicates are ignored unless `AllowDuplicates` is set

### Example

```razor
@code {
    private List<string> scopes = new() { "openid", "profile" };
    private static readonly string[] Known = { "openid", "profile", "email", "offline_access" };

    private Task<IEnumerable<string>> Search(string text, CancellationToken token)
        => Task.FromResult(Known.Where(s => s.Contains(text, StringComparison.OrdinalIgnoreCase)));

    private string Validate(string scope) => scope.Any(char.IsWhiteSpace) ? "No whitespace allowed" : null;
}

<MudExTagField @bind-Values="@scopes"
               Label="OAuth scopes"
               Delimiters="@(new[] { ' ', ',' })"
               SearchFunc="@Search"
               MaxSuggestions="5"
               ValidateChip="@Validate"
               IsChipClosable="@(scope => scope != "openid")"
               CommitOnBlur="true" />
```
## MudExCronEdit

Cron expression editor bound to a string.

### Basic Usage

```razor
<MudExCronEdit @bind-Value="@cron" Label="Schedule" />

@code {
    private string cron = "0 6 * * 1-5";
}
```

### Features

- Presets: every few minutes, hourly, daily, weekly (weekday chips), monthly
- Free text expression (5 fields, or 6 with seconds) with strict validation (numbers in range, JAN-DEC / SUN-SAT, ranges, lists, steps; `L`/`nW` for the day and `nL`/`n#k` for the weekday) and a description in words
- An empty value means no schedule: no error, clearing the field emits `null`
- Expressions the presets can't show stay unchanged and are shown as "Custom"
- An invalid typed expression shows an error and never overwrites the bound value
- `ReadOnly`, `Disabled`, `Variant`, `Margin`, `Dense`, `TimeIsUtc`
- Use it for a string property in `MudExObjectEdit` via `meta.Property(m => m.Schedule).RenderWith<MudExCronEdit, string>(e => e.Value)`

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
