using System.Collections;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Services;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Renders file metadata as label/value rows, grouped and typed.
/// </summary>
/// <remarks>
/// Renders whatever <see cref="IMudExFileDisplay.FileMetaInformationAsync"/> returned, since the metadata of
/// a file has no fixed shape - an mp3 reports a bitrate, a csv its rows and columns.
/// <para>
/// Values are rendered by type: a bool as a yes/no icon, a date in local time, a url as a link, a byte count
/// as a readable size, a collection as chips. A key of the form <c>Group.Field</c>, or a nested dictionary,
/// opens a group.
/// </para>
/// </remarks>
public partial class MudExFileMetaView : MudExBaseComponent<MudExFileMetaView>
{
    [Inject] private IJsApiService JsApiService { get; set; }

    private string _filter;

    /// <summary>
    /// Name of the file.
    /// </summary>
    [Parameter, SafeCategory("Data")] public string FileName { get; set; }

    /// <summary>
    /// Content type (mime type) of the file.
    /// </summary>
    [Parameter, SafeCategory("Data")] public string ContentType { get; set; }

    /// <summary>
    /// Size of the file in bytes.
    /// </summary>
    [Parameter, SafeCategory("Data")] public long? Size { get; set; }

    /// <summary>
    /// Last modified date of the file.
    /// </summary>
    [Parameter, SafeCategory("Data")] public DateTimeOffset? LastModified { get; set; }

    /// <summary>
    /// Path of the file.
    /// </summary>
    [Parameter, SafeCategory("Data")] public string Path { get; set; }

    /// <summary>The metadata to render, in whatever shape the viewer reported it.</summary>
    [Parameter, SafeCategory("Data")] public IDictionary<string, object> MetaInformation { get; set; }

    /// <summary>Renders null, empty and whitespace values instead of skipping them.</summary>
    [Parameter, SafeCategory("Behavior")] public bool ShowEmptyValues { get; set; }

    /// <summary>Compact vertical spacing.</summary>
    [Parameter, SafeCategory("Appearance")] public bool Dense { get; set; } = true;

    /// <summary>Shows a heading per group. Off renders one flat list.</summary>
    [Parameter, SafeCategory("Appearance")] public bool ShowGroups { get; set; } = true;

    /// <summary>Shows a copy button per row and one for the whole set.</summary>
    [Parameter, SafeCategory("Appearance")] public bool AllowCopy { get; set; } = true;

    /// <summary>Shows a filter box from this many rows on. Zero never shows it.</summary>
    [Parameter, SafeCategory("Appearance")] public int FilterThreshold { get; set; } = 10;

    /// <summary>
    /// Name of the group the file's own properties go into.
    /// </summary>
    [Parameter, SafeCategory("Appearance")] public string FileGroupName { get; set; } = "File";

    /// <summary>
    /// Name of the group metadata without a group of its own goes into.
    /// </summary>
    [Parameter, SafeCategory("Appearance")] public string DetailsGroupName { get; set; } = "Details";

    private string RowClass => Dense ? "mud-ex-file-meta-row mud-ex-file-meta-row-dense" : "mud-ex-file-meta-row";

    /// <summary>One label/value pair, already assigned to a group.</summary>
    private sealed record MetaRow(string Group, string Label, object Value, string Icon = null);

    /// <summary>The rows to render: the file's own properties first, then what the viewer reported.</summary>
    private List<IGrouping<string, MetaRow>> Groups => BuildRows()
        .Where(Matches)
        .GroupBy(r => r.Group)
        .ToList();

    private int RowCount => BuildRows().Count();

    private bool ShowFilter => FilterThreshold > 0 && RowCount >= FilterThreshold;

    private IEnumerable<MetaRow> BuildRows()
    {
        var rows = new List<MetaRow>();

        Add(rows, FileGroupName, "File name", FileName, BrowserFileExt.GetIcon(FileName, ContentType));
        Add(rows, FileGroupName, "Content type", ContentType);
        Add(rows, FileGroupName, "Size", Size.HasValue ? new ByteSize(Size.Value) : null);
        Add(rows, FileGroupName, "Last modified", LastModified);
        Add(rows, FileGroupName, "Path", Path);

        if (MetaInformation != null)
        {
            foreach (var entry in MetaInformation.Where(e => !IsAlreadyShown(e.Key)))
                AddMeta(rows, entry.Key, entry.Value);
        }

        return rows;
    }

