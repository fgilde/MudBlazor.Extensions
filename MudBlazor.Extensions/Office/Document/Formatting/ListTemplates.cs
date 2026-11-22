namespace MudBlazor.Extensions.Office.Document;

/// <summary>A predefined list definition.</summary>
public enum ListTemplate
{
    /// <summary>Bullets •, o and ▪, repeating over the levels.</summary>
    Bullet,

    /// <summary>Decimal numbers on every level.</summary>
    Decimal,

    /// <summary>1., a. and i., repeating over the levels.</summary>
    Multilevel
}

internal static class ListTemplates
{
    private static readonly string[] Bullets = ["•", "o", "▪"];

    private static readonly NumberFormat[] Multilevel = [NumberFormat.Decimal, NumberFormat.LowerLetter, NumberFormat.LowerRoman];

    internal static AbstractNumbering Create(ListTemplate template) => new(0, Enumerable.Range(0, 9).Select(level => Level(template, level)).ToList());

    private static NumberingLevel Level(ListTemplate template, int level)
    {
        var (format, text) = template switch
        {
            ListTemplate.Bullet => (NumberFormat.Bullet, Bullets[level % 3]),
            ListTemplate.Decimal => (NumberFormat.Decimal, $"%{level + 1}."),
            _ => (Multilevel[level % 3], $"%{level + 1}.")
        };
        return new NumberingLevel(level, 1, format, text, null, new ParagraphFormat { IndentLeft = 720 * (level + 1), IndentFirstLine = -360 }, new RunFormat(), "tab");
    }
}
