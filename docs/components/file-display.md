# MudExFileDisplay

The `MudExFileDisplay` component is designed to display file contents with automatic format detection and preview capabilities. It can handle URLs or streams and delivers the best possible display for various file types.

![File Display PDF](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/Screenshots/FileDisplayPdf.png)

## Features

- ✅ **Automatic Format Detection** - Detects file type and chooses best renderer
- ✅ **Multiple File Types** - PDF, images, videos, audio, text, ZIP archives, and more
- ✅ **URL or Stream Support** - Works with both URLs and content streams
- ✅ **Extensible** - Register custom file display components
- ✅ **ZIP Archive Support** - Browse and extract ZIP files
- ✅ **Markdown Support** - Render markdown files

## Basic Usage

### Display from URL

```razor
<MudExFileDisplay FileName="document.pdf" 
                  ContentType="application/pdf" 
                  Url="@fileUrl" />

@code {
    private string fileUrl = "https://example.com/document.pdf";
}
```

### Display from Stream

```razor
<MudExFileDisplay FileName="image.jpg" 
                  ContentType="image/jpeg" 
                  ContentStream="@imageStream" />

@code {
    private Stream imageStream;
}
```

### Display Uploaded File

```razor
<MudExFileDisplay FileName="@file.Name" 
                  ContentType="@file.ContentType" 
                  ContentStream="@file.OpenReadStream()" />

@code {
    private IBrowserFile file;
}
```

## Supported File Types

- **Documents**: PDF, DOC, DOCX, RTF, EML, MHT/MHTML, XPS/OXPS
- **Adobe artwork**: AI/AIT (PDF-compatible files), EPS/EPSF/EPSI/EPI/PS (embedded TIFF or EPSI previews)
- **Images**: JPG, PNG, GIF, SVG/SVGZ, BMP, WEBP, TIFF, TGA, QOI, PBM, PGM, PPM, AVIF, HEIC/HEIF, PSD/PSB
- **OpenDocument**: ODT/ODS/ODP/ODG and their template variants
- **Comic books**: CBZ and CBR
- **Columnar data**: Apache Parquet, Arrow IPC and Feather
- **Medical images**: uncompressed little-endian DICOM
- **Videos**: MP4, WEBM, OGG
- **Audio**: MP3, WAV, OGG
- **Text**: TXT, CSV, JSON, XML
- **Code**: JS, CS, HTML, CSS (with syntax highlighting)
- **Archives**: ZIP (with file browser)
- **Markdown**: MD (rendered as HTML)

`MudExImageViewer` uses the browser's native AVIF support and loads pinned browser decoders for PSD/PSB and
HEIC/HEIF only when one of those formats is opened. Photoshop files expose a layer panel whose entries can be
shown and hidden. Common groups, opacity values and blend modes are recomposited in the browser; complex clipping,
adjustment-layer and effect combinations can only be approximated without Photoshop's rendering engine.

The columnar-data viewer loads Arrow plus LZ4 and Zstandard codecs on demand so that compressed Arrow IPC and
Feather record batches can be read. DICOM intentionally supports the common uncompressed little-endian transfer
syntaxes; compressed clinical images show an explicit compatibility message instead of being interpreted
incorrectly. XPS and OpenDocument rendering focuses on safe preview content and does not execute embedded scripts
or macros.

### Adobe Illustrator and PostScript

`MudExFileDisplayAdobe` uses the browser's native PDF preview for AI and AIT files saved with Illustrator's
**Create PDF Compatible File** option. Keeping this path isolated from the optional PDF component also avoids
collisions with AMD loaders used by code editors. For EPS, EPSF, EPSI, EPI, PS, and older Illustrator files it extracts
an embedded TIFF or device-independent EPSI preview and opens that preview in `MudExImageViewer`.

The component does not execute PostScript in the browser. Files without a PDF-compatible section or an
embedded preview therefore show an explanatory fallback instead of attempting an unsafe or incomplete render.

