[![GitHub](https://img.shields.io/github/license/garderoben/mudblazor?color=594ae2&style=flat-square&logo=github)](https://github.com/fgilde/MudBlazor.Extensions/blob/master/LICENSE)
[![GitHub](https://img.shields.io/github/license/garderoben/mudblazor?color=594ae2&style=flat-square&logo=github)](https://github.com/fgilde/MudBlazor.Extensions/blob/master/LICENSE)
[![GitHub last commit](https://img.shields.io/github/last-commit/fgilde/MudBlazor.Extensions?color=594ae2&style=flat-square&logo=github)](https://github.com/fgilde/MudBlazor.Extensions)
[![Nuget version](https://img.shields.io/nuget/v/MudBlazor.Extensions?color=ff4081&label=nuget%20version&logo=nuget&style=flat-square)](https://www.nuget.org/packages/MudBlazor.Extensions/)
[![Website](https://img.shields.io/website?label=mudex.org&url=http%3A%2F%2Fmudex.org)](https://www.mudex.org/)


# MudBlazor.Extensions
MudBlazor.Extensions is a extension library for MudBlazor Component library from https://mudblazor.com


## Demos
[![Azure](https://img.shields.io/badge/Azure-Demo-blue)](https://mudex.azurewebsites.net/) 
[![Cloudflare](https://img.shields.io/badge/Cloudflare-Demo-blue)](https://mudblazor-extensions.pages.dev)
#


<!---
[Running Sample Application (Github Pages)](https://fgilde.github.io/MudBlazor.Extensions/sample/wwwroot/)
-->




# MudBlazor.Extensions

The MudBlazor.Extensions is a convenient package that extends the capabilities of the MudBlazor component library. This guide will demonstrate the setup process for your project, along with detailed explanations of the components, extensions, and functionalities provided.

It's important to note that this package requires a MudBlazor project and the referenced MudBlazor package. For further information and assistance, please visit the official MudBlazor documentation at [MudBlazor](https://mudblazor.com/) and [MudBlazor/Templates](https://github.com/MudBlazor/Templates).

## Table of Contents
- [Installation](#installation)
- [Setting Up MudBlazor.Extensions](#setting-up-mudblazor-extensions)
- [Components](#components)
  - [MudExObjectEdit](#mudexobjectedit)
  - [MudExFileDisplay](#mudexfiledisplay)
  - [MudExFileDisplayZip](#mudexfiledisplayzip)
  - [MudExFileDisplayDialog](#mudexfiledisplaydialog)
  - [MudExUploadEdit](#mudexuploadedit)
- [Extensions](#extensions)
  - [Making Dialogs Resizable and Draggable](#making-dialogs-resizable-and-draggable)
  - [Adding a Maximize Button](#adding-a-maximize-button)
  - [Adding Custom Buttons](#adding-custom-buttons)
  - [Animation for Showing Dialog](#animation-for-showing-dialog)
  - [Removing Need of DialogParameters](#removing-need-of-dialogparameters)

## Installation

The installation process is straightforward. All you need to do is add the `MudBlazor.Extensions` NuGet package to your Blazor project. You can do this using the following code:

```
<PackageReference Include="MudBlazor.Extensions" Version="*" />
```

## Setting Up MudBlazor.Extensions

Setting up MudBlazor.Extensions involves three steps:

1. Update the `_Imports.razor` with the following lines:

```
@using MudBlazor.Extensions
@using MudBlazor.Extensions.Components
@using MudBlazor.Extensions.Components.ObjectEdit
```

2. Register MudBlazor.Extensions in your `Startup.cs` in the `ConfigureServices` method.

```
// use this to add MudServices and the MudBlazor.Extensions
builder.Services.AddMudServicesWithExtensions();

// or this to add only the MudBlazor.Extensions
builder.Services.AddMudExtensions();
```

3. (Optional) Define default dialogOptions.

```
builder.Services.AddMudServicesWithExtensions(c =>
{
    c.WithDefaultDialogOptions(ex =>
    {
        ex.Position = DialogPosition.BottomRight;
    });
});
```

Please note: The dialog extensions are static, hence, you need to set the IJSRuntime somewhere in your code, for example, in your `App.razor` or `MainLayout.razor` in the `OnAfterRenderAsync` method. This is not a requirement but it does save you from passing the IJSRuntime in every `DialogOptionsEx`.

```
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
        await JsRuntime.InitializeMudBlazorExtensionsAsync();
    await base.OnAfterRenderAsync(firstRender);
}
```

## Components

This section introduces you to the various components provided by the MudBlazor.Extensions.

### MudExObjectEdit
The `MudExObjectEdit` is a robust component that allows for object editing and automatically generates the corresponding UI. This component supports automatic validation for DataAnnotation Validations or fluent registered validations for your model.

To use `MudExObjectEdit`, you can simply use the `MudExObjectEditForm` and pass your model to it as shown below:

```
<MudExObjectEditForm OnValidSubmit="@OnSubmit" Value="@MyModel"></MudExObjectEditForm>
```

You can also utilize the `MudExObjectEditDialog` to edit your model in a dialog. The easiest way to do this is by using the extension method `EditObject` on the `IDialogService`.

```
dialogService.EditObject(User, "Dialog Title", dialogOptionsEx);
```

For more information about MudExObjectEdit, you can check [here](https://github.com/fgilde/MudBlazor.Extensions/blob/main/MudBlazor.Extensions/Docs/ObjectEdit.md).

### MudExFileDisplay
The `MudExFileDisplay` component is designed to display file contents, such as a preview before uploading or for referenced files. This component can automatically handle URLs or streams and deliver the best possible display. Additionally, you can implement `IMudExFileDisplay` in your own component to register a custom file display.

Example of using `MudExFileDisplay`:

```
 <MudExFileDisplay FileName="NameOfYourFile.pdf" ContentType="application/pdf" Url="@Url"></MudExFileDisplay>
```

![SAMPLE](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/Screenshots/FileDisplayPdf.png)

### MudExFileDisplayZip 
This component can be automatically utilized by `MudExFileDisplay`, but can also be used manually if necessary.

```
 <MudExFileDisplayZip AllowDownload="@AllowDownload" RootFolderName="@FileName" ContentStream="@ContentStream" Url="@Url"></MudExFileDisplayZip>
```

![SAMPLE](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/Screenshots/FileDisplayZip.png)

### MudExFileDisplayDialog
A small dialog for the `MudExFileDisplay` Component. It can be used with static helpers as shown below:

```
 await MudExFileDisplayDialog.Show(_dialogService, dataUrl, request.FileName, request.ContentType, ex => ex.JsRuntime = _jsRuntime);
```

It can be used directly with an IBrowserFile:

```
 IBrowserFile file = File;
 await MudExFileDisplayDialog.Show(_dialogService, file, ex => ex.JsRuntime = _jsRuntime);
```

Or it can be used manually with the MudBlazor dialogService:

```
var parameters = new DialogParameters
{
    {nameof(Icon), BrowserFileExtensions.IconForFile(contentType)},
    {nameof(Url), url},
    {nameof(ContentType), contentType}
};
await dialogService.ShowEx<MudExFileDisplayDialog>(title, parameters, optionsEx);
```

![SAMPLE](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/Screenshots/FileDisplayDialog.gif)

### MudExUploadEdit

This component provides multi-file upload functionality, with features like duplicate checks, max size, specific allowed content types, max items, zip auto-extract, and many more.

![SAMPLE](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/Screenshots/UploadEdit.gif)
[Download Video](https://github.com/fgilde/MudBlazor.Extensions/blob/main/MudBlazor.Extensions/Screenshots/UploadEdit.mkv?raw=true)

## Extensions

### Resizable or Draggable Dialogs

You can make your dialogs resizable or draggable using the following code snippet:

```
var options = new DialogOptionsEx { Resizeable = true, DragMode = MudDialogDragMode.Simple, CloseButton = true, FullWidth = true };
var dialog = await _dialogService.ShowEx<YourMudDialog>("Your Dialog Title", parameters, options);
```

### Adding a Maximize Button

You can add a maximize button to your dialogs with the following code:

```
var options = new DialogOptionsEx { MaximizeButton = true, CloseButton = true };
var dialog = await _dialogService.ShowEx<YourMudDialog>("Your Dialog Title", parameters, options);
```

### Adding Custom Buttons

To add custom buttons to your dialog, first define the callback methods as `JSInvokable` in your component code:

```
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

Next, define your custom buttons:

```
var buttons = new[]
{
    new MudDialogButton( DotNetObjectReference.Create(this as object), nameof(AlarmClick)) {Icon = Icons.Filled.Alarm},
    new MudDialogButton( DotNetObjectReference.Create(this as object), nameof(ColorLensClick)) {Icon = Icons.Filled.ColorLens},
};
```

Finally, add your custom buttons to the dialog:

```
var options = new DialogOptionsEx { MaximizeButton = true, CloseButton = true, Buttons = buttons };
var dialog = await _dialogService.ShowEx<YourMudDialog>("Your Dialog Title", parameters, options);
```

Your dialog can now look like this:

![SAMPLE](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/sampleDialogScreenshot.png)

### Using Animation to Show Dialog

You can use animation to display your dialog. This is done by setting the `Animation` property of `DialogOptionsEx`.

```
var options = new DialogOptionsEx { 
    MaximizeButton = true, 
    CloseButton = true, 
    Buttons = buttons, 
    Position = DialogPosition.CenterRight, 
    Animation = AnimationType.SlideIn, 
    AnimationDuration = TimeSpan.FromMilliseconds(500),
    FullHeight = true
};
var dialog = await _dialogService.ShowEx<YourMudDialog>("Your Dialog Title", parameters, options);
```

![SAMPLE](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/Screenshots/slideIn.gif)

When you animate a dialog with dialogServiceEx, it's recommended to add the class `mud-ex-dialog-initial` to your dialog to ensure no visibility before animation.

```
<MudDialog Class="mud-ex-dialog-initial">
```

> **_NOTE:_** All animations can be used on other components as well. Currently, the following animations are supported: `SlideIn,FadeIn,Scale,Slide,Fade,Zoom,Roll,JackInTheBox,Hinge,Rotate,Bounce,Back,Jello,Wobble,Tada,Swing,HeadShake,Shake,RubberBand,Pulse,Flip,FlipX,FlipY`.

### Using Extension Method with an `Action<YourDialog>` 

Instead of using `DialogParameters`, you can call the extension method with an `Action<YourDialog>`

```
await dialogService.ShowEx<SampleDialog>("Simple Dialog", dialog => { dialog.ContentMessage = "Hello"; },options);
```

## Conclusion

This README file provides an overview of the MudBlazor.Extensions library, which is designed to simplify and enhance the development process in Blazor using MudBlazor. The library contains a variety of components, extensions, and features that aim to reduce the time and effort required to build intricate UIs. For additional information or help, visit the official [MudBlazor website](https://mudblazor.com/) or [MudBlazor GitHub repository](https://github.com/MudBlazor/Templates).

We hope you find this library helpful and encourage you to provide any feedback or contribute to its development.

## License

MudBlazor.Extensions is released under the MIT License. See the bundled LICENSE file for details.



## Change Log 
Latest Changes: 
<!-- CHANGELOG:START -->

<!-- CHANGELOG:END -->

 [Full change log can be found here]https://github.com/fgilde/MudBlazor.Extensions/blob/main/MudBlazor.Extensions/Docs/CHANGELOG.md) 


#### Planned Features
Notice this is just a first preview version. 
There are some features planned like 
 - Dragging with snap behaviour  
 

 #
 
If you like this package, please star it on [GitHub](https://github.com/fgilde/MudBlazor.Extensions) and share it with your friends
If not, you can give a star anyway and let me know what I can improve to make it better for you. 
 
## Links
[![GitHub](https://img.shields.io/badge/GitHub-Source-blue)](https://github.com/fgilde/MudBlazor.Extensions) 
[![NuGet](https://img.shields.io/badge/NuGet-Package-blue)](https://www.nuget.org/packages/MudBlazor.Extensions)
#
