using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Helper.Internal;

/// <summary>
/// Line ending normalization for the text based file viewers.
/// </summary>
internal static class LineEndings
{
    // A run of carriage returns plus an optional line feed is ONE line break, not one per CR: "\r\r\n" is a
    // single broken line ending, so "\r\n?" would wrongly turn it into two newlines and invent a blank line.
    // The second alternative covers a bare LF.
    private static readonly Regex AnyLineBreak = new(@"\r+\n?|\n", RegexOptions.Compiled);

    /// <summary>
    /// Turns every line break variant into "\n". A plain <c>Replace("\r\n", "\n")</c> is not enough: on a file
    /// with doubled carriage returns ("\r\r\n") it leaves a "\r" behind, and every subsequent line comparison,
    /// regex anchor or empty line check then silently fails on that invisible character.
    /// </summary>
    public static string NormalizeLineEndings(this string text)
        => string.IsNullOrEmpty(text) ? text : AnyLineBreak.Replace(text, "\n");

    /// <summary>
    /// Normalizes the line endings and splits into lines.
    /// </summary>
    public static string[] SplitLines(this string text)
        => text.NormalizeLineEndings().Split('\n');
}
