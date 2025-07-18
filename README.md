﻿[![GitHub Repo stars](https://img.shields.io/github/stars/fgilde/mudblazor.extensions?color=594ae2&style=flat-square&logo=github)](https://github.com/fgilde/mudblazor.extensions/stargazers)
[![GitHub](https://img.shields.io/github/license/garderoben/mudblazor?color=594ae2&style=flat-square&logo=github)](https://github.com/fgilde/MudBlazor.Extensions/blob/master/LICENSE)
[![GitHub last commit](https://img.shields.io/github/last-commit/fgilde/MudBlazor.Extensions?color=594ae2&style=flat-square&logo=github)](https://github.com/fgilde/MudBlazor.Extensions)
[![Nuget version](https://img.shields.io/nuget/v/MudBlazor.Extensions?color=ff4081&label=nuget%20version&logo=nuget&style=flat-square)](https://www.nuget.org/packages/MudBlazor.Extensions/)
[![Nuget downloads](https://img.shields.io/nuget/dt/MudBlazor.Extensions?color=ff4081&label=nuget%20downloads&logo=nuget&style=flat-square)](https://www.nuget.org/packages/MudBlazor.Extensions)
[![Website](https://img.shields.io/website?label=mudex.org&url=http%3A%2F%2Fwww.mudex.org)](https://www.mudex.org/)
[![Publish Nuget Preview Package and deploy Test App](https://github.com/fgilde/MudBlazor.Extensions/actions/workflows/nuget_preview_publish_and_app_deploy.yml/badge.svg)](https://github.com/fgilde/MudBlazor.Extensions/actions/workflows/nuget_preview_publish_and_app_deploy.yml)
[![Publish Nuget Release Package](https://github.com/fgilde/MudBlazor.Extensions/actions/workflows/nuget_release_publish.yml/badge.svg)](https://github.com/fgilde/MudBlazor.Extensions/actions/workflows/nuget_release_publish.yml)
[![Deploy TryMudEx](https://github.com/fgilde/MudBlazor.Extensions/actions/workflows/TryMudEx.yml/badge.svg)](https://github.com/fgilde/MudBlazor.Extensions/actions/workflows/TryMudEx.yml)

# MudBlazor.Extensions
The MudBlazor.Extensions is a convenient package that extends the capabilities of the MudBlazor component library. This guide will demonstrate the setup process for your project, along with detailed explanations of the components, extensions, and functionalities provided.
It's important to note that this package requires a MudBlazor project and the referenced MudBlazor package. For further information and assistance, please visit the official MudBlazor documentation at [MudBlazor](https://mudblazor.com/) and [MudBlazor/Templates](https://github.com/MudBlazor/Templates).

## Demos
[![Azure](https://img.shields.io/badge/Azure-Demo-blue)](https://mudex.azurewebsites.net/) 
[![Cloudflare](https://img.shields.io/badge/Cloudflare-Demo-blue)](https://mudblazor-extensions.pages.dev)
#

## Try Online
[![TryMudEx](https://img.shields.io/badge/TryMudEx-purple)](https://try.mudex.org/) 
#

<!---
[Running Sample Application (Github Pages)](https://fgilde.github.io/MudBlazor.Extensions/sample/wwwroot/)
-->
<!---
![Alt](https://repobeats.axiom.co/api/embed/d1dbd5b5469b639723e0fc094a8408628d2487af.svg "Repobeats analytics image")
-->

## Table of Contents
- [Installation](#installation)
- [Setting Up MudBlazor.Extensions](#setting-up-mudblazor-extensions)
- [Components](#components)
  - [MudExObjectEdit](#mudexobjectedit)
  - [MudExStructuredDataEditor](#mudexstructureddataeditor)
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

// or this to add only the MudBlazor.Extensions but please ensure that this is added after mud servicdes are added. That means after `AddMudServices`
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


if your are running on Blazor Server side, you should also use the `MudBlazorExtensionMiddleware` you can do this in your startup or program.cs by adding the following line on your WebApplication:

```    
    app.Use(MudExWebApp.MudExMiddleware);
```

(Optional) if you have problems with automatic loaded styles you can also load the styles manually by adding the following line to your `index.html` or `_Host.cshtml`

```
<link id="mudex-styles" href="_content/MudBlazor.Extensions/mudBlazorExtensions.min.css" rel="stylesheet">
```

If you have loaded styles manually you should disable the automatic loading of the styles in the `AddMudExtensions` or `AddMudServicesWithExtensions` method. You can do this by adding the following line to your `Startup.cs` in the `ConfigureServices` method.
```c#
builder.Services.AddMudServicesWithExtensions(c => c.WithoutAutomaticCssLoading());
```

## Showcase Videos

<details>
  <summary>Expand videos</summary>

<!-- WIKI:START -->
<!-- Copied from https://raw.githubusercontent.com/wiki/fgilde/MudBlazor.Extensions/Showcase.md on 2024-09-23 15:35:58 -->
## Showcase

https://github.com/fgilde/MudBlazor.Extensions/assets/11070717/39e06d88-a947-43cd-9151-a7cf96bcd849


https://github.com/fgilde/MudBlazor.Extensions/assets/11070717/3c77b8bf-6198-4385-b452-f25cc2852e0a


https://github.com/fgilde/MudBlazor.Extensions/assets/11070717/ce9bdf86-aaf9-4f04-b861-bd57698bb7f5


https://github.com/fgilde/MudBlazor.Extensions/assets/11070717/6b054bdc-a413-437c-8dbb-ded4e242d2a7


https://github.com/fgilde/MudBlazor.Extensions/assets/11070717/57f39cec-c3e9-43aa-8bfe-260d9aa05f63


https://github.com/fgilde/MudBlazor.Extensions/assets/11070717/c6a0e47d-2ed6-4a4e-b2b8-f4963274c9f8


https://github.com/fgilde/MudBlazor.Extensions/assets/11070717/3fc658d7-7fa2-487e-98d2-91860e00374a


https://github.com/fgilde/MudBlazor.Extensions/assets/11070717/aa266253-f1ac-450d-bd7b-510d2b99e3c0


https://github.com/fgilde/MudBlazor.Extensions/assets/11070717/cf4ff772-953e-4462-90fc-b32249083fb8


https://github.com/fgilde/MudBlazor.Extensions/assets/11070717/79bccec3-9e04-4901-a7d2-a08c9cef031c


https://github.com/fgilde/MudBlazor.Extensions/assets/11070717/8963eaaa-0f96-4c76-8e3c-c945920b2c23


https://github.com/fgilde/MudBlazor.Extensions/assets/11070717/cd5bab33-75cd-442d-a156-f43cc3a1c78c


https://github.com/fgilde/MudBlazor.Extensions/assets/11070717/8545a70c-1ce2-4683-8f1e-40a69efe462b


https://github.com/fgilde/MudBlazor.Extensions/assets/11070717/5c736020-94ba-431a-94f7-8e437530978e


<!-- WIKI:END -->

</details>

## Components

This section introduces you to the various components provided by the MudBlazor.Extensions.

### MudExObjectEdit
<!-- OBJECTEDIT:START -->
<!-- Copied from ObjectEdit.md on 2024-09-23 15:35:55 -->
The `MudExObjectEdit` is a robust component that allows for object editing and automatically generates the corresponding UI. This component supports automatic validation for DataAnnotation Validations or fluent registered validations for your model.

To use `MudExObjectEdit`, you can simply use the `MudExObjectEditForm` and pass your model to it as shown below:

```
<MudExObjectEditForm OnValidSubmit="@OnSubmit" Value="@MyModel"></MudExObjectEditForm>
```

You can also utilize the `MudExObjectEditDialog` to edit your model in a dialog. The easiest way to do this is by using the extension method `EditObject` on the `IDialogService`.

```
dialogService.EditObject(User, "Dialog Title", dialogOptionsEx);
```

[More](https://github.com/fgilde/MudBlazor.Extensions/blob/main/MudBlazor.Extensions/Docs/ObjectEdit.md)
<!-- OBJECTEDIT:END -->

### MudExStructuredDataEditor

The `MudExStructuredDataEditor` is a component that allows object editing and automatically generates the corresponding UI based on structured data like json, xml or yaml. 
This component supports all the same as MudExObjectEditForm.

To use `MudExStructuredDataEditor`, you can simply bind your data string shown as bellow:

```
    <MudExStructuredDataEditor @bind-Data="_dataString"></MudExStructuredDataEditor>
```

You can also utilize the `MudExStructuredDataEditor` to edit your data in a dialog. The easiest way to do this is by using the extension method `EditStructuredDataString` on the `IDialogService`.

```
dialogService.EditStructuredDataString(_dataType, _dataString, $"Auto Generated Editor for {_dataType}", ((_,_) => Task.FromResult("")));
```

You can find a running [Sample here](https://www.mudex.org/structured-data-edit)


### MudExFileDisplay
<!-- FILEDISPLAY:START -->
<!-- Copied from MudExFileDisplay.md on 2024-09-23 15:35:55 -->
The `MudExFileDisplay` component is designed to display file contents, such as a preview before uploading or for referenced files. 
This component can automatically handle URLs or streams and deliver the best possible display. 
Additionally, you can implement `IMudExFileDisplay` in your own component to register a custom file display.
This is excacly what `MudExFileDisplayZip` does, which is used by `MudExFileDisplay` to display zip files or what `MudExFileDisplayMarkdown` does to display markdown files.


Example of using `MudExFileDisplay`:

```
 <MudExFileDisplay FileName="NameOfYourFile.pdf" ContentType="application/pdf" Url="@Url"></MudExFileDisplay>
```

![SAMPLE](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/Screenshots/FileDisplayPdf.png)

### MudExFileDisplayZip 
This component can be automatically utilized by `MudExFileDisplay`, but can also be used manually if necessary.
Note: If you're using the ContentStream it should not be closed or disposed.

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


[More](https://github.com/fgilde/MudBlazor.Extensions/blob/main/MudBlazor.Extensions/Docs/MudExFileDisplay.md)
<!-- FILEDISPLAY:END -->

### MudExUploadEdit
<!-- UPLOADEDIT:START -->
<!-- Copied from MudExUploadEdit.md on 2024-09-23 15:35:55 -->

`MudExUploadEdit` is a versatile file upload component with a wide range of features such as MIME and extension whitelisting/blacklisting, folder upload, drag and drop, copy and paste, renaming, and integration with Dropbox, Google Drive, and OneDrive.

![SAMPLE](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/Screenshots/UploadEdit.gif)


[More](https://github.com/fgilde/MudBlazor.Extensions/blob/main/MudBlazor.Extensions/Docs/MudExUploadEdit.md)
<!-- UPLOADEDIT:END -->

## Extensions
<!-- DIALOG_EXT:START -->
<!-- Copied from DialogExtensions.md on 2024-09-23 15:35:55 -->
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
    new MudDialogButton( DotNetObjectReference.Create(this as object), nameof(AlarmClick)) {Icon = Icons.Material.Filled.Alarm},
    new MudDialogButton( DotNetObjectReference.Create(this as object), nameof(ColorLensClick)) {Icon = Icons.Material.Filled.ColorLens},
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


[More](https://github.com/fgilde/MudBlazor.Extensions/blob/main/MudBlazor.Extensions/Docs/DialogExtensions.md)
<!-- DIALOG_EXT:END -->

## Conclusion

This README file provides an overview of the MudBlazor.Extensions library, which is designed to simplify and enhance the development process in Blazor using MudBlazor. The library contains a variety of components, extensions, and features that aim to reduce the time and effort required to build intricate UIs. For additional information or help, visit the official [MudBlazor website](https://mudblazor.com/) or [MudBlazor GitHub repository](https://github.com/MudBlazor/Templates).

We hope you find this library helpful and encourage you to provide any feedback or contribute to its development.

## License

MudBlazor.Extensions is released under the MIT License. See the bundled LICENSE file for details.


## Change Log 
Latest Changes: 
<!-- CHANGELOG:START -->
<!-- Copied from CHANGELOG.md on 2024-09-23 15:35:55 -->
 - 8.9.0 > Support touch events for dialog dragging
 - 8.9.0 > Fixes bug with icon picker if no value is specified
 - 8.9.0 > Fixes bug with custom size in non modal dialogs
 - 8.9.0 > Update MudBlazor to 8.9.0 and other Packages to latest version 
 - 8.8.0 > Update MudBlazor to 8.8.0 and other Packages to latest version 
 - 8.8.0 > Fix Bug in MudExOneDriveFilePicker where the file couldnt be loaded when AutoLoadDataBytes is true
 - 8.7.0 > Update MudBlazor to 8.7.0
 - 8.7.0 > Fix bug in SnapDrag Mode for dialog
 - 8.6.1 > New finally implemented SnapDrag Mode for dialog. Sample available [here](https://www.mudex.org/dialog-snap)
 - 8.6.0 > Improve performance for lightbulb on cards
 - 8.6.0 > Update MudBlazor to 8.6.0
 - 8.5.2 > Fix bug where no focus in searchbox is possible in MudExSelect
 - 8.5.2 > DialogService now has more helping methods like PickAsync, SelectAsync or EditAsync for items editing
 - 8.5.0 > Update to MudBlazor 8.5.0
 - 8.5.0 > Bug fix 
 - 8.5.0 > Inline dialog improvements for statechange and server rendered
 - 8.3.0 > Update to MudBlazor 8.3.0
 - 8.3.0 > Option for Screenshots in capture service
 - 8.3.0 > Bugfix in MudExSelect
 - 8.2.1 > Fix label outlined bug in MudExSelect #120
 - 8.2.0 > Update to MudBlazor 8.2.0
 - 8.2.0 > Fix some smaller bugs
 - 8.0.2 > CaptureService now supporting full VideoConstraints and AudioConstraints.  
 - 8.0.1 > Fix small bug in MudExSelect
 - 8.0.0 > Support for MudBlazor 8.0.0
 - 8.0.0 > MudExObject now supports Protected elements where edit needs to be confirmed `meta.Property(m => m.LastName).WithEditConfirmation()`
 - 2.1.0 > MudExObject now supports default focused element within the meta configuration with `meta.Property(m => m.LastName).WithDefaultFocus()`
 - 2.1.0 > MudExObject edit now has AutoFocus for first input field if no other focus is configured
 - 2.1.0 > Provide a Middleware again without deprecated UseMudExtensions now you should use `app.Use(MudExWebApp.MudExMiddleware);`
 - 2.1.0 > Fix another bug with dialog that only occurs on webassembly projects hosted in a .net8 runtime
 - 2.0.9 > Fix bug with dialog animations on server side rendered projects #112
 - 2.0.8 > Ensure dialog initial relative state if configured
 - 2.0.8 > Fix Remove Item Bug in Collection editor 
 - 2.0.7 > Update MudBlazor to 7.15.0
 - 2.0.7 > For the MudExObjectEdit you can now easially register a component as editor for a specific type [see here how you can register your component as editor for a type](https://www.mudex.org/d/ObjectEditRegisterComponent)
 - 2.0.7 > **_Breaking:_** The DailogOptionsEx class has a new Property `KeepRelations`. this is true by default and ensures positions and sizes are in relative percentage values. With this a dialog stays in the same position and size relative to the screen size. If you want to have a dialog with fixed sizes and positions you can set this to false and return to the old behaviour. 
 - 2.0.7 > The DailogOptionsEx class has a new Property `KeepMaxSizeConstraints`. if this is is true then the max width and max height while resizing is limited to initial MaxWidth or MaxHeight property values. 
 - 2.0.7 > New Component [MudExObjectEditPicker](https://www.mudex.org//objectedit-picker) is the known MudExObjectEdit as a picker.
 - 2.0.7 > All MudEx picker components like [MudExObjectEditPicker](https://www.mudex.org//objectedit-picker) [MudExColorEdit](https://www.mudex.org/mud-ex-color-edit), [MudExIconPicker](https://www.mudex.org/mud-ex-icon-picker) or [MudExPicker](https://www.mudex.org/mud-ex-picker) now inherits from new [MudExPickerBase](https://www.mudex.org/https://www.mudex.org/api/MudExPickerBase). All theese pickers now supports animations, and all DialogOptionsEx for PickerVariant as Dialog
 - 2.0.7 > New Component [MudExPicker](https://www.mudex.org/mud-ex-picker) is a picker component that easially supports own picker content.
 - 2.0.7 > New Component [MudExGroupBox](https://www.mudex.org/mud-ex-group-box) is a simple group box component to group content with a title and a border.
 - 2.0.7 > [MudExUploadEdit](https://www.mudex.org/upload-edit) now allows recording of audio, video and captured screen directly using the new [CaptureService](https://www.mudex.org/capture-service)
 - 2.0.7 > Add [CaptureService](https://www.mudex.org/capture-service) to allow easy recording of screen capture, camera video and audio
 - 2.0.7 > New Component [MudExCaptureButton](https://www.mudex.org/mud-ex-capture-button) to allow easy recording of screen capture, camera video and audio
 - 2.0.6 > [MudExAudioPlayer](https://www.mudex.org/file-display?file=4.Unified-Voice.mp3) now displays meta infos
 - 2.0.6 > The [MudExImageViewer](https://www.mudex.org/image-view) now allows area to select with a rubberband and open, download, print or directly switching the view to the selected area as an image.
 - 2.0.6 > Allow Xls and CSV files and fix header bug in [MudExFileDisplayExcel](https://www.mudex.org/file-display?file=ExcelSheet.xlsx)
 - 2.0.6 > Allow async child loading in [MudExTreeView](https://www.mudex.org/demos/TreeView).
 - 2.0.6 > Fixed error in sample app for [MudExSelect](https://www.mudex.org/mud-ex-select) and [MudExThemeEdit](https://www.mudex.org/theme-edit)
 - 2.0.6 > update used nuget packages to latest versions
<!-- CHANGELOG:END -->
Full change log can be found [here](https://github.com/fgilde/MudBlazor.Extensions/blob/main/MudBlazor.Extensions/Docs/CHANGELOG.md) 

 

 #
 
If you like this package, please star it on [GitHub](https://github.com/fgilde/MudBlazor.Extensions) and share it with your friends
If not, you can give a star anyway and let me know what I can improve to make it better for you. 
 
## Links
[![GitHub](https://img.shields.io/badge/GitHub-Source-blue)](https://github.com/fgilde/MudBlazor.Extensions) 
[![NuGet](https://img.shields.io/badge/NuGet-Package-blue)](https://www.nuget.org/packages/MudBlazor.Extensions)
#
