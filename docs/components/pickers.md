# Picker Components

MudBlazor.Extensions provides several specialized picker components for selecting colors, icons, fonts, enums, and more.

## MudExColorPicker

Enhanced color picker with advanced features and color utilities integration.

### Basic Usage

```razor
<MudExColorPicker @bind-Value="@selectedColor" />
```

### Features

- Color selection with various formats (RGB, HEX, HSL)
- Support for transparency/alpha channel
- Color swatches and presets
- Integration with MudExColor utilities
- Inline or dialog mode

### Example

```razor
@code {
    private MudExColor selectedColor = MudExColor.Primary;
}

<MudExColorPicker @bind-Value="@selectedColor" 
                  Label="Select Color"
                  ShowAlpha="true" />
```

## MudExIconPicker

Icon picker component for selecting from MudBlazor icons library.

### Basic Usage

```razor
<MudExIconPicker @bind-Value="@selectedIcon" />
```

### Features

- Search and filter icons
- Category-based browsing
- Preview selected icon
- Support for custom icon sets
- Inline or dialog mode

### Example

```razor
@code {
    private string selectedIcon = Icons.Material.Filled.Home;
}

<MudExIconPicker @bind-Value="@selectedIcon" 
                 Label="Select Icon"
                 ShowPreview="true" />
```

## MudExFontSelect

Font selection component with preview functionality.

### Basic Usage

```razor
<MudExFontSelect @bind-Value="@selectedFont" />
```

### Features

- Browse system and web fonts
- Live font preview
- Font family and weight selection
- Google Fonts integration
- Custom font support

### Example

```razor
@code {
    private string selectedFont = "Roboto";
}

<MudExFontSelect @bind-Value="@selectedFont" 
                 Label="Select Font"
                 ShowPreview="true" />
```

## MudExEnumSelect

Specialized select component for enum types with automatic option generation.

### Basic Usage

```razor
<MudExEnumSelect T="MyEnum" @bind-Value="@selectedValue" />
```

### Features

- Automatic enum value discovery
- Display name attributes support
- Description tooltips
- Icon support for enum values
- Grouping support

### Example

```razor
@code {
    public enum Priority
    {
        [Display(Name = "Low Priority")]
        Low,
        [Display(Name = "Medium Priority")]
        Medium,
        [Display(Name = "High Priority")]
        High
    }

    private Priority selectedPriority = Priority.Medium;
}

<MudExEnumSelect T="Priority" 
                 @bind-Value="@selectedPriority" 
                 Label="Select Priority" />
```

## MudExCultureSelect

Culture and language selection component.

### Basic Usage

```razor
<MudExCultureSelect @bind-Culture="@selectedCulture" />
```

### Features

- Browse available cultures
- Display culture names in native language
- Filter and search cultures
- Support for specific or neutral cultures
- Flag icons for countries

### Example

```razor
@code {
    private CultureInfo selectedCulture = CultureInfo.CurrentCulture;
}

<MudExCultureSelect @bind-Culture="@selectedCulture" 
                    Label="Select Language"
                    ShowFlags="true" />
```

## MudExPicker

Base picker component for creating custom pickers.

### Features

- Dialog or inline mode
- Customizable picker content
- Action buttons (OK, Cancel)
- Keyboard navigation
- Validation support

### Example

```razor
<MudExPicker Label="Custom Picker" 
             PickerOpened="@OnPickerOpened"
             PickerClosed="@OnPickerClosed">
    <PickerContent>
        <!-- Your custom picker content -->
    </PickerContent>
</MudExPicker>
```

## Best Practices

- Use appropriate picker for the data type being selected
- Enable search/filter for large datasets
- Provide clear labels and descriptions
- Consider inline vs dialog mode based on available space
- Use validation to ensure required selections

## See Also

- [Form Components](other-components.md)
- [Object Edit](object-edit.md) - Pickers are automatically used in object editing
- [Utilities](../utilities/index.md)
