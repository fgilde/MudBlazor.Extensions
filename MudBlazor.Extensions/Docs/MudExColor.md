# MudExColor

`MudExColor` is a static utility class that provides a set of extension methods for working with `Color` and `MudColor` instances. These methods include converting between `Color` and `MudColor`, creating CSS variable names and declarations, and checking if a `MudColor` is black or white.

## Methods

### CssVarName (this Color)

Returns the CSS variable name for a given `Color`.

#### Parameters

- `color`: The `Color` instance.

#### Example

```csharp
string cssVarName = myColor.CssVarName();
```

### CssVarDeclaration (this Color)

Returns the CSS variable declaration for a given `Color`.

#### Parameters

- `color`: The `Color` instance.

#### Example

```csharp
string cssVarDeclaration = myColor.CssVarDeclaration();
```

### ToDrawingColor (this MudColor)

Converts a `MudColor` to a `System.Drawing.Color`.

#### Parameters

- `color`: The `MudColor` instance.

#### Example

```csharp
System.Drawing.Color drawingColor = myMudColor.ToDrawingColor();
```

### ToMudColor (this System.Drawing.Color)

Converts a `System.Drawing.Color` to a `MudColor`.

#### Parameters

- `color`: The `System.Drawing.Color` instance.

#### Example

```csharp
MudColor mudColor = myDrawingColor.ToMudColor();
```

### ToCssRgba (this MudColor)

Converts a `MudColor` to a CSS RGBA string.

#### Parameters

- `color`: The `MudColor` instance.

#### Example

```csharp
string cssRgba = myMudColor.ToCssRgba();
```

### IsBlack (this MudColor)

Checks if a `MudColor` instance is black.

#### Parameters

- `color`: The `MudColor` instance.

#### Example

```csharp
bool isBlack = myMudColor.IsBlack();
```

### IsWhite (this MudColor)

Checks if a `MudColor` instance is white.

#### Parameters

- `color`: The `MudColor` instance.

#### Example

```csharp
bool isWhite = myMudColor.IsWhite();
```
