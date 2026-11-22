namespace MudBlazor.Extensions.Office.Document;

/// <summary>A predefined list definition.</summary>
public enum ListTemplate
{
    /// <summary>Bullets &#x2022;, o and &#x25AA;, repeating over the levels.</summary>
    Bullet,

    /// <summary>Decimal numbers on every level.</summary>
    Decimal,

    /// <summary>1., a. and i., repeating over the levels.</summary>
    Multilevel
}

internal static class ListTemplates
{
    private static readonly (string Text, string Font)[] Bullets = [("\uF0B7", "Symbol"), ("o", "Courier New"), ("\uF0A7", "Wingdings")];

    private static readonly NumberFormat[] Multilevel = [NumberFormat.Decimal, NumberFormat.LowerLetter, NumberFormat.LowerRoman];

    internal static AbstractNumbering Create(ListTemplate template) => new(0, Enumerable.Range(0, 9).Select(level => Level(template, level)).ToList()) { Multilevel = template == ListTemplate.Multilevel };

    private static NumberingLevel Level(ListTemplate template, int level)
    {
        var (format, text, font) = template switch
        {
            ListTemplate.Bullet => (NumberFormat.Bullet, Bullets[level % 3].Text, Bullets[level % 3].Font),
            ListTemplate.Decimal => (NumberFormat.Decimal, $"%{level + 1}.", null),
            _ => (Multilevel[level % 3], $"%{level + 1}.", null)
        };
        return new NumberingLevel(level, 1, format, text, null, new ParagraphFormat { IndentLeft = 720 * (level + 1), IndentFirstLine = -360 }, new RunFormat { Font = font }, "tab");
    }
}
