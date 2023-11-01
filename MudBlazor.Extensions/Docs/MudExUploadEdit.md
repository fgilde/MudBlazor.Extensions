# MudExUploadEdit

<!-- UPLOADEDIT:START -->

`MudExUploadEdit` is a versatile file upload component with a wide range of features such as MIME and extension whitelisting/blacklisting, folder upload, drag and drop, copy and paste, renaming, and integration with Dropbox, Google Drive, and OneDrive.

![SAMPLE](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/main/MudBlazor.Extensions/Screenshots/UploadEdit.gif)

![Download Video](https://github.com/fgilde/MudBlazor.Extensions/blob/main/MudBlazor.Extensions/Screenshots/UploadEdit.mkv?raw=true)


<!-- UPLOADEDIT:END -->

## Parameters

### Dialog Options

- `ExternalDialogOptions`: Options for the external file dialog. Default settings include a close button, simple drag mode, resizable, small maximum width, full width, and flip X animation.

### Appearance and Styling

- `ColoredImagesForExternalFilePicker`: Set to `true` to use original colors for images in the external file picker.
- `ExternalProviderRendering`: Defines how external file providers are rendered. Default is `ImagesNewLine`.
- `ButtonVariant`: Variant of action buttons. Default is `Text`.
- `ButtonColor`: Color of action buttons. Default is `Primary`.
- `ButtonSize`: Size of action buttons. Default is `Small`.
- `ButtonsJustify`: Alignment of action buttons. Default is `Center`.
- `ErrorAnimation`: The animation type for errors. Default is `Pulse`.
- `Variant`: The variant of the component.

### External Storage Providers

- `OneDriveClientId`: Client ID for One Drive.
- `GoogleDriveClientId`: Client ID for Google Drive.
- `DropBoxApiKey`: API key for DropBox.

Please notice, you can also set the client ID and API key in the `MudBlazor.Extensions` configuration to easialier using.

```c#
	builder.Services.AddMudServicesWithExtensions(c =>
    c.EnableDropBoxIntegration("<DROP_BOX_API_KEY>")
        .EnableGoogleDriveIntegration("<GOOGLE_DRIVE_CLIENT_ID>")
        .EnableOneDriveIntegration("<ONE_DRIVE_CLIENT_ID>")
);
```

### Text and Labels

- `TextAddExternal`: Text displayed in the add external dialog. Default is "Add External".
- `TextDropZone`: Text displayed in the drop zone.
- `TextUploadFiles`: Text for the upload files button. Default is "Upload Files".
- `TextUploadFile`: Text for the upload file button. Default is "Upload File".
- `TextUploadFolder`: Text for the upload folder button. Default is "Upload Folder".
- `TextAddUrl`: Text for the add URL button. Default is "Add Url".
- `TextAddFromGoogle`: Text for adding from Google Drive. Default is "Google Drive".
- `TextRemoveAll`: Text for the remove all button. Default is "Remove All".
- `TextErrorDuplicateFile`: Error text for duplicate files.
- `TextErrorMaxFileSize`: Error text for exceeding maximum file size.
- `TextErrorMaxFileCount`: Error text for exceeding maximum file count.
- `TextErrorExtensionNotAllowed`: Error text for disallowed file extensions.
- `TextErrorExtensionForbidden`: Error text for forbidden file extensions.
- `TextErrorMimeTypeNotAllowed`: Error text for disallowed MIME types.
- `TextErrorMimeTypeForbidden`: Error text for forbidden MIME types.
- `TextAddUrlTitle`: Title text in the add URL dialog. Default is "Add external Url".
- `TextAddUrlMessage`: Message text in the add URL dialog. Default is "Enter the URL to existing file".
- `Label`: The label displayed in the component.
- `HelperText`: The helper text displayed in the component.

### File Restrictions

- `MimeTypes`: Mime types for restrictions based on `MimeRestrictionType`.
- `Extensions`: Extensions for restrictions based on `ExtensionRestrictionType`.
- `MimeRestrictionType`: The type of the MIME restriction. Default is `WhiteList`.
- `ExtensionRestrictionType`: The type of the restriction for extensions. Default is `WhiteList`.


### Button Customization

- `ButtonVariant`: Variant of action buttons. Default is `Variant.Text`.
- `ButtonColor`: Color of action buttons. Default is `Color.Primary`.
- `ButtonSize`: Size of action buttons. Default is `Size.Small`.
- `ButtonsJustify`: Alignment of action buttons. Default is `Justify.Center`.

### Behavior and Appearance

- `CodeBlockTheme`: Specify Theme to use for code file previews. Default is `CodeBlockTheme.AtomOneDark`.
- `StreamUrlHandling`: Specify how temporary URLs are created. Default is `StreamUrlHandling.BlobUrl`.
- `ForceNativeRender`: Set to `true` to initially render native and ignore registered `IMudExFileDisplay`.
- `Label`: The label displayed in the component.
- `ReadOnly`: Defines whether the component is read-only.
- `HelperText`: The helper text displayed in the component.
- `Variant`: The variant of the component.
- `ErrorAnimation`: The animation type for errors. Default is `AnimationType.Pulse`.

### File Management

- `AllowRename`: Defines whether renaming of files is allowed. Default is `true`.
- `AllowExternalUrl`: Defines whether adding of external URL is allowed. Default is `true`.
- `AllowGoogleDrive`: Defines whether adding of external files from Google Drive is allowed. Default is `true`.
- `AllowDropBox`: Defines whether adding of external files from Drop Box is allowed. Default is `true`.
- `AllowOneDrive`: Defines whether adding of external files from Microsoft One Drive or office 365 is allowed. Default is `true`.
- `UploadFieldId`: The ID of the upload field.
- `MimeTypes`: Mime types for MimeRestrictions based on the `MimeRestrictionType` this types are allowed or forbidden.
- `Extensions`: Extensions for FileRestrictions based on the `ExtensionRestrictionType` this types are allowed or forbidden.
- `MimeRestrictionType`: The type of the MIME restriction. Default is `RestrictionType.WhiteList`.
- `ExtensionRestrictionType`: The type of the restriction for extensions. Default is `RestrictionType.WhiteList`.
- `MaxFileSize`: The maximum file size allowed in bytes. Default is `null`.
- `MaxHeight`: The maximum height allowed.
- `MinHeight`: The minimum height allowed.
- `MaxMultipleFiles`: The maximum number of multiple files allowed. Default is `100`.
- `UploadRequests`: The upload requests.
- `AllowMultiple`: Defines whether multiple files can be uploaded. Default is `true`.
- `AllowFolderUpload`: Defines whether folder upload is allowed. Default is `true`.
- `AllowPreview`: Defines whether preview of the files is allowed. Default is `true`.
- `ShowFileUploadButton`: Defines whether the file upload button is displayed. Default is `true`.
- `ShowFolderUploadButton`: Defines whether the folder upload button is displayed. Default is `true`.
- `ShowClearButton`: Defines whether the clear button is displayed. Default is `true`.
- `AllowRemovingItems`: Defines whether removing of items is allowed. Default is `true`.
- `SelectItemsMode`: The mode of selecting items. Default is `SelectItemsMode.None`.
- `AutoExtractArchive`: Defines whether zip or other archives files should be automatically extracted. Default is `false`.
- `UploadRequest`: The current upload request.
- `AllowDuplicates`: Defines whether duplicates are allowed.
- `DisplayErrors`: Defines whether errors should be displayed. Default is `true`.
- `SelectedRequests`: The selected requests.
- `RemoveErrorAfter`: The time to remove an error after it has been displayed. Default is `TimeSpan.FromSeconds(5)`.
- `AutoRemoveError`: Defines whether errors should be automatically removed.
- `RemoveErrorOnChange`: Defines whether errors should be removed when there are changes. Default is `true`.
- `AllowDrop`: Defines whether file drop is allowed. Default is `true`.