    /// <summary>
    /// The reported metadata carries the file's own properties too, so those keys are skipped when the
    /// intrinsic parameters already cover them.
    /// </summary>
    private bool IsAlreadyShown(string key) => key?.ToLowerInvariant() switch
    {
        "file" or "filename" => !string.IsNullOrWhiteSpace(FileName),
        "contenttype" or "mimetype" => !string.IsNullOrWhiteSpace(ContentType),
        "size" => Size.HasValue,
        "path" => !string.IsNullOrWhiteSpace(Path),
        "lastmodified" => LastModified.HasValue,
        _ => false
    };

    /// <summary>Places one entry. A nested dictionary or a dotted key opens a group of its own.</summary>
    private void AddMeta(List<MetaRow> rows, string key, object value)
    {
        if (value is IDictionary nested)
        {
            foreach (DictionaryEntry entry in nested)
                Add(rows, Humanize(key), Humanize(entry.Key?.ToString()), entry.Value);
            return;
        }

        var separator = key?.LastIndexOfAny(new[] { '.', '/' }) ?? -1;
        if (separator > 0 && separator < key.Length - 1)
        {
            Add(rows, Humanize(key.Substring(0, separator)), Humanize(key.Substring(separator + 1)), value);
            return;
        }

        Add(rows, DetailsGroupName, Humanize(key), value);
    }

    private void Add(List<MetaRow> rows, string group, string label, object value, string icon = null)
    {
        if (ShouldShow(value))
            rows.Add(new MetaRow(group, label, value, icon));
    }

    private bool Matches(MetaRow row)
    {
        if (string.IsNullOrWhiteSpace(_filter))
            return true;

        return Contains(row.Label) || Contains(row.Group) || Contains(AsText(row.Value));

        bool Contains(string text) => text?.Contains(_filter, StringComparison.OrdinalIgnoreCase) == true;
    }

    private bool ShouldShow(object value)
    {
        if (ShowEmptyValues) return true;
        return value switch
        {
            null => false,
            string s => !string.IsNullOrWhiteSpace(s),
            ICollection { Count: 0 } => false,
            _ => true
        };
    }

    /// <summary>
    /// Turns a key into a label: localized when possible, otherwise split on the casing.
    /// </summary>
    private string Humanize(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return key;
        if (IsLocalized(key))
            return TryLocalize(key);

        var words = key.Replace('_', ' ').Trim().SplitByUpperCase().Select(w => w.Trim()).Where(w => w.Length > 0).ToList();
        if (words.Count == 0)
            return key;

        var first = words[0];
        var head = char.ToUpper(first[0], CultureInfo.CurrentCulture) + first.Substring(1);
        return string.Join(" ", new[] { head }.Concat(words.Skip(1).Select(w => w.ToLower(CultureInfo.CurrentCulture))));
    }

    /// <summary>A size in bytes, so it is not rendered as a plain number.</summary>
    private readonly record struct ByteSize(long Bytes)
    {
        public override string ToString() => Nextended.Blazor.Extensions.BrowserFileExtensions.GetReadableFileSize(Bytes);
    }

    private static bool IsUrl(string value)
        => value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
           || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static bool IsMail(string value)
        => value.Count(c => c == '@') == 1 && !value.Contains(' ') && value.Contains('.');

    private static bool IsColor(string value)
        => value.Length is 4 or 7 or 9 && value.StartsWith('#')
           && value.Skip(1).All(Uri.IsHexDigit);

    /// <summary>A value's plain text form, used for the filter and for copying.</summary>
    private static string AsText(object value) => value switch
    {
        null => string.Empty,
        bool b => b ? "yes" : "no",
        DateTimeOffset d => d.LocalDateTime.ToString("g", CultureInfo.CurrentCulture),
        DateTime d => d.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
        TimeSpan t => t.ToString(t.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss", CultureInfo.CurrentCulture),
        ByteSize s => s.ToString(),
        string s => s,
        IFormattable f when IsNumeric(value) => f.ToString("N0", CultureInfo.CurrentCulture),
        IEnumerable e => string.Join(", ", e.Cast<object>().Select(AsText)),
        _ => value.ToString()
    };

    private static bool IsNumeric(object value)
        => value is byte or sbyte or short or ushort or int or uint or long or ulong or decimal
            || (value is float f && f == MathF.Round(f))
            || (value is double d && d == Math.Round(d));

    private async Task CopyAsync(string text)
    {
        if (JsApiService != null)
            await JsApiService.CopyToClipboardAsync(text);
    }

    /// <summary>Copies every visible row as <c>label: value</c> lines, grouped.</summary>
    private Task CopyAllAsync()
    {
        var lines = Groups.SelectMany(group => new[] { $"[{TryLocalize(group.Key)}]" }
            .Concat(group.Select(row => $"{row.Label}: {AsText(row.Value)}"))
            .Append(string.Empty));

        return CopyAsync(string.Join(Environment.NewLine, lines).TrimEnd());
    }
}