## MudExFileDisplayZip

Display ZIP archive contents with a file browser:

![File Display ZIP](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/Screenshots/FileDisplayZip.png)

```razor
<MudExFileDisplayZip RootFolderName="@FileName" 
                     ContentStream="@zipStream" 
                     AllowDownload="true" />

@code {
    private Stream zipStream;
}
```

## MudExFileDisplayDialog

Display files in a dialog:

![File Display Dialog](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/Screenshots/FileDisplayDialog.gif)

### Using Static Helper

```csharp
await MudExFileDisplayDialog.Show(
    _dialogService, 
    dataUrl, 
    fileName, 
    contentType, 
    ex => ex.JsRuntime = _jsRuntime
);
```

### With IBrowserFile

```csharp
IBrowserFile file = selectedFile;
await MudExFileDisplayDialog.Show(
    _dialogService, 
    file, 
    ex => ex.JsRuntime = _jsRuntime
);
```

### Manual Usage

```csharp
var parameters = new DialogParameters
{
    {nameof(Icon), BrowserFileExtensions.IconForFile(contentType)},
    {nameof(Url), url},
    {nameof(ContentType), contentType}
};
await dialogService.ShowEx<MudExFileDisplayDialog>(title, parameters, optionsEx);
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `FileName` | `string` | - | Name of the file to display |
| `ContentType` | `string` | - | MIME type of the file |
| `Url` | `string` | - | URL to the file |
| `ContentStream` | `Stream` | - | Stream containing file data |
| `AllowDownload` | `bool` | `true` | Allow file download |
| `ForceNativeRender` | `bool` | `false` | Force native rendering |

## Custom File Display

Implement `IMudExFileDisplay` to create custom file display components:

```csharp
public class MyCustomFileDisplay : ComponentBase, IMudExFileDisplay
{
    public bool CanHandleFile(string fileName, string contentType)
    {
        return contentType == "application/x-custom";
    }

    public RenderFragment Render()
    {
        return builder =>
        {
            // Your custom rendering logic
        };
    }
}
```

Register your custom display:

```csharp
// In your service configuration
builder.Services.AddScoped<IMudExFileDisplay, MyCustomFileDisplay>();
```

## Complete Example

```razor
@page "/file-viewer"
@inject IDialogService DialogService

<MudFileUpload T="IBrowserFile" OnFilesChanged="@OnFileSelected">
    <ButtonTemplate>
        <MudButton HtmlTag="label"
                   Variant="Variant.Filled"
                   Color="Color.Primary"
                   StartIcon="@Icons.Material.Filled.CloudUpload"
                   for="@context">
            Select File to Preview
        </MudButton>
    </ButtonTemplate>
</MudFileUpload>

@if (selectedFile != null)
{
    <MudPaper Class="mt-4 pa-4">
        <MudExFileDisplay FileName="@selectedFile.Name"
                          ContentType="@selectedFile.ContentType"
                          ContentStream="@selectedFile.OpenReadStream(maxAllowedSize: 10485760)" />
    </MudPaper>

    <MudButton Class="mt-2" 
               Variant="Variant.Filled" 
               Color="Color.Primary"
               OnClick="@ShowInDialog">
        View in Dialog
    </MudButton>
}

@code {
    private IBrowserFile selectedFile;

    private void OnFileSelected(InputFileChangeEventArgs e)
    {
        selectedFile = e.File;
    }

    private async Task ShowInDialog()
    {
        await MudExFileDisplayDialog.Show(DialogService, selectedFile);
    }
}
```

## Live Demo

Experience the component: [File Display Demo](https://www.mudex.org/file-display)

## See Also

- [Upload Edit](upload-edit.md) - File upload component
- [Object Edit](object-edit.md) - Form generation
- [Dialog Extensions](../extensions/dialog-extensions.md) - Enhanced dialogs
