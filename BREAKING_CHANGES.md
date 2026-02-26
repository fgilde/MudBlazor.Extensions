# Breaking Changes - MudBlazor.Extensions v9

This document outlines the breaking changes introduced in MudBlazor.Extensions v9 to align with [MudBlazor v9 API conventions](https://github.com/MudBlazor/MudBlazor/issues/12666).

## Overview

MudBlazor v9 introduced consistent async naming across its APIs, renaming methods like `Show` to `ShowAsync` and `ShowMessageBox` to `ShowMessageBoxAsync`. MudBlazor.Extensions v9 aligns its own public API with these conventions.

All renamed methods retain their previous versions marked with `[Obsolete]` to ease migration. The obsolete methods delegate to the new async implementations and will be removed in a future major version.

---

## Dialog Service Extension Methods

### `Show<TDialog>` → `ShowAsync<TDialog>`

The `DialogServiceExt.Show<TDialog>` extension methods on `IDialogService` have been renamed to `ShowAsync<TDialog>` and now return `Task<IMudExDialogReference<TDialog>>` instead of `IMudExDialogReference<TDialog>`.

**Reason:** Aligns with MudBlazor v9's rename of `IDialogService.Show` to `IDialogService.ShowAsync`. The new methods also properly call MudBlazor's `ShowAsync` internally, fixing a potential issue with the previous synchronous wrappers.

**Before:**
```csharp
var dialog = dialogService.Show<MyDialog>("Title", dialogParameters, options);
```

**After:**
```csharp
var dialog = await dialogService.ShowAsync<MyDialog>("Title", dialogParameters, options);
```

**Affected overloads:**

| Old Method | New Method |
|---|---|
| `Show<TDialog>(title, Action<TDialog>, Action<DialogOptions>)` | `ShowAsync<TDialog>(title, Action<TDialog>, Action<DialogOptions>)` |
| `Show<TDialog>(title, TDialog, Action<DialogOptions>)` | `ShowAsync<TDialog>(title, TDialog, Action<DialogOptions>)` |
| `Show<TDialog>(title, TDialog, DialogOptions)` | `ShowAsync<TDialog>(title, TDialog, DialogOptions)` |
| `Show<TDialog>(title, Action<TDialog>, DialogOptions)` | `ShowAsync<TDialog>(title, Action<TDialog>, DialogOptions)` |

---

## Dialog Close/Cancel Extension Methods

### `CloseAnimated` → `CloseAnimatedAsync`

### `CancelAnimated` → `CancelAnimatedAsync`

### `CloseAnimatedIf` → `CloseAnimatedIfAsync`

### `CancelAnimatedIf` → `CancelAnimatedIfAsync`

The `MudDialogInstanceExtensions` close/cancel methods have been renamed with an `Async` suffix and now return `Task` instead of `void`.

**Reason:** Aligns with .NET and MudBlazor v9 conventions where asynchronous methods use the `Async` suffix. The previous `void` methods discarded the underlying async operation's result, which could mask exceptions. The new methods allow callers to properly `await` the close animation.

**Before:**
```csharp
MudDialog.CloseAnimatedIf(DialogResult.Ok(true), JsRuntime);
MudDialog.CancelAnimatedIf(JsRuntime);
```

**After:**
```csharp
await MudDialog.CloseAnimatedIfAsync(DialogResult.Ok(true), JsRuntime);
await MudDialog.CancelAnimatedIfAsync(JsRuntime);
```

**Affected methods on `IMudDialogInstance`:**

| Old Method | New Method |
|---|---|
| `CloseAnimated(jsRuntime)` | `CloseAnimatedAsync(jsRuntime)` |
| `CancelAnimated(jsRuntime)` | `CancelAnimatedAsync(jsRuntime)` |
| `CloseAnimated(result, jsRuntime)` | `CloseAnimatedAsync(result, jsRuntime)` |
| `CloseAnimated<T>(result, jsRuntime)` | `CloseAnimatedAsync<T>(result, jsRuntime)` |
| `CloseAnimatedIf(jsRuntime)` | `CloseAnimatedIfAsync(jsRuntime)` |
| `CancelAnimatedIf(jsRuntime)` | `CancelAnimatedIfAsync(jsRuntime)` |
| `CloseAnimatedIf(result, jsRuntime)` | `CloseAnimatedIfAsync(result, jsRuntime)` |
| `CloseAnimatedIf<T>(result, jsRuntime)` | `CloseAnimatedIfAsync<T>(result, jsRuntime)` |

**Affected methods on `IDialogReference`:**

| Old Method | New Method |
|---|---|
| `CloseAnimated(jsRuntime)` | `CloseAnimatedAsync(jsRuntime)` |
| `CancelAnimated(jsRuntime)` | `CancelAnimatedAsync(jsRuntime)` |
| `CloseAnimated(result, jsRuntime)` | `CloseAnimatedAsync(result, jsRuntime)` |
| `CloseAnimated<T>(result, jsRuntime)` | `CloseAnimatedAsync<T>(result, jsRuntime)` |
| `CloseAnimatedIf(jsRuntime)` | `CloseAnimatedIfAsync(jsRuntime)` |
| `CancelAnimatedIf(jsRuntime)` | `CancelAnimatedIfAsync(jsRuntime)` |
| `CloseAnimatedIf(result, jsRuntime)` | `CloseAnimatedIfAsync(result, jsRuntime)` |
| `CloseAnimatedIf<T>(result, jsRuntime)` | `CloseAnimatedIfAsync<T>(result, jsRuntime)` |

---

## Previously Deprecated Methods (Reminder)

The following methods were already deprecated in earlier versions and continue to point to their async replacements:

| Old Method | Replacement |
|---|---|
| `ShowEx<TDialog>(...)` | `ShowExAsync<TDialog>(...)` |
| `ShowMessageBoxEx(...)` | `ShowMessageBoxExAsync(...)` |
| `ShowFileDisplayDialog(...)` | `ShowFileDisplayDialogAsync(...)` |
| `ShowObject<TModel>(...)` | `ShowObjectAsync<TModel>(...)` |
| `EditObject<TModel>(...)` | `EditObjectAsync<TModel>(...)` |
| `ShowStructuredDataString(...)` | `ShowStructuredDataStringAsync(...)` |
| `EditStructuredDataString(...)` | `EditStructuredDataStringAsync(...)` |

---

## Migration Guide

1. **Find and replace** method calls in your code:
   - `CloseAnimated(` → `CloseAnimatedAsync(`
   - `CancelAnimated(` → `CancelAnimatedAsync(`
   - `CloseAnimatedIf(` → `CloseAnimatedIfAsync(`
   - `CancelAnimatedIf(` → `CancelAnimatedIfAsync(`
   - `.Show<` (on `IDialogService`) → `.ShowAsync<`

2. **Add `await`** to all renamed method calls, since they now return `Task`.

3. **Update method signatures** if the calling method was `void` — change it to `async Task` or `async void` (for event handlers only).

4. The old method names still work but will produce compiler warnings. They will be removed in a future major version.
