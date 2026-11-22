namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

internal static class DocumentAllFeatures
{
    internal const string Author = "MudBlazor.Extensions";

    internal static TextDocument Build()
    {
        var d = TextDocument.Create();
        Add(d, "MudBlazor.Extensions document editor", "Title");

        Add(d, "1 Text formatting", "Heading1");
        var runs = Add(d, "Plain bold italic underlined struck red highlighted x2 H2O mono and larger text.");
        Format(d, runs, "bold", f => f with { Bold = true });
        Format(d, runs, "italic", f => f with { Italic = true });
        Format(d, runs, "underlined", f => f with { Underline = "single" });
        Format(d, runs, "struck", f => f with { Strike = true });
        Format(d, runs, "red", f => f with { Color = "#C00000" });
        Format(d, runs, "highlighted", f => f with { Highlight = "yellow" });
        Format(d, runs, "2 H", f => f with { VerticalAlign = VerticalAlign.Superscript }, 1);
        Format(d, runs, "2O", f => f with { VerticalAlign = VerticalAlign.Subscript }, 1);
        Format(d, runs, "mono", f => f with { Font = "Courier New" });
        Format(d, runs, "larger", f => f with { SizePt = 16 });

        Add(d, "1.1 Paragraph formatting", "Heading2");
        Add(d, "Centred paragraph.", format: f => f with { Alignment = ParagraphAlignment.Center });
        Add(d, "Right aligned paragraph.", format: f => f with { Alignment = ParagraphAlignment.Right });
        Add(d, "A justified paragraph with a left, right and first line indent, space before and after it and one and a half line spacing, long " +
               "enough to wrap onto a second line so that the justification and the indents can be seen.",
            format: f => f with
            {
                Alignment = ParagraphAlignment.Justify, IndentLeft = 720, IndentRight = 360, IndentFirstLine = 360, SpacingBefore = 240, SpacingAfter = 240,
                Line = new LineSpacing(360, LineRule.Auto)
            });

        Add(d, "1.1.1 Lists", "Heading3");
        string[] bullets = [Add(d, "First bullet"), Add(d, "Second bullet"), Add(d, "Third bullet")];
        d.Execute(new ApplyListCommand(bullets, ListTemplate.Bullet));
        string[] steps = [Add(d, "Step one"), Add(d, "Step two"), Add(d, "Detail a"), Add(d, "Detail b"), Add(d, "Step three"), Add(d, "Restarted one"), Add(d, "Restarted two")];
        d.Execute(new ApplyListCommand(steps, ListTemplate.Multilevel));
        d.Execute(new ChangeListLevelCommand([steps[2], steps[3]], 1));
        d.Execute(new RestartNumberingCommand(steps[5]));

        Add(d, "2 Table", "Heading1");
        var before = Add(d, "A table with a header row, a merged cell, shading and a nested table:");
        d.Execute(new InsertTableCommand(End(d, before), 3, 3));
        var table = (Table)d.Blocks[d.Blocks.ToList().FindIndex(b => b.Id == before) + 1];
        string[][] cells = [["Name", "Kind", "Value"], ["Alpha", "Merged across two columns", ""], ["Beta", "b", "2"]];
        for (var r = 0; r < 3; r++)
        for (var c = 0; c < 3; c++)
            if (cells[r][c].Length > 0)
                Type(d, table.Rows[r].Cells[c].Blocks[0].Id, cells[r][c]);
        d.Execute(new SetTableHeaderRowsCommand(table.Id, 1));
        d.Execute(new MergeTableCellsCommand(table.Id, 1, 1, 1, 2));
        d.Execute(new SetTableCellFormatCommand(table.Id, [(0, 0), (0, 1), (0, 2)], c => c with { Shading = "#D9E2F3" }, "Shading"));
        var host = ((Table)d.FindBlock(table.Id)).Rows[2].Cells[0].Blocks[0].Id;
        d.Execute(new InsertTableCommand(End(d, host), 2, 2));
        var nested = (Table)((Table)d.FindBlock(table.Id)).Rows[2].Cells[0].Blocks[1];
        string[] inner = ["n1", "n2", "n3", "n4"];
        for (var i = 0; i < 4; i++) Type(d, nested.Rows[i / 2].Cells[i % 2].Blocks[0].Id, inner[i]);

        Add(d, "3 Objects", "Heading1");
        var picture = Add(d, "Picture: ");
        d.Execute(new InsertImageCommand(new DocumentRange(End(d, picture), End(d, picture)), ObjectCommandTests.Png(160, 80, 0x50), "image/png"));
        d.Execute(new SetImageAltTextCommand(d.PositionOf(picture, Length(d, picture) - 1), "A grey test picture"));
        var link = Add(d, "Visit example.com for more.");
        Format(d, link, "example.com", null, link: "https://example.com/");
        var paging = Add(d, "Text before a page break.");
        d.Execute(new InsertBreakCommand(End(d, paging), BreakKind.Page));
        Type(d, paging, "Text after the page break.", Length(d, paging));

        var last = Add(d, "End of the portrait section.");
        d.Execute(new InsertSectionBreakCommand(End(d, last), SectionStart.NextPage));
        var landscape = d.Sections[^1];
        var portrait = landscape.Page;
        d.Execute(new SetPageSetupCommand(landscape.Id, portrait with { Width = portrait.Height, Height = portrait.Width, Orientation = PageOrientation.Landscape }));
        foreach (var (kind, type, text) in new[]
                 {
                     (HeaderFooterKind.Header, HeaderFooterType.Default, "Landscape header"), (HeaderFooterKind.Footer, HeaderFooterType.Default, "Landscape footer"),
                     (HeaderFooterKind.Header, HeaderFooterType.First, "First page header")
                 })
        {
            d.Execute(new AddHeaderFooterCommand(d.Sections[^1].Id, kind, type));
            var references = kind == HeaderFooterKind.Header ? d.Sections[^1].Headers : d.Sections[^1].Footers;
            Type(d, d.HeaderFooter(references[type]).Blocks[0].Id, text);
        }

        Add(d, "4 Landscape section", "Heading1");
        Add(d, "This section is in landscape orientation and has its own header, footer and first-page header.");
        d.Execute(new SetDocumentPropertiesCommand(new DocumentProperties("All features", "Document editor test fixture", Author, "docx, fixture, editor",
            "Generated by DocumentAllFeatures.Build from commands.", Author, new DateTimeOffset(2026, 11, 22, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 22, 12, 0, 0, TimeSpan.Zero))));
        return d;
    }

