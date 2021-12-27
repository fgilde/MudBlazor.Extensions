# MudBlazor.Extensions
MudBlazor.Extensions is a small extension for MudBlazor from https://mudblazor.com/

### Using
Using is as easy it can be
Sure you need a MudBlazor project and the referenced package to MudBlazor for more informations and help see https://mudblazor.com/ and https://github.com/MudBlazor/Templates

Add the nuget Package `MudBlazor.Extensions` to your blazor project

Now you can start using it.
Currently we only have extensions for the Mud Dialog Service

#### Make dialogs resizeable or draggable

```csharp
       var options = new DialogOptionsEx { Resizeable = true, DragMode = MudDialogDragMode.Simple, CloseButton = true,  FullWidth = true };
       var dialog = await _dialogService.ShowEx<YourMudDialog>("your dialog title", parameters, options);
```

#### Add Maximize Button

```csharp
       var options = new DialogOptionsEx { MaximizeButton = true, CloseButton = true};
       var dialog = await _dialogService.ShowEx<YourMudDialog>("your Dialog title", parameters, options);
```

#### Add Custom Buttons

First in your component code you need to define the callback methods as `JSInvokable`

```csharp
        [JSInvokable]
        public void AlarmClick()
        {
           // OnAlarmButton Click
        }

        [JSInvokable]
        public void ColorLensClick()
        {
           // OnColorLensButton Click
        }
```

Then define your custom buttons

```csharp
          var buttons = new[]
            {
                new MudDialogButton( DotNetObjectReference.Create(this as object), nameof(AlarmClick)) {Icon = Icons.Filled.Alarm},
                new MudDialogButton( DotNetObjectReference.Create(this as object), nameof(ColorLensClick)) {Icon = Icons.Filled.ColorLens},
            };
```

```csharp
       var options = new DialogOptionsEx { MaximizeButton = true, CloseButton = true, Buttons = buttons};
       var dialog = await _dialogService.ShowEx<YourMudDialog>("your dialog title", parameters, options);
```

Now a dialog can look like this

[![SAMPLE](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/sampleDialogScreenshot.png)]

Use animation to show dialog

```csharp
       var options = new DialogOptionsEx { MaximizeButton = true, CloseButton = true, Buttons = buttons, Position = DialogPosition.CenterRight, Animation = AnimationType.SlideIn, FullHeight = true};
       var dialog = await _dialogService.ShowEx<YourMudDialog>("your dialog title", parameters, options);
```


[![SAMPLE](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/slideIn.gif)]

#### Change Log
 - 1.2.0 Slide in animations for dialogs. 
 - 1.1.2 New option FullHeight for dialogs

#### Planned Features
Notice this is just a first preview version. 
There are some features planned like
 - Dragging with snap behaviour
 - Charm in and out behavior for left, right, top and bottom
 - Automatic generation for a dialog to edit given model
 
