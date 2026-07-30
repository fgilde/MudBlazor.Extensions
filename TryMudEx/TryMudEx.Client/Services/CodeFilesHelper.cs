namespace TryMudEx.Client.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Text;
    using Try.Core;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.CodeAnalysis.CSharp;

    public static class CodeFilesHelper
    {
        private const int MainComponentFileContentMinLength = 10;

        public static readonly HashSet<string> ValidCodeFileExtensions = new(StringComparer.Ordinal)
        {
            CodeFile.RazorFileExtension,
            CodeFile.CsharpFileExtension,
            CodeFile.RefFileExtension
        };

        /// <summary>
        /// Normalizes a code file path. Supports relative folder paths ("Components/Header.razor"):
        /// every folder segment must be a valid C# identifier, the file part follows the classic rules.
        /// </summary>
        public static string NormalizeCodeFilePath(string path, out string error)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                error = "Path cannot be empty.";
                return null;
            }

            var trimmedPath = path.Trim().Replace('\\', '/');

            var segments = trimmedPath.Split('/');
            var folderSegments = new List<string>();
            for (var i = 0; i < segments.Length - 1; i++)
            {
                var segment = segments[i].Trim();
                if (segment.Length == 0)
                {
                    error = "Path cannot contain empty segments or start with '/'.";
                    return null;
                }

                if (!SyntaxFacts.IsValidIdentifier(segment))
                {
                    error = $"'{segment}' is not a valid folder name. It must be a valid C# identifier.";
                    return null;
                }

                folderSegments.Add(segment);
            }

            var filePart = segments[^1].Trim();

            var extension = Path.GetExtension(filePart).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
            {
                // Razor files are the default
                extension = CodeFile.RazorFileExtension;
            }

            if (!ValidCodeFileExtensions.Contains(extension))
            {
                error = $"Extension cannot be '{extension}'. Valid extensions: {string.Join("; ", ValidCodeFileExtensions)}";
                return null;
            }

            var fileName = Path.GetFileNameWithoutExtension(filePart);
            if (!SyntaxFacts.IsValidIdentifier(fileName))
            {
                error = $"'{fileName}' is not a valid file name. It must be a valid C# identifier.";
                return null;
            }

            if (extension == CodeFile.RazorFileExtension && char.IsLower(fileName[0]))
            {
                error = $"'{fileName}' starts with a lowercase character. Razor file names must start with an uppercase character or _.";
                return null;
            }

            error = null;
            var normalizedFile = fileName + extension;
            return folderSegments.Count == 0 ? normalizedFile : string.Join("/", folderSegments) + "/" + normalizedFile;
        }

        public static string ValidateCodeFilesForSnippetCreation(IEnumerable<CodeFile> codeFiles)
        {
            if (codeFiles == null)
            {
                throw new ArgumentNullException(nameof(codeFiles));
            }

            var containsMainComponent = false;
            var processedFilePaths = new HashSet<string>();
            var index = 0;
            foreach (var codeFile in codeFiles)
            {
                if (codeFile == null)
                {
                    return $"File #{index} - no file.";
                }

                if (string.IsNullOrWhiteSpace(codeFile.Path))
                {
                    return $"File #{index} - no file path.";
                }

                if (processedFilePaths.Contains(codeFile.Path))
                {
                    return $"File '{codeFile.Path}' is duplicated.";
                }

                var extension = Path.GetExtension(codeFile.Path);
                if (!ValidCodeFileExtensions.Contains(extension))
                {
                    return $"File '{codeFile.Path}' has invalid extension: {extension}. Valid extensions: {string.Join("; ", ValidCodeFileExtensions)}";
                }

                var fileName = Path.GetFileNameWithoutExtension(codeFile.Path);
                if (!SyntaxFacts.IsValidIdentifier(fileName))
                {
                    return $"'{fileName}' is not a valid file name. It must be a valid C# identifier.";
                }

                if (extension == CodeFile.RazorFileExtension && char.IsLower(fileName[0]))
                {
                    return $"'{fileName}' starts with a lowercase character. Razor file names must start with an uppercase character or _.";
                }

                if (codeFile.Path == CoreConstants.MainComponentFilePath)
                {
                    if (string.IsNullOrWhiteSpace(codeFile.Content) || codeFile.Content.Trim().Length < MainComponentFileContentMinLength)
                    {
                        return $"Main component content should be at least {MainComponentFileContentMinLength} characters long.";
                    }

                    if (codeFile.Content.Contains("@typeparam ", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return "Main.razor component should not contain @typeparam directive. Create components requiring '@typeparam' directive in separate files.";
                    }

                    containsMainComponent = true;
                }

                processedFilePaths.Add(codeFile.Path);
                index++;
            }

            if (!containsMainComponent)
            {
                return "No main component file provided.";
            }

            return null;
        }

        public static IEnumerable<CodeFile> ToCodeFiles(this string urlEncodedBase64compressedCode)
            => InlineCode.Decode(urlEncodedBase64compressedCode);
    }
}
