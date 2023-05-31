# MudExCssBuilder Class Documentation

---

## Class Description

The `MudExCssBuilder` class is a utility class that helps in building CSS classes dynamically. It implements the `IAsyncDisposable` and `IMudExClassAppearance` interfaces.

It uses concurrent dictionaries for storing CSS classes to ensure thread-safety. It also stores disposable objects which are disposed off when the `MudExCssBuilder` is disposed.

---

## Public Properties

- `Default` : Provides a new instance of `MudExCssBuilder`.
- `Class` : Returns the current CSS class string.

---

## Public Methods

### Factory Methods

- `From(string cls, params string[] other)`
- `From(MudExCss.Classes cls, params MudExCss.Classes[] other)`
- `From(MudExCssBuilder builder)`
- `FromStyleAsync(object styleObj)`
- `FromStyleAsync(string style)`
- `FromStyleAsync(MudExStyleBuilder styleBuilder)`

### AddClass Methods

- `AddClass(string value)`
- `AddClass(MudExCss.Classes cssClass, bool when)`
- `AddClass(MudExCss.Classes cssClass, params MudExCss.Classes[] other)`
- `AddClass(string value, params string[] other)`
- `AddClass(string value, bool when)`
- `AddClass(string value, Func<bool> when)`
- `AddClass(Func<string> value, bool when = true)`
- `AddClass(Func<string> value, Func<bool> when)`
- `AddClass(CssBuilder builder, bool when = true)`
- `AddClass(CssBuilder builder, Func<bool> when)`
- `AddClass(MudExCssBuilder builder, bool when = true)`
- `AddClass(MudExCssBuilder builder, Func<bool> when)`

### AddClassFromStyleAsync Methods

- `AddClassFromStyleAsync(MudExStyleBuilder builder, bool when = true)`
- `AddClassFromStyleAsync(MudExStyleBuilder builder, Func<bool> when)`
- `AddClassFromStyleAsync(object styleObject, bool when = true)`
- `AddClassFromStyleAsync(string styleString, bool when = true)`
- `AddClassFromStyleAsync(object styleObject, Func<bool> when)`
- `AddClassFromStyleAsync(string styleString, Func<bool> when)`

### Other Methods

- `RemoveClassesAsync(params string[] values)` : Asynchronously removes the specified CSS classes.
- `AddClassFromAttributes(IReadOnlyDictionary<string, object> additionalAttributes)` : Adds CSS classes from additional attributes.
- `Build()` : Builds and returns the CSS class string.
- `DisposeAsync()` : Disposes off the disposable objects stored in `_disposables`.
