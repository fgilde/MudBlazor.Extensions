using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Document.FormatXml;

namespace MudBlazor.Extensions.Office.Document;

internal static class NumberingXml
{
    internal static Numbering Read(XDocument numbering)
    {
        var root = numbering?.Root;
        return new Numbering(
            root?.Elements(W + "abstractNum").Select(a => new AbstractNumbering(Int(a, "abstractNumId") ?? 0, a.Elements(W + "lvl").Select(Level).ToList()) { Source = a }) ?? [],
            root?.Elements(W + "num").Select(n => new NumberingInstance(Int(n, "numId") ?? 0, Int(n.Element(W + "abstractNumId"), "val") ?? 0, n.Elements(W + "lvlOverride").Select(Override).ToList())
            {
                Source = n
            }) ?? []);
    }

    private static LevelOverride Override(XElement levelOverride) =>
        new(Int(levelOverride, "ilvl") ?? 0, Int(levelOverride.Element(W + "startOverride"), "val"), levelOverride.Element(W + "lvl") is { } level ? Level(level) : null);

    private static NumberingLevel Level(XElement level) =>
        new(Int(level, "ilvl") ?? 0,
            Int(level.Element(W + "start"), "val") ?? 0,
            Val(level, "numFmt") switch
            {
                null or "decimal" => NumberFormat.Decimal,
                "decimalZero" => NumberFormat.DecimalZero,
                "lowerLetter" => NumberFormat.LowerLetter,
                "upperLetter" => NumberFormat.UpperLetter,
                "lowerRoman" => NumberFormat.LowerRoman,
                "upperRoman" => NumberFormat.UpperRoman,
                "bullet" => NumberFormat.Bullet,
                "none" => NumberFormat.None,
                _ => NumberFormat.Other
            },
            Val(level, "lvlText"),
            Int(level.Element(W + "lvlRestart"), "val"),
            FormatXml.Paragraph(level.Element(W + "pPr")) ?? new ParagraphFormat(),
            Run(level.Element(W + "rPr")) ?? new RunFormat(),
            Val(level, "suff") ?? "tab")
        {
            Source = level
        };
}
