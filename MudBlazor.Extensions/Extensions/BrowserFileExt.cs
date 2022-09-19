using System.Net.Mime;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Nextended.Blazor.Extensions;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Extensions
{
    public static class BrowserFileExt
    {
        public static Task DownloadAsync(this IBrowserFile browserFile, IJSRuntime jsRuntime) 
            => browserFile.DownloadFileAsync(jsRuntime);
       
        //public static async Task DownloadAsync(this IBrowserFile browserFile, IJSRuntime jsRuntime)
        //{
        //    var url = await DataUrl.GetDataUrlAsync(await browserFile.GetBytesAsync(), browserFile.ContentType);
        //    await jsRuntime.InvokeVoidAsync("MudBlazorExtensions.downloadFile", new
        //    {
        //        Url = url,
        //        FileName = $"{browserFile.Name}",
        //        MimeType = browserFile.ContentType
        //    });
        //}

        public static string IconForFile(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                return Icons.Custom.FileFormats.FileDocument;
            return IconForFile(new ContentType(contentType));
        }

        public static string GetIcon(this IBrowserFile file)
        {
            return IconForFile(file.GetContentType());
        }

        public static string GetContentType(this IBrowserFile file)
        {
            return string.IsNullOrWhiteSpace(file.ContentType) ? MimeType.GetMimeType(file.Name) : file.ContentType;
        }

        public static string IconForFile(ContentType contentType)
        {
            var mime = contentType.ToString().ToLower();
            if (MimeType.IsZip(mime))
                return Icons.Filled.Archive;
            if (MimeType.Matches(mime, "application/x-dotnet*"))
                return Icons.Custom.Brands.MicrosoftVisualStudio;
            if (MimeType.Matches(mime, "application/vnd.ms-excel", "text/csv", "application/vnd.openxmlformats-officedocument.spreadsheetml*", "application/vnd.ms-excel*"))
                return Icons.Custom.FileFormats.FileExcel;
            if (MimeType.Matches(mime, "application/vnd.ms*", "application/msword", "application/vnd.openxmlformats-officedocument*"))
                return Icons.Custom.FileFormats.FileWord;
            if (MimeType.Matches(mime, "application/pdf"))
                return Icons.Custom.FileFormats.FilePdf;
            if (MimeType.Matches(mime, "application/java*", "application/json*", "text/html", "text/xml", "application/xml"))
                return Icons.Custom.FileFormats.FileCode;

            return contentType.MediaType.Split("/").First().ToLower() switch
            {
                "image" => Icons.Custom.FileFormats.FileImage,
                "audio" => Icons.Custom.FileFormats.FileMusic,
                "video" => Icons.Custom.FileFormats.FileVideo,
                "text" => Icons.Custom.FileFormats.FileDocument,
                _ => Icons.Custom.FileFormats.FileDocument
            };
        }


        public static string IconForExtension(string extension)
        {
            return IconForFile(MimeType.GetMimeType(extension = extension?.EnsureStartsWith(".")));
        }
    }
}