    private static string Add(TextDocument d, string text, string style = "Normal", Func<ParagraphFormat, ParagraphFormat> format = null)
    {
        var last = (Paragraph)d.Blocks[^1];
        if (last.Length > 0)
            d.Execute(new ReplaceTextCommand(new DocumentRange(End(d, last.Id), End(d, last.Id)), "\n", new RunFormat()));
        var id = d.Blocks[^1].Id;
        d.Execute(new SetParagraphFormatCommand([id], _ => new ParagraphFormat(), "Clear"));
        d.Execute(new ApplyParagraphStyleCommand([id], style));
        if (format != null) d.Execute(new SetParagraphFormatCommand([id], format, "Format"));
        Type(d, id, text);
        return id;
    }

    private static void Type(TextDocument d, string paragraphId, string text, int offset = 0)
    {
        var at = d.PositionOf(paragraphId, offset);
        d.Execute(new ReplaceTextCommand(new DocumentRange(at, at), text, new RunFormat()));
    }

    private static void Format(TextDocument d, string paragraphId, string text, Func<RunFormat, RunFormat> change, int length = -1, string link = null)
    {
        var start = ((Paragraph)d.FindBlock(paragraphId)).Text.IndexOf(text, StringComparison.Ordinal);
        var range = new DocumentRange(d.PositionOf(paragraphId, start), d.PositionOf(paragraphId, start + (length < 0 ? text.Length : length)));
        d.Execute(link != null ? new SetHyperlinkCommand(range, link) : new SetRunFormatCommand(range, change, "Format"));
    }

    private static DocumentPosition End(TextDocument d, string paragraphId) => d.PositionOf(paragraphId, Length(d, paragraphId));

    private static int Length(TextDocument d, string paragraphId) => ((Paragraph)d.FindBlock(paragraphId)).Length;
}
