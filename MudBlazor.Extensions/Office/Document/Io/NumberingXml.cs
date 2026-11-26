using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Document.FormatXml;

namespace MudBlazor.Extensions.Office.Document;

internal static class NumberingXml
{
    private static readonly string[] Order = ["numPicBullet", "abstractNum", "num", "numIdMacAtCleanup"];

    internal static XDocument Write(XDocument original, Numbering numbering)
    {
        var xml = original != null
            ? new XDocument(original)
            : new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), new XElement(W + "numbering", new XAttribute(XNamespace.Xmlns + "w", W)));
        var root = xml.Root!;
        var kept = numbering.Abstracts.Select(a => a.Source).Concat(numbering.Instances.Select(i => i.Source)).Where(s => s != null).ToHashSet();
        if (original != null)
            foreach (var (source, copy) in original.Root!.Elements().Zip(root.Elements()).ToList())
                if ((source.Name == W + "abstractNum" || source.Name == W + "num") && !kept.Contains(source))
                    copy.Remove();
        foreach (var definition in numbering.Abstracts.Where(a => a.Source == null))
            Place(root, new XElement(W + "abstractNum", new XAttribute(W + "abstractNumId", definition.Id), Value("multiLevelType", definition.Multilevel ? "multilevel" : "hybridMultilevel"),
                definition.Levels.Select(Level)), Order);
        foreach (var instance in numbering.Instances.Where(i => i.Source == null))
            Place(root, new XElement(W + "num", new XAttribute(W + "numId", instance.Id), Value("abstractNumId", Number(instance.AbstractId)),
                instance.Overrides.Select(o => new XElement(W + "lvlOverride", new XAttribute(W + "ilvl", o.Level),
                    o.StartOverride is { } start ? Value("startOverride", Number(start)) : null,
                    o.Definition == null ? null : Level(o.Definition)))), Order);
        return xml;
    }

    private static XElement Level(NumberingLevel level)
    {
        var element = new XElement(W + "lvl", new XAttribute(W + "ilvl", level.Level),
            Value("start", Number(level.Start)),
            Value("numFmt", level.Format switch
            {
                NumberFormat.DecimalZero => "decimalZero",
                NumberFormat.LowerLetter => "lowerLetter",
                NumberFormat.UpperLetter => "upperLetter",
                NumberFormat.LowerRoman => "lowerRoman",
                NumberFormat.UpperRoman => "upperRoman",
                NumberFormat.Bullet => "bullet",
                NumberFormat.None => "none",
                _ => "decimal"
            }),
            level.Restart is { } restart ? Value("lvlRestart", Number(restart)) : null,
            level.Suffix is null or "tab" ? null : Value("suff", level.Suffix),
            Value("lvlText", level.Text ?? ""),
            Value("lvlJc", "left"));
        var pPr = new XElement(W + "pPr");
        PatchParagraph(pPr, level.Indent ?? new ParagraphFormat());
        var rPr = new XElement(W + "rPr");
        PatchRun(rPr, level.LabelFormat ?? new RunFormat());
        rPr.Element(W + "rFonts")?.SetAttributeValue(W + "hint", "default");
        element.Add(pPr.HasElements ? pPr : null, rPr.HasElements ? rPr : null);
        return element;
    }

    private static XElement Value(string name, string value) => new(W + name, new XAttribute(W + "val", value));

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
