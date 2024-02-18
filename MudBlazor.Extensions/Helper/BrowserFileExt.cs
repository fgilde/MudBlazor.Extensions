using System.Net.Mime;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using Nextended.Blazor.Extensions;
using Nextended.Core;
using Nextended.Core.Contracts;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Helper
{
    /// <summary>
    /// Extensions for <see cref="IBrowserFile"/>
    /// </summary>
    [HasDocumentation("BrowserFileExt.md")]
    public static class BrowserFileExt
    {
        private static readonly Dictionary<string, string> ColorMap = new()
        {
            // Microsoft Office Colors
            { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "#217346" }, // Excel green
            { "application/vnd.ms-excel", "#217346" },
            { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "#2B579A" }, // Word blue
            { "application/msword", "#2B579A" },
            { "application/vnd.openxmlformats-officedocument.presentationml.presentation", "#D24726" }, // PowerPoint red
            { "application/vnd.ms-powerpoint", "#D24726" },

            // Adobe Colors
            { "application/pdf", "#ae0c00" }, // Adobe PDF red

            // Archive Formats
            { "application/zip", "#FFD700" }, // ZIP yellow
            { "application/x-zip-compressed", "#FFD700" },
            { "application/x-rar-compressed", "#FFD700" },
            { "application/x-tar", "#FFD700" },
            { "application/tar+gzip", "#FFD700" },
            { "application/x-7z-compressed", "#FFD700" },

            // Image Formats
            { "image/jpeg", "#B19CD9" }, // Lavender
            { "image/png", "#B19CD9" },
            { "image/gif", "#B19CD9" },
            { "image/bmp", "#B19CD9" },
            { "image/webp", "#B19CD9" },

            // Video Formats
            { "video/mp4", "#FF8C00" }, // Dark orange
            { "video/mpeg", "#FF8C00" },
            { "video/avi", "#FF8C00" },
            { "video/quicktime", "#FF8C00" },

            // Audio Formats
            { "audio/mpeg", "#1E90FF" }, // Bright blue
            { "audio/wav", "#1E90FF" },
            { "audio/aac", "#1E90FF" },
            { "audio/ogg", "#1E90FF" },

            { "text/markdown", "#ADD8E6" },        // Light blue for Markdown
            { "text/plain", "#B0B0B0" },           // Neutral gray for TXT
            { "application/json", "#787878" },     // Darker gray for JSON
        
            // Web Development Formats
            { "text/html", "#E34F26" }, // HTML5 orange
            { "text/css", "#1572B6" }, // CSS3 blue
            { "application/javascript", "#F7DF1E" }, // JavaScript yellow
            { "application/xml", "#FF4500" }, // Orange-red

            // Database Formats
            { "application/x-sqlite3", "#025E8C" }, // SQLite blue
            { "application/sql", "#F29111" }, // Generic SQL color

            // Scripting Languages
            { "text/x-python", "#306998" }, // Python blue
            { "text/x-php", "#8892BF" }, // PHP purple
            { "text/x-ruby", "#CC342D" }, // Ruby red
            { "text/x-perl", "#39457E" }, // Perl blue

            // Others
            { "text/yaml", "#DDFF55" }, // Bright yellowish-green
            { "application/x-sh", "#4EAA25" }, // Green            

            // Other Document Types
            { "application/rtf", "#7D7D7D" }, // Neutral gray
            { "application/x-latex", "#008080" }, // Teal color

            // 3D Graphics
            { "application/vnd.ms-pki.stl", "#FF6347" }, // Tomato color            

            // Other Media Types
            { "image/svg+xml", "#FF69B4" } // HotPink for SVG

        };

        /// <summary>
        /// Downloads the file
        /// </summary>
        public static Task DownloadAsync(this IBrowserFile browserFile, IJSRuntime jsRuntime)
            => browserFile.DownloadFileAsync(jsRuntime);

        /// <summary>
        /// Returns Icon for contentType
        /// </summary>
        public static string IconForFile(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                return Icons.Custom.FileFormats.FileDocument;
            return IconForFile(new ContentType(contentType));
        }

        /// <summary>
        /// Returns Icon for contentType
        /// </summary>
        public static string IconForFile(IUploadableFile file) => GetIcon(file.FileName, file.ContentType);

        /// <summary>
        /// Returns Icon for IBrowserFile
        /// </summary>
        public static string GetIcon(this IBrowserFile file) => GetIcon(file.Name, file.GetContentType());

        /// <summary>
        /// Returns Icon for IBrowserFile
        /// </summary>
        public static string GetIcon(string fileName, string contentType)
        {
            if (!string.IsNullOrWhiteSpace(fileName) && Path.GetExtension(fileName)?.Equals(".nupkg", StringComparison.InvariantCultureIgnoreCase) == true)
                return MudExIcons.Custom.Brands.Nuget;
            return IconForFile(contentType);
        }

        /// <summary>
        /// Returns the content type for given file
        /// </summary>
        public static string GetContentType(this IBrowserFile file)
            => file == null ? "" : string.IsNullOrWhiteSpace(file.ContentType) ? MimeType.GetMimeType(file.Name) : file.ContentType;

        /// <summary>
        /// Returns the preferred color for the given content type
        /// </summary>
        public static MudExColor GetPreferredColor(string contentType)
        {
            return ColorMap.TryGetValue(contentType, out string color) ? color :
                MudBlazor.Color.Default;
        }

        /// <summary>
        /// Returns Icon for IBrowserFile
        /// </summary>
        public static MudExColor GetPreferredColor(this IBrowserFile file)
            => GetPreferredColor(file.GetContentType());

        /// <summary>
        /// Returns Icon for contentType
        /// </summary>
        public static string IconForFile(ContentType contentType)
        {
            var mime = contentType.ToString().ToLower();
            if (MimeType.IsArchive(mime))
                return Icons.Material.Filled.Archive;
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

        /// <summary>
        /// Returns Icon for an extension
        /// </summary>
        public static string IconForExtension(string extension) => IconForFile(MimeType.GetMimeType(extension?.EnsureStartsWith(".")));
    }
}