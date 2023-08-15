# MudExFileDisplay

<!-- FILEDISPLAY:START -->
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

<!-- FILEDISPLAY:END -->
