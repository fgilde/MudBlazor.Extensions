# MudExObjectEdit

The `MudExObjectEdit` is a powerful component that automatically generates user interfaces for object editing. It supports automatic validation for DataAnnotation validations or fluent registered validations for your model.

![Object Edit](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/Screenshots/ObjectEdit.gif)

## Features

- ✅ **Automatic UI Generation** - Creates forms from your models automatically
- ✅ **Built-in Validation** - Supports DataAnnotations and FluentValidation
- ✅ **Highly Customizable** - Configure layout, styling, and behavior
- ✅ **Conditional Rendering** - Show/hide fields based on conditions
- ✅ **Custom Components** - Use any component for property rendering
- ✅ **Grouping & Layout** - Organize fields with tabs, accordions, or dock panels
- ✅ **Dialog Integration** - Easy dialog-based editing

## Basic Usage

### Using MudExObjectEditForm

The simplest way to use object editing is with `MudExObjectEditForm`:

```razor
<MudExObjectEditForm OnValidSubmit="@OnSubmit" Value="@MyModel" />

@code {
    private MyModel MyModel = new MyModel();

    private void OnSubmit()
    {
        // Handle valid submission
        Console.WriteLine($"Name: {MyModel.Name}");
    }
}
```

### Using MudExObjectEditDialog

Edit objects in a dialog using the extension method on `IDialogService`:

```csharp
@inject IDialogService DialogService

@code {
    private async Task EditUser()
    {
        var user = new User { Name = "John Doe" };
        var dialogOptionsEx = new DialogOptionsEx 
        { 
            Resizeable = true,
            CloseButton = true
        };
        
        var result = await DialogService.EditObject(user, "Edit User", dialogOptionsEx);
        
        if (!result.Cancelled)
        {
            // User clicked save
            var editedUser = result.Data as User;
        }
    }
}
```

## Model Example

Define your model with DataAnnotations for automatic validation:

```csharp
public class MyModel
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(50, MinimumLength = 3)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Range(18, 100)]
    public int Age { get; set; }

    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateTime? BirthDate { get; set; }

    public bool IsActive { get; set; }

    [Display(Name = "Programming Skills")]
    public ProgrammingSkill Skills { get; set; }
}

public enum ProgrammingSkill
{
    Beginner,
    Intermediate,
    Advanced,
    Expert
}
```

## Advanced Configuration

### Using IObjectMetaConfiguration

For advanced customization, implement `IObjectMetaConfiguration<T>`:

```csharp
public class MyModelMetaConfiguration : IObjectMetaConfiguration<MyModel>
{
    public Task ConfigureAsync(ObjectEditMeta<MyModel> meta)
    {
        // Configure specific property
        meta.Property(m => m.Name)
            .WithLabel("Full Name")
            .WithHelperText("Enter your full name")
            .WrapInMudItem(i => i.xs = 6);

        // Configure multiple properties at once
        meta.Properties(m => m.Email, m => m.Age)
            .WrapInMudItem(i => i.xs = 6);

        // Group properties
        meta.Property(m => m.BirthDate)
            .WithGroup("Personal Information");

        return Task.CompletedTask;
    }
}
```

Then use it in your form:

```razor
<MudExObjectEditForm Value="@MyModel" 
                     MetaConfiguration="@metaConfiguration" 
                     OnValidSubmit="@OnSubmit" />

@code {
    private IObjectMetaConfiguration<MyModel> metaConfiguration = 
        new MyModelMetaConfiguration();
}
```

### Using Custom Components

Render properties with custom components:

```csharp
public class MyModelMetaConfiguration : IObjectMetaConfiguration<MyModel>
{
    public Task ConfigureAsync(ObjectEditMeta<MyModel> meta)
    {
        // Use a custom rich text editor
        meta.Property(m => m.Description)
            .WithGroup("Content")
            .RenderWith(e => e.Value, ConfigureRichTextEditor())
            .WithSeparateLabelComponent();

        return Task.CompletedTask;
    }

    private static Action<MyRichTextEditor> ConfigureRichTextEditor()
    {
        return editor =>
        {
            editor.EnableResize = true;
            editor.Height = "250px";
            editor.Placeholder = "Enter description...";
        };
    }
}
```

