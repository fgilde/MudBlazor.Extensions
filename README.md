# MudBlazor.Extensions
MudBlazor.Extensions is a small extension for MudBlazor from https://mudblazor.com/

[Running Sample Application](https://mudex.azurewebsites.net/)

### Using / Prerequirements
Using is as easy it can be
Sure you need a MudBlazor project and the referenced package to MudBlazor for more informations and help see https://mudblazor.com/ and https://github.com/MudBlazor/Templates

Add the nuget Package `MudBlazor.Extensions` to your blazor project

```
<PackageReference Include="MudBlazor.Extensions" Version="1.6.62" />
```

For easier using the components should change your `_Imports.razor` and add this entries.

```csharp
@using MudBlazor.Extensions
@using MudBlazor.Extensions.Components
@using MudBlazor.Extensions.Components.ObjectEdit
```

Register the MudBlazor.Extensions in your `Startup.cs` in the `ConfigureServices` method.
> **_NOTICE:_** You can pass Assemblies params to search and add the possible service implementations for `IObjectMetaConfiguration` and `IDefaultRenderDataProvider` automaticly. If you don't pass any Assembly the MudBlazor.Extensions will search in the Entry and calling Assembly.

```csharp
// use this to add MudServices and the MudBlazor.Extensions
builder.Services.AddMudServicesWithExtensions();

// or this to add only the MudBlazor.Extensions
builder.Services.AddMudExtensions();
```

Because the dialog extensions are static you need to set the IJSRuntime somewhere in your code for example in your `App.razor` or `MainLayout.razor` in the `OnAfterRenderAsync` method.
This is not required but otherwise you need to pass the IJSRuntime in every `DialogOptionsEx`
If I find a better solution I will change this.
```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
        await JsRuntime.InitializeMudBlazorExtensionsAsync();
    await base.OnAfterRenderAsync(firstRender);
}
```

## Components
#### MudExObjectEdit !NEW
`MudExObjectEdit` is a powerfull component to edit objects and automatically render the whole UI. 
You can also use the `MudExObjectEditForm` to have automatic validation and submit.
Validation works automatically for DataAnnotation Validations or fluent registered validations for your model.
The easiest way to use it is to use the `MudExObjectEditForm` and pass your model to it.
```csharp
<MudExObjectEditForm OnValidSubmit="@OnSubmit" Value="@MyModel"></MudExObjectEditForm>
```

You can also use the `MudExObjectEditDialog` to edit you model in a dialog. The easieest way to do this is to use the extension method `EditObject` on the `IDialogService`.
```csharp
dialogService.EditObject(User, "Dialog Title", dialogOptionsEx);
```

[More Informations of MudExObjectEdit you can find here](https://github.com/fgilde/MudBlazor.Extensions/blob/main/MudBlazor.Extensions/Docs/ObjectEdit.md)



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

If you animate a dialog with dialogServiceEx, you should add the class `mud-ex-dialog-initial` to your dialog to ensure no visibility before animation.
Currently you can use following animations: `SlideIn,FadeIn,Scale,Slide,Fade,Zoom,Roll,JackInTheBox,Hinge,Rotate,Bounce,Back,Jello,Wobble,Tada,Swing,HeadShake,Shake,RubberBand,Pulse,Flip,FlipX,FlipY`

```csharp
    <MudDialog Class="mud-ex-dialog-initial">
```
> **_BETA (Work still in progress):_** All animations can currently also used on other components for example in this popover. `<MudPopover Style="@(IsOpen $"animation: {new [] {AnimationType.FadeIn, AnimationType.SlideIn}.GetAnimationCssStyle(TimeSpan.FromSeconds(1))}" : "")">Popover content</MudPopover>`

#### Remove need of DialogParameters
Also you can call our extension method with an `Action<YourDialog>` instead of DialogParameters.

```csharp
    await dialogService.ShowEx<SampleDialog>("Simple Dialog", dialog => { dialog.ContentMessage = "Hello"; },options);
```


#### Change Log
 - 1.5.0 Add `MudExObjectEdit` `MudExObjectEditForm` `MudExObjectEditDialog` and `MudExCollectionEditor`
 - 1.4.6 Registered Localizer is no longer a requirement
 - 1.4.0 Add New Component `MudExEnumSelect`
 - 1.2.8 Add New Component `MudExChipSelect`
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
