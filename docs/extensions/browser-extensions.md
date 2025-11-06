# Browser Extensions

Utility extension methods for browser file handling and management.

## BrowserFileExtensions

Extensions for working with `IBrowserFile`.

### Get File Icon

```csharp
var icon = BrowserFileExtensions.IconForFile(contentType);
```

### Get File Data URL

```csharp
var dataUrl = await file.GetDataUrlAsync();
```

## Features

- ✅ **File Icons** - Get appropriate icons for file types
- ✅ **Data URLs** - Convert files to data URLs
- ✅ **Type Detection** - Detect file types and MIME types
