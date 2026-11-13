namespace MudBlazor.Extensions.Office.Notebook;

internal static class NoteValues
{
    public static string NewId() => Guid.NewGuid().ToString("N");

    public static bool IsId(string value) => value is { Length: > 0 and <= 64 } && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    public static bool IsColor(string value) => value is { Length: 7 } && value[0] == '#' && value.Skip(1).All(char.IsAsciiHexDigit);

    public static bool IsFont(string value) => value is { Length: > 0 and <= 100 } && value.All(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_');

    public static bool IsSafeLink(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" or "mailto"
                                                         && value.StartsWith(uri.Scheme + ":", StringComparison.OrdinalIgnoreCase);
}
