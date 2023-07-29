# MudExCss

MudExCss is a static utility class that provides a set of methods to work with CSS animations, generate CSS strings, manipulate CSS variables, and more. It is now also equipped with extension methods to use with IJSRuntime.

## Methods

### GetAnimationCssStyle (AnimationType, TimeSpan, AnimationDirection?, AnimationTimingFunction, DialogPosition?)

Generates a CSS animation string for a single animation type. Now available as an extension method to IJSRuntime.

#### Parameters

- `type`: The `AnimationType` to be used for the animation.
- `duration`: The duration of the animation as a `TimeSpan`.
- `direction` (optional): The `AnimationDirection` for the animation. Default is `null`.
- `animationTimingFunction` (optional): The `AnimationTimingFunction` for the animation. Default is `null`.
- `targetPosition` (optional): The `DialogPosition` for the animation. Default is `null`.

#### Example

```csharp
var animationCss = AnimationType.SlideIn.GetAnimationCssStyle(TimeSpan.FromSeconds(1), AnimationDirection.In);
```

### GetAnimationCssStyle (AnimationType[], TimeSpan, AnimationDirection?, AnimationTimingFunction, DialogPosition?)

Generates a CSS animation string for an array of animation types. Now available as an extension method to IJSRuntime.

#### Parameters

- `types`: An array of `AnimationType` to be used for the animations.
- `duration`: The duration of the animations as a `TimeSpan`.
- `direction` (optional): The `AnimationDirection` for the animations. Default is `null`.
- `animationTimingFunction` (optional): The `AnimationTimingFunction` for the animations. Default is `null`.
- `targetPosition` (optional): The `DialogPosition` for the animations. Default is `null`.

#### Example

```csharp
var animationTypes = new AnimationType[] { AnimationType.SlideIn, AnimationType.FadeIn };
var animationCss = animationTypes.GetAnimationCssStyle(TimeSpan.FromSeconds(1));
```

### Get (Classes, Classes[])

Quickly accesses classes.

#### Parameters

- `cls`: A class of type `Classes`.
- `other` (optional): Other classes to be accessed. Default is `null`.

#### Example

```csharp
var classString = MudExCss.Get(MudExCss.Classes.Dialog.FullHeightContent, "overflow-hidden", MudExCss.Classes.Dialog._Initial);
```

### CreateStyle (Action<MudExStyleBuilder>)

Creates a style builder with applied styles from given action.

#### Parameters

- `action` (optional): An action to be performed on the `MudExStyleBuilder`. Default is `null`.

#### Example

```csharp
var styleBuilder = MudExCss.CreateStyle(builder => builder.Add("color", "red"));
```

### Obsolete Methods

Following methods are now obsolete and should be used as `MudExStyleBuilder` or `IJSRuntime` extension methods instead.

- `GenerateCssString(object, string)`
- `GenerateCssString(object, CssUnit, string)`
- `CombineCSSStrings(string, string)`
- `CssStringToObject<T>(string)`
- `GetCssVariablesAsync()`
- `FindCssVariablesByValueAsync(string)`
- `SetCssVariableValueAsync(KeyValuePair<string, string>)`
- `SetCssVariableValueAsync(string, object, object[])`
- `SetCssVariableValueAsync(string, object)`
- `SetCssVariableValueAsync(string, Color)`
- `SetCssVariableValueAsync(string, MudColor)`
- `SetCssVariableValueAsync(string, string)`
- `GetCssColorVariablesAsync()`
```

### Deprecated Examples

#### GenerateCssString

```csharp
var cssObj = new { width = 100, height = 200, backgroundColor = "red" };
var cssString = MudExCss.GenerateCssString(cssObj); // Deprecated
```

#### SetCssVariableValueAsync

```csharp
var task = MudExCss.SetCssVariableValueAsync("color", "red"); // Deprecated
```

These deprecated methods are replaced by the use of `MudExStyleBuilder` or `IJSRuntime` extension methods. For more details, refer to the documentation of these new methods.