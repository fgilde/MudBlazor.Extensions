# Extensions Overview

MudBlazor.Extensions provides powerful extension methods that enhance the functionality of standard MudBlazor components.

## Available Extensions

- **[Dialog Extensions](https://github.com/fgilde/MudBlazor.Extensions/blob/fd8e30adddf8877e69e672278398a2b95d86989e/docs/extensions/dialog-extensions.md)** - Resizable, draggable dialogs with animations
- **[Browser Extensions](https://github.com/fgilde/MudBlazor.Extensions/blob/fd8e30adddf8877e69e672278398a2b95d86989e/docs/extensions/browser-extensions.md)** - Browser and file utilities

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