## Conditional Rendering

### IgnoreIf - Conditional Visibility

Hide properties based on conditions:

```csharp
public class MyModelMetaConfiguration : IObjectMetaConfiguration<MyModel>
{
    public Task ConfigureAsync(ObjectEditMeta<MyModel> meta)
    {
        // Only show email when user is active
        meta.Property(m => m.Email)
            .IgnoreIf(m => !m.IsActive);

        // Multiple conditions
        meta.Property(m => m.AdvancedSettings)
            .IgnoreIf(m => !m.IsActive || m.Age < 18);

        return Task.CompletedTask;
    }
}
```

### WithAttributesIf - Conditional Attributes

Set component attributes based on conditions:

```csharp
public Task ConfigureAsync(ObjectEditMeta<MyModel> meta)
{
    // Make field readonly based on condition
    meta.Property(m => m.Name)
        .WithAttributesIf(m => !m.CanEdit(),
            new KeyValuePair<string, object>("ReadOnly", true),
            new KeyValuePair<string, object>("Disabled", true));

    // Alternative syntax using component type
    meta.Property(m => m.Name)
        .WithAttributesIf<MyModel, MudTextField<string>>(
            m => !m.CanEdit(),
            field =>
            {
                field.ReadOnly = true;
                field.Disabled = true;
            });

    return Task.CompletedTask;
}
```

### Conditional Based on Property Value

Use conditions based on the property value itself:

```csharp
public Task ConfigureAsync(ObjectEditMeta<MyModel> meta)
{
    // Make readonly when value is "Admin"
    meta.Property(m => m.Role)
        .WithAttributesIf<string, MudTextField<string>>(
            role => role == "Admin",
            field =>
            {
                field.ReadOnly = true;
                field.HelperText = "Admin role cannot be changed";
            });

    return Task.CompletedTask;
}
```

## Layout & Grouping

### Wrapping in MudGrid

Organize properties in a grid layout:

```csharp
public Task ConfigureAsync(ObjectEditMeta<MyModel> meta)
{
    // Wrap entire form in grid
    meta.WrapEachInMudItem(i => i.xs = 6); // All fields in 2 columns

    // Specific properties with custom widths
    meta.Property(m => m.Name)
        .WrapInMudItem(i => i.xs = 12); // Full width

    meta.Properties(m => m.Email, m => m.Phone)
        .WrapInMudItem(i => i.xs = 6); // Half width each

    return Task.CompletedTask;
}
```

### Custom Wrapper Components

Wrap properties in custom components:

```csharp
public Task ConfigureAsync(ObjectEditMeta<MyModel> meta)
{
    // Single wrapper
    meta.Property(m => m.Description)
        .WrapIn<MudPaper>(paper =>
        {
            paper.Elevation = 2;
            paper.Class = "pa-4 ma-2";
        });

    // Multiple nested wrappers
    meta.Property(m => m.ImportantField)
        .WrapIn<MudPaper>(paper => paper.Elevation = 4)
        .WrapIn<MudCard>(card => card.Outlined = true);

    return Task.CompletedTask;
}
```

### Grouping by Tabs

Organize properties into tabs:

```csharp
public Task ConfigureAsync(ObjectEditMeta<MyModel> meta)
{
    meta.Property(m => m.Name).WithGroup("Basic Info");
    meta.Property(m => m.Email).WithGroup("Basic Info");
    meta.Property(m => m.Address).WithGroup("Contact");
    meta.Property(m => m.Phone).WithGroup("Contact");
    
    // Set the group mode to tabs
    meta.WithGroupMode(ObjectEditGroupMode.Tabs);

    return Task.CompletedTask;
}
```

### Grouping by Accordions

Use accordions for collapsible groups:

```csharp
public Task ConfigureAsync(ObjectEditMeta<MyModel> meta)
{
    meta.Property(m => m.Name).WithGroup("Personal");
    meta.Property(m => m.Age).WithGroup("Personal");
    meta.Property(m => m.Bio).WithGroup("Details");
    
    meta.WithGroupMode(ObjectEditGroupMode.Accordion);

    return Task.CompletedTask;
}
```

