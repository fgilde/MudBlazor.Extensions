﻿[![GitHub Repo stars](https://img.shields.io/github/stars/fgilde/mudblazor.extensions?color=594ae2&style=flat-square&logo=github)](https://github.com/fgilde/mudblazor.extensions/stargazers)
[![GitHub](https://img.shields.io/github/license/garderoben/mudblazor?color=594ae2&style=flat-square&logo=github)](https://github.com/fgilde/MudBlazor.Extensions/blob/master/LICENSE)
[![GitHub last commit](https://img.shields.io/github/last-commit/fgilde/MudBlazor.Extensions?color=594ae2&style=flat-square&logo=github)](https://github.com/fgilde/MudBlazor.Extensions)
[![Nuget version](https://img.shields.io/nuget/v/MudBlazor.Extensions?color=ff4081&label=nuget%20version&logo=nuget&style=flat-square)](https://www.nuget.org/packages/MudBlazor.Extensions/)
[![Nuget downloads](https://img.shields.io/nuget/dt/MudBlazor.Extensions?color=ff4081&label=nuget%20downloads&logo=nuget&style=flat-square)](https://www.nuget.org/packages/MudBlazor.Extensions)
[![Website](https://img.shields.io/website?label=mudex.org&url=http%3A%2F%2Fmudex.org)](https://www.mudex.org/)

#### CI Stats
[![Publish Nuget Preview Package and deploy Test App](https://github.com/fgilde/MudBlazor.Extensions/actions/workflows/nuget_preview_publish_and_app_deploy.yml/badge.svg)](https://github.com/fgilde/MudBlazor.Extensions/actions/workflows/nuget_preview_publish_and_app_deploy.yml)
[![Publish Nuget Release Package](https://github.com/fgilde/MudBlazor.Extensions/actions/workflows/nuget_release_publish.yml/badge.svg)](https://github.com/fgilde/MudBlazor.Extensions/actions/workflows/nuget_release_publish.yml)

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
    app.UseMudExtensions();
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
<!-- Copied from https://raw.githubusercontent.com/wiki/fgilde/MudBlazor.Extensions/Showcase.md on 2023-09-15 10:38:31 -->
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
<!-- Copied from ObjectEdit.md on 2023-09-15 10:38:30 -->
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
<!-- Copied from MudExFileDisplay.md on 2023-09-20 13:17:34 -->
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
<!-- Copied from MudExUploadEdit.md on 2023-09-20 13:17:34 -->

This component provides multi-file upload functionality, with features like duplicate checks, max size, specific allowed content types, max items, zip auto-extract, and many more.

![SAMPLE](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/Screenshots/UploadEdit.gif)
[Download Video](https://github.com/fgilde/MudBlazor.Extensions/blob/main/MudBlazor.Extensions/Screenshots/UploadEdit.mkv?raw=true)


[More](https://github.com/fgilde/MudBlazor.Extensions/blob/main/MudBlazor.Extensions/Docs/MudExUploadEdit.md)
<!-- UPLOADEDIT:END -->

## Extensions
<!-- DIALOG_EXT:START -->
<!-- Copied from DialogExtensions.md on 2023-09-20 13:17:34 -->
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
<!-- Copied from CHANGELOG.md on 2023-09-20 13:17:33 -->
 - 1.7.68 > Fix memoryleak for streams in [MudExFileDisplay](https://www.mudex.org/file-display)
 - 1.7.68 > Add new Parameter "ColorizeIcons" to [MudExUploadEdit](https://www.mudex.org/upload-edit) if true the icons will be colored
 - 1.7.68 > **_Breaking:_** Add new Parameter "StreamUrlHandling" to [MudExUploadEdit](https://www.mudex.org/upload-edit) this can have the value `StreamUrlHandling.BlobUrl` or `StreamUrlHandling.DataUrl` this always was earlier handled as DataUrl what now changed as default to handle as BlobUrl to allow preview of bigger files as well
 - 1.7.68 > **_Breaking:_** Add new Parameter "StreamUrlHandling" to [MudExFileDisplay](https://www.mudex.org/file-display) this can have the value `StreamUrlHandling.BlobUrl` or `StreamUrlHandling.DataUrl` this always was earlier handled as DataUrl what now changed as default to handle as BlobUrl to allow preview of bigger files as well
 - 1.7.68 > Add new Parameter "ColorizeIcons" to [MudExFileDisplay](https://www.mudex.org/file-display) if true the icons will be colored
 - 1.7.68 > New small component [MudExIcon](https://www.mudex.org/mud-ex-icon) default as MudIcon with the benefit of having [MudExColor](https://www.mudex.org/d/MudExColor) as ColorType that allows Theme and Customcolors in one type
 - 1.7.68 > [MudExFileDisplayZip](https://www.mudex.org/file-display) now nativly supports Tar, Tar+Gzip, 7zip and Rar archives
 - 1.7.68 > Update MudBlazor to 6.10.0
 - 1.7.68 > Add Dense Parameter to [MudExFileDisplay](https://www.mudex.org/file-display)
 - 1.7.68 > Add multi tag search in [MudExFileDisplay](https://www.mudex.org/file-display)
<!-- CHANGELOG:END -->
Full change log can be found [here](https://github.com/fgilde/MudBlazor.Extensions/blob/main/MudBlazor.Extensions/Docs/CHANGELOG.md) 

 


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
