# MudExSize<T>

## Overview

`MudExSize<T>` is a readonly struct that provides a convenient and type-safe way to deal with size values in the context of MudBlazor components. It accepts values of type `T` and pairs them with a `CssUnit` enumeration to represent the CSS unit of the size value.
This struct is default type for the most current and upcoming size properties in MudBlazor.Extensions components. Like in [MudExDivider](https://www.mudex.org/c/MudExDivider) for example

Some of the supported `CssUnit` values are: 

- Pixels
- Points
- Percent
- Em
- Rem
- ViewportWidth
- ViewportHeight
- CharacterZero

## Usage

You can use `MudExSize<T>` to assign size properties in your Blazor components. It allows for flexible sizing assignments with appropriate unit specification.

Here are some examples of how you can assign sizes using `MudExSize<T>`:

```
<MudExDivider Size="2" Vertical="true" />
<MudExDivider Size="@("2rem")" Vertical="true" />
<MudExDivider Size="@(new MudExSize<double>(2, CssUnit.Points))" Vertical="true" />
```

In these examples:

- `2` is an implicitly converted string representing a size in pixels (the default unit).
- `"2rem"` is a string that represents a size in rem units.
- `new MudExSize<double>(2, CssUnit.Points)` is an instance of `MudExSize<double>` that represents a size of `2pt`.

The `MudExSize<T>` struct also provides an override of the `ToString` method which returns a string representation of the size value along with its CSS unit, making it easy to use in contexts where a string size value is required.

The struct provides implicit conversions to and from the type `T`, `string`, and `MudExSize<T>`, allowing for a seamless and intuitive usage in various scenarios.
*/