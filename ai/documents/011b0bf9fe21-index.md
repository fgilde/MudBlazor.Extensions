# Extensions Overview

MudBlazor.Extensions provides powerful extension methods that enhance the functionality of standard MudBlazor components.

## Available Extensions

- **[Dialog Extensions](https://github.com/fgilde/MudBlazor.Extensions/blob/0ceb7f3a4cd928b003ae41cb95070ce9b32395f6/docs/extensions/dialog-extensions.md)** - Resizable, draggable dialogs with animations
- **[Browser Extensions](https://github.com/fgilde/MudBlazor.Extensions/blob/0ceb7f3a4cd928b003ae41cb95070ce9b32395f6/docs/extensions/browser-extensions.md)** - Browser and file utilities

## Quick Example

```csharp
// Enhanced dialog with animations
var options = new DialogOptionsEx { 
    Resizeable = true,
    DragMode = MudDialogDragMode.Simple,
    Animation = AnimationType.SlideIn
};
await dialogService.ShowEx<MyDialog>("Title", parameters, options);
```
