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

## Image Viewer Without ImageSharp (9.11.2)

`SixLabors.ImageSharp` is no longer referenced. `MudExImageViewerSaveOptions.GetImageFormat()` and the static
`GetImageFormat(ImageViewerExportFormat)` returned ImageSharp's `IImageFormat` and were removed. Use
`MudExImageViewerSaveOptions.GetFileType(format)`, which returns the file extension and mime type.

**Reason:** ImageSharp 3.x has unpatched security advisories (TIFF decoding among them) and the fixed 4.x versions
require a commercial license. Decoding and encoding now run in the browser, see `MudExImageCodecs.js`.

`ImageViewerExportFormat.Pbm` still writes a binary color pixmap (`.ppm`), as before.

---

## MudExFileDisplayCode Is an Editor

`MudExFileDisplayCode` now derives from `MudExFileEditorBase` and renders an editable textarea unless
`ReadOnly` is set. Inside `MudExFileDisplay` the display sets `ReadOnly` from its edit mode, so nothing changes there.
If you render `<MudExFileDisplayCode>` directly and want the previous highlighted, read-only view, add `ReadOnly="true"`.

**Reason:** text and code files can be edited and saved through the shared file editor pipeline.

---

## Excel Viewers Replaced by `MudExSpreadsheetEditor`

`MudExFileDisplayExcel` (ExcelDataReader grid) and `MudExFileDisplayExcelUniver` (Univer from a CDN) are removed together with
`IMudExFileService.ReadExcelFile`, `ExcelFile`, `ExcelSheet` and `ExcelRow`. `MudExFileDisplay` now opens xlsx, xlsm, xls, csv and tsv
in `MudExSpreadsheetEditor`, read-only unless the display is in edit mode (the display sets `ReadOnly` from its edit mode).

**Reason:** one spreadsheet component that reads, edits and saves xlsx, xls and csv without loading scripts from a CDN.

**Before:**
```razor
<MudExFileDisplayExcel Src="@url" />
```
```csharp
var excel = fileService.ReadExcelFile(stream, contentType);
```

**After:**
```razor
<MudExFileDisplay Url="@url" />
<MudExSpreadsheetEditor FileDisplayInfos="@infos" ReadOnly="true" />
```
```csharp
var workbook = XlsxReader.Read(stream);
```

---

## Presentation Viewer Replaced by `MudExSlideEditor`

`MudExFileDisplayPresentation` is removed. `MudExFileDisplay` now opens pptx in `MudExSlideEditor`, read-only unless the display is in
edit mode (the display sets `ReadOnly` from its edit mode). The speaker notes pane of the old viewer is the editor's `ShowNotes` parameter.

**Reason:** one slide component that views, edits and saves pptx without losing content it does not model.

**Before:**
```razor
<MudExFileDisplayPresentation FileDisplayInfos="@infos" />
```

**After:**
```razor
<MudExFileDisplay Url="@url" />
<MudExSlideEditor FileDisplayInfos="@infos" ReadOnly="true" ShowNotes="true" />
```

---

## XML Nested Deeper Than 256 Levels Is Rejected

Files whose XML is nested deeper than 256 elements are no longer read. `XlsxReader.Read` and the pptx reader throw
`InvalidDataException`, the OpenDocument, XPS, GPX and KML viewers fail with `InvalidDataException`, the e-invoice reader returns
`null` and EPUB books fail with `FormatException` (no package found).

**Reason:** recursive processing of such XML could overflow the stack and end the process, which no `catch` can prevent.

---

## DTDs Are Prohibited in Viewer XML

The OpenDocument viewer, XPS, GPX/KML and the e-invoice reader now reject XML with a document type declaration
(`XmlException` or `InvalidDataException`, the e-invoice reader returns `null`); before, internal entities were expanded. EPUB
ignores DTDs (valid books carry `<!DOCTYPE html>`), so entities in EPUB files are no longer expanded.

**Reason:** DTD processing allows entity expansion attacks and is not needed by any of these formats.

---

## `HtmlTableFormat` Raw-Text Elements, Closing Tags and CDATA

The clipboard HTML tokenizer behind `HtmlTableFormat.Read` skips the content of `script`, `style`, `textarea`, `title`, `noscript`,
`iframe`, `noembed`, `noframes`, `template` and `xmp` up to the matching closing tag (a closing tag must be followed by a character that
cannot continue the tag name, so `</scripty>` does not end a `script`), or to the end of the input when there is none. `svg`, `math` and
`object` are skipped the same way unless written self-closing. Any other self-closing raw-text tag without a closing tag later in the
input (`<title/>` in an XHTML head, `<script src="x"/>`) is an empty element and the rest of the input is read. Comments and CDATA
sections are skipped and never produce text.

**Reason:** the previous tokenizer could take text inside such elements for table content, and an XHTML `<title/>` swallowed the whole
document.

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
