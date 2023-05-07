# BrowserFileExt

`BrowserFileExt` is a static utility class that provides a set of extension methods for working with `IBrowserFile` instances. These methods include downloading a file, getting the appropriate file icon based on its content type or extension, and getting the content type of a file.

## Methods

### DownloadAsync (this IBrowserFile, IJSRuntime)

Downloads an `IBrowserFile` asynchronously.

#### Parameters

- `browserFile`: The `IBrowserFile` to be downloaded.
- `jsRuntime`: The `IJSRuntime` instance to be used for JavaScript interop.

#### Example

```csharp
await browserFile.DownloadAsync(jsRuntime);
```

### IconForFile (string)

Returns the appropriate icon for a file based on its content type.

#### Parameters

- `contentType`: The content type of the file.

#### Example

```csharp
string fileIcon = BrowserFileExt.IconForFile("application/pdf");
```

### GetIcon (this IBrowserFile)

Returns the appropriate icon for an `IBrowserFile` instance.

#### Example

```csharp
string fileIcon = browserFile.GetIcon();
```

### GetContentType (this IBrowserFile)

Returns the content type of an `IBrowserFile` instance.

#### Example

```csharp
string contentType = browserFile.GetContentType();
```

### IconForFile (ContentType)

Returns the appropriate icon for a file based on its `ContentType`.

#### Parameters

- `contentType`: The `ContentType` of the file.

#### Example

```csharp
string fileIcon = BrowserFileExt.IconForFile(new ContentType("application/pdf"));
```

### IconForExtension (string)

Returns the appropriate icon for a file based on its file extension.

#### Parameters

- `extension`: The file extension.

#### Example

```csharp
string fileIcon = BrowserFileExt.IconForExtension(".pdf");
```
