# MudExStyleBuilder

## Table of Contents
- [Introduction](#introduction)
- [Methods](#methods)
- [Usage](#usage)

## Introduction
The `MudExStyleBuilder` is a C# class used to build style strings. It provides various methods to add or modify CSS properties. It also allows the construction of styles from objects and CSS strings.

## Methods

### Static Methods

- `FromObject(object obj, string existingCss = "", CssUnit cssUnit = CssUnit.Pixels)`: Creates a `MudExStyleBuilder` from an object.

- `FromStyle(string style)`: Creates a `MudExStyleBuilder` from a style string.

- `GenerateStyleString(object obj, CssUnit cssUnit, string existingCss = "")`: Generates a style string from an object, adding a unit to certain properties.

- `CombineStyleStrings(string cssString, string leadingCssString)`: Combines two style strings, overriding properties in the leading CSS string with those in the CSS string.

- `StyleStringToObject<T>(string css)`: Converts a style string to an object of a specified type.

### Fluent WithProperty Methods

These methods allow to add or modify CSS properties:

- `WithStyle(string styleString, bool when = true)`: Adds style properties from a style string.

- `With(object styleObj, CssUnit cssUnit = CssUnit.Pixels, bool when = true)`: Adds style properties from an object.

- `WithColor(string color, bool when = true)`: Sets the color property.

- `WithBorder(string border, bool when = true)`: Sets the border property.

- `WithBorderRadius(string borderRadius, bool when = true)`: Sets the border-radius property.

And many more for other CSS properties.

- `With(string key, string value, bool when = true)`: Adds or modifies a style property.

### Other Methods

- `AsCssBuilderAsync()`: Returns a `MudExCssBuilder` with the current styles added as a class.

- `BuildAsClassRuleAsync(string className = null, IJSRuntime jSRuntime = null)`: Builds the current styles into a class rule.

- `RemoveClassRuleAsync(string className, IJSRuntime jSRuntime = null)`: Removes a class rule.

- `DisposeAsync()`: Removes all temporary class rules.

- `Build()`: Returns the current styles as a style string.

- `ToObject<T>()`: Returns the current styles as an object of a specified type.

- `ToString()`: Returns the current styles as a style string.

## Usage

Here's an example of using the `MudExStyleBuilder`:

```c#
var styleBuilder = MudExStyleBuilder.FromObject(new { width = 100, height = 200 });
styleBuilder.WithColor("red");
string style = styleBuilder.ToString(); // "width: 100px; height: 200px; color: red;"
```
