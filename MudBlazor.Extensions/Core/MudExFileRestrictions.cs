using MudBlazor.Extensions.Components;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Core;

/// <summary>
/// What a file has to satisfy to be accepted: its extension, its mime type and its size.
/// </summary>
/// <remarks>
/// One place for the rules, so every path that accepts a file applies the same ones - the upload dialog and a
/// file dropped straight onto a folder alike. A rule left empty allows everything.
/// </remarks>
public class MudExFileRestrictions
{
    /// <summary>Mime types, allowed or forbidden depending on <see cref="MimeRestrictionType"/>.</summary>
    public string[] MimeTypes { get; set; }

    /// <summary>Whether <see cref="MimeTypes"/> is what is allowed, or what is not.</summary>
    public RestrictionType MimeRestrictionType { get; set; } = RestrictionType.WhiteList;

    /// <summary>Extensions, allowed or forbidden depending on <see cref="ExtensionRestrictionType"/>.</summary>
    public string[] Extensions { get; set; }

    /// <summary>Whether <see cref="Extensions"/> is what is allowed, or what is not.</summary>
    public RestrictionType ExtensionRestrictionType { get; set; } = RestrictionType.WhiteList;

    /// <summary>Largest accepted file in bytes. Null or zero means no limit.</summary>
    public long? MaxFileSize { get; set; }

    /// <summary>True when nothing is restricted at all.</summary>
    public bool IsEmpty => MimeTypes?.Any() != true && Extensions?.Any() != true
                           && MaxFileSize is null or 0;

    /// <summary>
    /// Whether the extension passes.
    /// </summary>
    /// <remarks>
    /// Compared against the configured list directly: a white list allows exactly what it names, a black list
    /// forbids exactly what it names. Deriving an allowed set from mime types instead used to make black
    /// lists useless, because the derived extensions carry no leading dot and never matched the configured
    /// ones - and because a mime type maps back to only one of its extensions.
    /// </remarks>
    public bool IsExtensionAllowed(string extension)
    {
        if (Extensions?.Any() != true)
            return true;

        var listed = Extensions.Any(e => SameExtension(e, extension));
        return ExtensionRestrictionType == RestrictionType.WhiteList ? listed : !listed;
    }

    /// <summary>Whether the mime type passes.</summary>
    public bool IsMimeTypeAllowed(string mimeType)
    {
        if (MimeTypes?.Any() != true)
            return true;

        var listed = MimeType.Matches(mimeType, MimeTypes);
        return MimeRestrictionType == RestrictionType.WhiteList ? listed : !listed;
    }

    /// <summary>Whether the size passes.</summary>
    public bool IsSizeAllowed(long size)
        => MaxFileSize is null or 0 || size <= MaxFileSize.Value;

    /// <summary>
    /// Checks a file against every rule and returns why it was refused, or null when it passes.
    /// </summary>
    public string Validate(string fileName, string contentType, long size)
    {
        var extension = System.IO.Path.GetExtension(fileName ?? string.Empty);
        contentType = string.IsNullOrEmpty(contentType) ? MimeType.GetMimeType(fileName ?? string.Empty) : contentType;

        if (!IsExtensionAllowed(extension))
            return ExtensionRestrictionType == RestrictionType.WhiteList
                ? $"Files with the extension ({extension}) are not allowed. Only following types are allowed '{string.Join(',', Extensions)}'"
                : $"Files with the extension ({extension}) are not allowed. Following types are forbidden '{string.Join(',', Extensions)}'";

        if (!IsMimeTypeAllowed(contentType))
            return MimeRestrictionType == RestrictionType.WhiteList
                ? $"Files of this type ({contentType}) are not allowed. Only following types are allowed '{string.Join(',', MimeTypes)}'"
                : $"Files of this type ({contentType}) are not allowed. Following types are forbidden '{string.Join(',', MimeTypes)}'";

        if (!IsSizeAllowed(size))
            return $"The file has exceeded the maximum size of {MaxFileSize}";

        return null;
    }

    private static bool SameExtension(string left, string right)
        => string.Equals(
            (left ?? string.Empty).EnsureStartsWith('.'),
            (right ?? string.Empty).EnsureStartsWith('.'),
            StringComparison.OrdinalIgnoreCase);
}
