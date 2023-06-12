# MudExColor

## Overview

`MudExColor` is a readonly struct that provides a unified way of dealing with colors in the context of MudBlazor components. 
It leverages the power of `OneOf` to allow users to define colors in different formats - `System.Drawing.Color`, `MudBlazor.Color`, `MudBlazor.MudColor`, string (CSS color), and uint (ARGB representation).
This new struct now is default type for the most of the color properties in MudBlazor.Extensions components. Like in [MudExDivider](https://www.mudex.org/c/MudExDivider) for example

```c#
[Parameter] public MudExColor Color { get; set; } = MudBlazor.Color.Default;
```

## Usage

You can use `MudExColor` to assign color properties in your Blazor components. It accepts the five formats mentioned above, making the handling of colors more flexible and straightforward.

Here are some examples of how you can assign colors using `MudExColor`:

```c#
<MudExDivider Color="4294901760" Size="2" Vertical="true" />
<MudExDivider Color="Color.Primary" Size="2" Vertical="true" />
<MudExDivider Color="@("#ff0000")" Size="2" Vertical="true" />
<MudExDivider Color="@(new MudColor(255,0,0,255))" Size="2" Vertical="true" />
```

In these examples:

- `4294901760` is a uint that represents an ARGB color.
- `Color.Primary` is an enum value from `MudBlazor.Color` that represents a color. This color will be looked up from the available colors of the MudBlazor theme.
- `"#ff0000"` is a string that represents a color using CSS syntax.
- `new MudColor(255,0,0,255)` is an instance of `MudColor` which is a color represented as RGBA.

Additionally, you can use System.Drawing.Color to assign color as follows:

```c#
<MudExDivider Color="@(System.Drawing.Color.Red)" Size="2" Vertical="true" />
```

The `MudExColor` struct also provides several utility methods such as `ToCssStringValue` which can be used to get the CSS string value of the color, `ToMudColorAsync` which can be used to get the `MudColor` representation of the color, and `Is` which can be used to check if the color matches a specific value.

## Using and editing values
You can fully use it in your components as well, and its fully suported by [MudExObjectEdit](https://www.mudex.org/d/ObjectEdit/MudExObjectEdit)
The Object Edit then uses the newly created component [MudExColorEdit](https://www.mudex.org/c/MudExColorEdit) to edit the value.

