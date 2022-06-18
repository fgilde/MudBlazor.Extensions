# MudBlazor.Extensions
MudBlazor.Extensions is a small extension for MudBlazor from https://mudblazor.com/

### Using
Using is as easy it can be
Sure you need a MudBlazor project and the referenced package to MudBlazor for more informations and help see https://mudblazor.com/ and https://github.com/MudBlazor/Templates

Add the nuget Package `MudBlazor.Extensions` to your blazor project

Now you can start using it.
However if you want to use the Extensions components as well you should change your `_Imports.razor` and add this entries.

```csharp
@using MudBlazor.Extensions
@using MudBlazor.Extensions.Components
```

## Components
#### MudExFileDisplay
A Component to display file contents for example as preview before uploading or for referenced files.
This components automatically tries to display as best as possible and can handle urls or streams directly.
You can use it like this

```xml
 <MudExFileDisplay FileName="NameOfYourFile.pdf" ContentType="application/pdf" Url="@Url"></MudExFileDisplay>
```
![SAMPLE](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/Screenshots/FileDisplayPdf.png)

#### MudExFileDisplayZip 
 This component is also automatically used by `MudExFileDisplay` but can also used manually if you need to.

 ```xml
 <MudExFileDisplayZip AllowDownload="@AllowDownload" RootFolderName="@FileName" ContentStream="@ContentStream" Url="@Url"></MudExFileDisplayZip>
```
![SAMPLE](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/Screenshots/FileDisplayZip.png)

#### MudExFileDisplayDialog
A small dialog for the `MudExFileDisplay` Component. Can be used with static helpers to show like this

```csharp
 await MudExFileDisplayDialog.Show(_dialogService, dataUrl, request.FileName, request.ContentType, ex => ex.JsRuntime = _jsRuntime);
```

Can be used directly with an IBrowserFile
```csharp
 IBrowserFile file = File;
 await MudExFileDisplayDialog.Show(_dialogService, file, ex => ex.JsRuntime = _jsRuntime);
```

Can also be used completely manually with MudBlazor dialogService
```csharp
var parameters = new DialogParameters
{
    {nameof(Icon), BrowserFileExtensions.IconForFile(contentType)},
    {nameof(Url), url},
    {nameof(ContentType), contentType}
};
await dialogService.ShowEx<MudExFileDisplayDialog>(title, parameters, optionsEx);
```

![SAMPLE](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/Screenshots/FileDisplayDialog.gif)

#### (Planned)

One of the next planned Component is an Multi upload component with Features like duplicate check, max size, specific allowed content types, max items, zip auto extract and many more. 
The current State looks like this

![SAMPLE](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/Screenshots/UploadEdit.gif)
<br>
<a href="https://github.com/fgilde/MudBlazor.Extensions/blob/main/MudBlazor.Extensions/Screenshots/UploadEdit.mkv?raw=true" target="_blank">Download Video</a>

## Extensions

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

![SAMPLE](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/sampleDialogScreenshot.png)

Use animation to show dialog

```csharp
       var options = new DialogOptionsEx { 
           MaximizeButton = true, 
           CloseButton = true, 
           Buttons = buttons, 
           Position = DialogPosition.CenterRight, 
           Animation = AnimationType.SlideIn, 
           AnimationDuration = TimeSpan.FromMilliseconds(500),
           FullHeight = true
       };
       var dialog = await _dialogService.ShowEx<YourMudDialog>("your dialog title", parameters, options);
```


![SAMPLE](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/Screenshots/slideIn.gif)

#### Change Log
 - 1.2.6 Add New Animationtypes for dialog or manual using
 - 1.2.4 Add Components `MudExFileDisplay` `MudExFileDisplayZip` and `MudExFileDisplayDialog`
 - 1.2.2 Animations can be combined
 - 1.2.2 Add animation fade
 - 1.2.2 Improved animations for dialogs
 - 1.2.0 Slide in animations for dialogs. 
 - 1.1.2 New option FullHeight for dialogs

#### Planned Features
Notice this is just a first preview version. 
There are some features planned like
 - Multi upload component with preview and more
 - Dragging with snap behaviour 
 - Automatic generation for a dialog to edit given model
 

 #
## Links
[Github Repository](https://github.com/fgilde/MudBlazor.Extensions) | 
[Nuget Package](https://www.nuget.org/packages/MudBlazor.Extensions/)
#
