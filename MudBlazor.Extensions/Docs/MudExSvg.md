# MudExSvg

`MudExSvg` is a static utility class that provides a set of methods for combining SVG icons. You can combine icons horizontally, vertically, or centered or for generating SVG representations of application layouts. You can adjust the color and size parameters for the main layout components such as the AppBar, Drawer and Surface.

## Methods


### ApplicationImage

Generates an SVG image string that represents an application layout.

#### Overloads

- ApplicationImage (string, string, string, string[], string, string)
- ApplicationImage (string, string, string, string[], string)
- ApplicationImage (string, string, string, string[], MudExSize<int>)
- ApplicationImage (string, string, string, string, MudExDimension)
- ApplicationImage (string, string, string, string[], MudExDimension)
...and more

#### Parameters

Depending on the overload used, the parameters may include:

- `appBarColor`, `drawerColor`, `surfaceColor`: The color parameters for the layout components.
- `textColors`: The colors for the lines inside the layout.

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