### Dock Panel Layout

Create a dock panel layout:

```csharp
public Task ConfigureAsync(ObjectEditMeta<MyModel> meta)
{
    meta.WithGroupMode(ObjectEditGroupMode.DockPanel);

    return Task.CompletedTask;
}
```

## Parameters

### MudExObjectEditForm Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Value` | `T` | - | The object to edit |
| `ValueChanged` | `EventCallback<T>` | - | Callback when value changes |
| `OnValidSubmit` | `EventCallback` | - | Callback on valid form submission |
| `OnInvalidSubmit` | `EventCallback` | - | Callback on invalid form submission |
| `MetaConfiguration` | `IObjectMetaConfiguration<T>` | - | Configuration for customization |
| `WrapInMudGrid` | `bool?` | `null` | Whether to wrap in MudGrid (auto-detect by default) |
| `Spacing` | `int` | `3` | Spacing between items |
| `Justify` | `Justify` | `Start` | Grid justification |

### MudExObjectEditDialog Parameters

All `MudExObjectEditForm` parameters plus:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Title` | `string` | - | Dialog title |
| `SaveButtonText` | `string` | "Save" | Text for save button |
| `CancelButtonText` | `string` | "Cancel" | Text for cancel button |

## Validation

### DataAnnotations Validation

Use standard DataAnnotations attributes:

```csharp
public class MyModel
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Name { get; set; }

    [EmailAddress]
    public string Email { get; set; }

    [Range(1, 100)]
    public int Age { get; set; }

    [Compare(nameof(Email))]
    public string ConfirmEmail { get; set; }

    [RegularExpression(@"^\d{3}-\d{3}-\d{4}$")]
    public string PhoneNumber { get; set; }
}
```

### FluentValidation

Register FluentValidation in your services:

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<MyModelValidator>();
```

Create a validator:

```csharp
public class MyModelValidator : AbstractValidator<MyModel>
{
    public MyModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(3, 50);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Age)
            .InclusiveBetween(18, 100)
            .When(x => x.IsAdult);
    }
}
```

## Examples

### Complete Example with All Features

```csharp
public class UserEditConfiguration : IObjectMetaConfiguration<User>
{
    public Task ConfigureAsync(ObjectEditMeta<User> meta)
    {
        // Layout
        meta.WrapEachInMudItem(i => i.xs = 6);

        // Basic fields
        meta.Property(m => m.FirstName)
            .WithLabel("First Name")
            .WithHelperText("Enter your first name")
            .WrapInMudItem(i => i.xs = 6);

        meta.Property(m => m.LastName)
            .WithLabel("Last Name")
            .WrapInMudItem(i => i.xs = 6);

        // Email with conditional readonly
        meta.Property(m => m.Email)
            .WithGroup("Contact Information")
            .WithAttributesIf(m => m.IsVerified,
                new KeyValuePair<string, object>("ReadOnly", true))
            .WrapInMudItem(i => i.xs = 12);

        // Conditional visibility
        meta.Property(m => m.AdminNotes)
            .WithGroup("Admin")
            .IgnoreIf(m => !m.IsAdmin);

        // Custom component
        meta.Property(m => m.Bio)
            .WithGroup("Profile")
            .RenderWith(e => e.Value, ConfigureEditor());

        // Set group mode
        meta.WithGroupMode(ObjectEditGroupMode.Tabs);

        return Task.CompletedTask;
    }

    private Action<MudTextField<string>> ConfigureEditor()
    {
        return editor =>
        {
            editor.Lines = 5;
            editor.Variant = Variant.Outlined;
        };
    }
}
```

## Live Examples

Check out live examples at:

- [Basic Object Edit](https://www.mudex.org/object-edit)
- [Object Edit with Tabs](https://www.mudex.org/mudex-object-edit-tabs)
- [Object Edit with Dock Panel](https://www.mudex.org/mudex-object-edit-dock)

## See Also

- [Structured Data Editor](structured-data-editor.md) - Edit JSON/XML/YAML
- [Dialog Extensions](../extensions/dialog-extensions.md) - Enhanced dialogs
- [Utilities](../utilities/index.md) - Helper utilities
