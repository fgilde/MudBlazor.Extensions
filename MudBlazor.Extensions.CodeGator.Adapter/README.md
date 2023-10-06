# MudBlazor.Extensions.CodeGator Adapter

## Overview
The MudBlazor.Extensions.CodeGator Adapter seamlessly integrates [CG.Blazor.Forms](https://github.com/CodeGator/CG.Blazor.Forms._MudBlazor) with the [MudExObjectEdit](https://github.com/fgilde/MudBlazor.Extensions/blob/main/MudBlazor.Extensions/Docs/ObjectEdit.md) component from [MudBlazor.Extensions](https://www.mudex.org). It allows developers to utilize CodeGator attributes on their properties while rendering forms using the MudExObjectEdit component.
More about MudBlazor.Extension can found [here]((https://www.mudex.org/)


[Live sample](https://www.mudex.org/object-edit-cg-adapter?layout=empty)

## Benefits
- Maintain all CodeGator attributes in your model properties.
- Harness the power and flexibility of [MudExObjectEdit](https://github.com/fgilde/MudBlazor.Extensions/blob/main/MudBlazor.Extensions/Docs/ObjectEdit.md) for form rendering.
  
## Prerequisites
Ensure you have [MudBlazor.Extensions](https://github.com/fgilde/MudBlazor.Extensions) installed in your project.

## Getting Started

1. **Installation**: After ensuring you have MudBlazor.Extensions installed, simply add the following code to your service registration setup:

```c#
 builder.Services.AddMudExObjectEditCGBlazorFormsAdapter();
```

2. **Usage**: Use CodeGator attributes in conjunction with MudBlazor components. Here's a simple example of a model using both:

```c#
public class SampleModel
{
    [RenderMudAlert]
    public string Text { get; set; } = "MoIn";

    [RenderMudTextField]
    [Required]
    public string FirstName { get; set; } = "Pete";

    [RenderMudTextField]
    [Required]
    public string LastName { get; set; }

    [RenderMudDatePicker]
    public DateTime? DateOfBirth { get; set; }
}
```

With this model above you can then use it in MudExObjectEditForm like this

```c#
    <MudExObjectEditForm Value="@Model"></MudExObjectEditForm>
```

![Screenshot](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions.CodeGator.Adapter/screenshot.png)

## Conclusion
By integrating the best of both worlds, this package offers a streamlined approach to form rendering in Blazor. Whether you're a seasoned developer or just starting out, this adapter will enhance your development workflow and productivity.
