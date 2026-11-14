# Extensions Overview

MudBlazor.Extensions provides powerful extension methods that enhance the functionality of standard MudBlazor components.

## Available Extensions

- **[Dialog Extensions](https://github.com/fgilde/MudBlazor.Extensions/blob/73117ddd6169a0e2f1cb0a987e95e4bfdbfbefab/docs/extensions/dialog-extensions.md)** - Resizable, draggable dialogs with animations
- **[Browser Extensions](https://github.com/fgilde/MudBlazor.Extensions/blob/73117ddd6169a0e2f1cb0a987e95e4bfdbfbefab/docs/extensions/browser-extensions.md)** - Browser and file utilities

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
