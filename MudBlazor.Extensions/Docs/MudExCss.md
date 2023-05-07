# MudExCss

MudExCss is a static utility class that provides a set of methods to work with CSS animations, generate CSS strings, manipulate CSS variables, and more.

## Methods

### GetAnimationCssStyle (AnimationType, TimeSpan, AnimationDirection?, AnimationTimingFunction, DialogPosition?)

Generates a CSS animation string for a single animation type.

#### Parameters

- `type`: The `AnimationType` to be used for the animation.
- `duration`: The duration of the animation as a `TimeSpan`.
- `direction` (optional): The `AnimationDirection` for the animation. Default is `null`.
- `animationTimingFunction` (optional): The `AnimationTimingFunction` for the animation. Default is `null`.
- `targetPosition` (optional): The `DialogPosition` for the animation. Default is `null`.

#### Example

```csharp
var animationCss = MudExCss.GetAnimationCssStyle(AnimationType.SlideIn, TimeSpan.FromSeconds(1), AnimationDirection.In);
```

### GetAnimationCssStyle (AnimationType[], TimeSpan, AnimationDirection?, AnimationTimingFunction, DialogPosition?)

Generates a CSS animation string for an array of animation types.

#### Parameters

- `types`: An array of `AnimationType` to be used for the animations.
- `duration`: The duration of the animations as a `TimeSpan`.
- `direction` (optional): The `AnimationDirection` for the animations. Default is `null`.
- `animationTimingFunction` (optional): The `AnimationTimingFunction` for the animations. Default is `null`.
- `targetPosition` (optional): The `DialogPosition` for the animations. Default is `null`.

#### Example

```csharp
var animationTypes = new AnimationType[] { AnimationType.SlideIn, AnimationType.FadeIn };
var animationCss = MudExCss.GetAnimationCssStyle(animationTypes, TimeSpan.FromSeconds(1));
```

### GenerateCssString (object, string)

Generates a CSS string from an object's properties.

#### Parameters

- `obj`: The object to generate the CSS string from.
- `existingCss` (optional): An existing CSS string to be combined with the generated CSS string. Default is an empty string.

#### Example

```csharp
var cssObj = new { width = 100, height = 200, backgroundColor = "red" };
var cssString = MudExCss.GenerateCssString(cssObj);
```

### GenerateCssString (object, CssUnit, string)

Generates a CSS string from an object's properties with a specified CSS unit.

#### Parameters

- `obj`: The object to generate the CSS string from.
- `cssUnit`: The `CssUnit` to be used for the properties that require units.
- `existingCss` (optional): An existing CSS string to be combined with the generated CSS string. Default is an empty string.

#### Example

```csharp
var cssObj = new { width = 100, height = 200, backgroundColor = "red" };
var cssString = MudExCss.GenerateCssString(cssObj, CssUnit.Pixels);
```
