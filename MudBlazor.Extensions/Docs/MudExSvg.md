# MudExSvg

`MudExSvg` is a static utility class that provides a set of methods for combining SVG icons. You can combine icons horizontally, vertically, or centered.

## Methods

### CombineIconsHorizontal (string, params string[])

Combines SVG icons horizontally.

#### Parameters

- `image`: The first SVG icon.
- `other`: The other SVG icons.

#### Example

```csharp
string combinedIcons = MudExSvg.CombineIconsHorizontal(image1, image2, image3);
```

### CombineIconsVertical (string, params string[])

Combines SVG icons vertically.

#### Parameters

- `image`: The first SVG icon.
- `other`: The other SVG icons.

#### Example

```csharp
string combinedIcons = MudExSvg.CombineIconsVertical(image1, image2, image3);
```

### CombineIconsCentered (string, params string[])

Combines SVG icons centered.

#### Parameters

- `image`: The first SVG icon.
- `other`: The other SVG icons.

#### Example

```csharp
string combinedIcons = MudExSvg.CombineIconsCentered(image1, image2, image3);
```

### CombineIcons (int, int, string, string, params string[])

Combines SVG icons with specified margin and viewBox settings.

#### Parameters

- `marginLeftRight`: The margin between icons horizontally.
- `marginTopBottom`: The margin between icons vertically.
- `originalViewBox`: The original viewBox for the SVG icons.
- `image`: The first SVG icon.
- `other`: The other SVG icons.

#### Example

```csharp
string combinedIcons = MudExSvg.CombineIcons(14, 0, "0 0 24 24", image1, image2, image3);
```
