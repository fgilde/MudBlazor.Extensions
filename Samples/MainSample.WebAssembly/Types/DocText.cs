using System.Text.RegularExpressions;

namespace MainSample.WebAssembly.Types;

public static partial class DocText
{
    /// <summary>
    /// MudExResource marks a see cref in a summary as {{{P:Namespace.Type.Member}}} so MudExApiView can turn it
    /// into a link. Everywhere else it is shown as text, so only the short name is kept.
    /// </summary>
    public static string? WithoutDocLinks(this string? summary) =>
        summary == null ? null : DocLink().Replace(summary, m => m.Groups[1].Value);

    [GeneratedRegex(@"\{\{\{(?:\w:)?(?:[^{}(]*\.)?([^{}(.`]+)[^{}]*\}\}\}")]
    private static partial Regex DocLink();
}
