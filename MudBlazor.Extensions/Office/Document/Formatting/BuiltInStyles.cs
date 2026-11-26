namespace MudBlazor.Extensions.Office.Document;

internal static class BuiltInStyles
{
    private const string HeadingColor = "theme:accent1 shade=BF";
    private const string MajorFont = "theme:majorHAnsi";

    private static readonly string[] Linked = ["Heading1", "Heading2", "Heading3", "Heading4", "Heading5", "Heading6", "Title", "Subtitle", "Quote", "Header", "Footer"];

    internal static readonly IReadOnlyList<DocumentStyle> All = Create();

    private static List<DocumentStyle> Create()
    {
        var single = new LineSpacing(240, LineRule.Auto);
        var pageTabs = new TabStop[] { new(4680, TabAlignment.Center, null), new(9360, TabAlignment.Right, null) };
        var border = new Border("single", 0.5, "auto");
        List<DocumentStyle> styles =
        [
            Paragraph("Normal", "Normal", 0, true, null, new ParagraphFormat { SpacingAfter = 160, Line = new LineSpacing(259, LineRule.Auto) }) with { IsDefault = true, BasedOn = null },
            Paragraph("NoSpacing", "No Spacing", 1, true, null, new ParagraphFormat { SpacingAfter = 0, Line = single }),
            Heading(1, 16, 240),
            Heading(2, 13, 40),
            Heading(3, 12, 40, color: "theme:accent1 shade=7F"),
            Heading(4, 11, 40, italic: true),
            Heading(5, 11, 40),
            Heading(6, 11, 40, color: "theme:accent1 shade=7F"),
            Paragraph("Title", "Title", 10, true, new RunFormat { SizePt = 28, Font = MajorFont }, new ParagraphFormat { SpacingAfter = 0, Line = single }),
            Paragraph("Subtitle", "Subtitle", 11, true, new RunFormat { Color = "#5A5A5A" }, new ParagraphFormat { SpacingAfter = 160 }),
            Paragraph("Quote", "Quote", 29, true, new RunFormat { Italic = true, Color = "#404040" },
                new ParagraphFormat { Alignment = ParagraphAlignment.Center, SpacingBefore = 200, SpacingAfter = 160, IndentLeft = 864, IndentRight = 864 }),
            Paragraph("ListParagraph", "List Paragraph", 34, true, null, new ParagraphFormat { IndentLeft = 720 }),
            Character("Strong", "Strong", 22, true, new RunFormat { Bold = true }),
            Character("Emphasis", "Emphasis", 20, true, new RunFormat { Italic = true }),
            Character("Hyperlink", "Hyperlink", 99, false, new RunFormat { Color = "theme:hyperlink", Underline = "single" }),
            Paragraph("Header", "header", 99, false, null, new ParagraphFormat { SpacingAfter = 0, Line = single, Tabs = pageTabs }),
            Paragraph("Footer", "footer", 99, false, null, new ParagraphFormat { SpacingAfter = 0, Line = single, Tabs = pageTabs }),
            Style(StyleType.Table, "TableGrid", "Table Grid", 39, false) with
            {
                ParagraphFormat = new ParagraphFormat { SpacingAfter = 0, Line = single },
                TableFormat = new TableFormat { Borders = new TableBorders(border, border, border, border, border, border) }
            }
        ];
        var characters = styles.Where(s => Linked.Contains(s.Id))
            .Select(s => Character(s.Id + "Char", char.ToUpperInvariant(s.Name[0]) + s.Name[1..] + " Char", s.UiPriority!.Value, false, s.RunFormat) with { Link = s.Id })
            .ToList();
        return [.. styles.Select(s => Linked.Contains(s.Id) ? s with { Link = s.Id + "Char" } : s), .. characters];
    }

    private static DocumentStyle Heading(int level, double size, int before, string color = HeadingColor, bool? italic = null) =>
        Paragraph($"Heading{level}", $"heading {level}", 9, true, new RunFormat { SizePt = size, Color = color, Font = MajorFont, Italic = italic },
            new ParagraphFormat { SpacingBefore = before, SpacingAfter = 0, KeepWithNext = true, KeepLines = true, OutlineLevel = level - 1 });

    private static DocumentStyle Paragraph(string id, string name, int priority, bool quick, RunFormat run, ParagraphFormat paragraph) =>
        Style(StyleType.Paragraph, id, name, priority, quick) with { BasedOn = "Normal", Next = "Normal", RunFormat = run ?? new RunFormat(), ParagraphFormat = paragraph };

    private static DocumentStyle Character(string id, string name, int priority, bool quick, RunFormat run) =>
        Style(StyleType.Character, id, name, priority, quick) with { RunFormat = run };

    private static DocumentStyle Style(StyleType type, string id, string name, int priority, bool quick) =>
        new() { Id = id, Name = name, Type = type, UiPriority = priority, QuickFormat = quick, BuiltIn = true };
}
