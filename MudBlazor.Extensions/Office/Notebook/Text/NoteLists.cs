using System.Globalization;
using System.Text;

namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>Numbering of list paragraphs.</summary>
public static class NoteLists
{
    private static readonly (int Value, string Numeral)[] Roman =
        [(1000, "m"), (900, "cm"), (500, "d"), (400, "cd"), (100, "c"), (90, "xc"), (50, "l"), (40, "xl"), (10, "x"), (9, "ix"), (5, "v"), (4, "iv"), (1, "i")];

    /// <summary>
    /// The label of each paragraph: "1.", "a." or "i." (by indent level, repeating every three levels) for numbered paragraphs, otherwise an empty
    /// string. Numbers count per indent level. A paragraph that is no list item restarts all numbering; another list item restarts its level and
    /// the deeper ones.
    /// </summary>
    /// <param name="paragraphs">The paragraphs in order.</param>
    /// <returns>One label per paragraph.</returns>
    public static IReadOnlyList<string> Numbers(IReadOnlyList<NoteParagraph> paragraphs)
    {
        ArgumentNullException.ThrowIfNull(paragraphs);
        var counters = new int[NoteParagraph.MaxIndent + 1];
        var labels = new string[paragraphs.Count];
        for (var i = 0; i < paragraphs.Count; i++)
        {
            var paragraph = paragraphs[i];
            var level = Math.Clamp(paragraph.Indent, 0, NoteParagraph.MaxIndent);
            var restart = paragraph.List switch { NoteListKind.None => 0, NoteListKind.Number => level + 1, _ => level };
            Array.Clear(counters, restart, counters.Length - restart);
            labels[i] = paragraph.List == NoteListKind.Number ? Format(++counters[level], level % 3) + "." : "";
        }

        return labels;
    }

    private static string Format(int number, int style) => style switch
    {
        1 => Letters(number),
        2 => RomanNumeral(number),
        _ => number.ToString(CultureInfo.InvariantCulture)
    };

    private static string Letters(int number)
    {
        var text = new StringBuilder();
        for (; number > 0; number = (number - 1) / 26) text.Insert(0, (char)('a' + (number - 1) % 26));
        return text.ToString();
    }

    private static string RomanNumeral(int number)
    {
        if (number >= 4000) return number.ToString(CultureInfo.InvariantCulture);
        var text = new StringBuilder();
        foreach (var (value, numeral) in Roman)
            for (; number >= value; number -= value) text.Append(numeral);
        return text.ToString();
    }
}
