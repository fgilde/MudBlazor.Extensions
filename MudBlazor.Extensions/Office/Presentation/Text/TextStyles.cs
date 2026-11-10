using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Presentation;

internal static class TextStyles
{
    private static readonly RunFormat Defaults = new(false, false, false, false, 18, "scheme:tx1");

    internal static RunFormat EffectiveFormat(this AutoShape shape, TextRun run)
    {
        var level = shape.Text?.Paragraphs.FirstOrDefault(p => p.Runs.Any(r => ReferenceEquals(r, run)))?.Level ?? 0;
        var chain = Chain(shape).ToList();
        var format = chain.Select(s => PptxReader.ReadRunFormat(ListStyle(s)?.Element(Level(level))?.Element(PptxReader.A + "defRPr")))
            .Concat(chain.Select(s => new RunFormat(Color: ColorResolver.FromXml(s.Source?.Element(PptxReader.P + "style")?.Element(PptxReader.A + "fontRef")?.Elements().FirstOrDefault()))))
            .Append(PptxReader.ReadRunFormat(MasterLevel(shape, level)?.Element(PptxReader.A + "defRPr")))
            .Append(Defaults)
            .Aggregate(run.Format, Merge);
        return format with { Color = ColorResolver.Resolve(format.Color, shape.Master), Font = ThemeFont(format.Font, shape.Master) };
    }

    internal static IEnumerable<XElement> ListLevels(SlideShape shape, int level) =>
        Chain(shape).Select(s => ListStyle(s)?.Element(Level(level))).Append(MasterLevel(shape, level)).Where(e => e != null);

    internal static TextBody EffectiveBody(this AutoShape shape)
    {
        var bodies = Chain(shape).Select(s => (s as AutoShape)?.Text).Where(b => b != null).ToList();
        T Pick<T>(Func<TextBody, T?> value, T fallback) where T : struct => bodies.Select(value).FirstOrDefault(v => v != null) ?? fallback;
        return (shape.Text ?? new TextBody()) with
        {
            Anchor = Pick(b => b.Anchor, TextAnchor.Top),
            InsetLeft = Pick(b => b.InsetLeft, 91440L),
            InsetTop = Pick(b => b.InsetTop, 45720L),
            InsetRight = Pick(b => b.InsetRight, 91440L),
            InsetBottom = Pick(b => b.InsetBottom, 45720L),
            Wrap = Pick(b => b.Wrap, true)
        };
    }

    internal static TextAlign EffectiveAlign(this AutoShape shape, TextParagraph paragraph) =>
        paragraph.Align ?? ListLevels(shape, paragraph.Level).Select(l => ParseAlign((string)l.Attribute("algn"))).FirstOrDefault(a => a != null) ?? TextAlign.Left;

    internal static TextAlign? ParseAlign(string value) => value switch
    {
        "l" => TextAlign.Left,
        "ctr" => TextAlign.Center,
        "r" => TextAlign.Right,
        "just" or "justLow" or "dist" or "thaiDist" => TextAlign.Justify,
        _ => null
    };

    internal static TextAnchor? ParseAnchor(string value) => value switch
    {
        "t" or "just" or "dist" => TextAnchor.Top,
        "ctr" => TextAnchor.Middle,
        "b" => TextAnchor.Bottom,
        _ => null
    };

    private static IEnumerable<SlideShape> Chain(SlideShape shape)
    {
        for (var s = shape; s != null; s = s.InheritsFrom) yield return s;
    }

    private static XElement ListStyle(SlideShape shape) => shape.Source?.Element(PptxReader.P + "txBody")?.Element(PptxReader.A + "lstStyle");

    private static XName Level(int level) => PptxReader.A + $"lvl{level + 1}pPr";

    private static XElement MasterLevel(SlideShape shape, int level)
    {
        var styles = shape.Placeholder?.Type switch
        {
            null => shape.Master?.DefaultTextStyle,
            "title" or "ctrTitle" => shape.Master?.TextStyles?.Element(PptxReader.P + "titleStyle"),
            "dt" or "ftr" or "sldNum" or "hdr" => shape.Master?.TextStyles?.Element(PptxReader.P + "otherStyle"),
            _ => shape.Master?.TextStyles?.Element(PptxReader.P + "bodyStyle")
        };
        return styles?.Element(Level(level));
    }

    private static RunFormat Merge(RunFormat a, RunFormat b) =>
        new(a.Bold ?? b.Bold, a.Italic ?? b.Italic, a.Underline ?? b.Underline, a.Strike ?? b.Strike, a.SizePt ?? b.SizePt, a.Color ?? b.Color, a.Font ?? b.Font);

    private static string ThemeFont(string font, SlideMaster master) => font switch
    {
        ['+', 'm', 'j', ..] => master?.MajorFont,
        ['+', 'm', 'n', ..] => master?.MinorFont,
        _ => font
    };
}
