using System.IO.Compression;
using System.Text.Json;

namespace MainSample.WebAssembly.Examples.Notebook;

public static class NotebookSamples
{
    private const string PictureBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4z8DwHwAFAAH/q842iQAAAABJRU5ErkJggg==";
    private static readonly DateTimeOffset Stamp = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static MudBlazor.Extensions.Office.Notebook.Notebook CreateNotebook() => MudBlazor.Extensions.Office.Notebook.Notebook.Load(new MemoryStream(CreateFile()));

    public static byte[] CreateFile()
    {
        var sections = new[]
        {
            (Id: "welcome", Name: "Welcome", Color: "#7719AA", Pages: new[] { "hello", "text", "lists" }),
            (Id: "sketches", Name: "Sketches", Color: "#2B579A", Pages: new[] { "shapes", "ink" })
        };
        var pages = new Dictionary<string, object>
        {
            ["hello"] = Page("Hello notebook", 0, Heading("t1", 40, 30, "Hello notebook", "h1"),
                Text("t2", 40, 100, 520, Paragraph("Click anywhere on the page to type. Pick a pen to draw, add shapes, tables and pictures, and zoom with the status bar.")),
                Text("t3", 40, 200, 520, Rich(Run("This notebook was built in code and saved as a "), Run("mxnb", bold: true), Run(" file: a zip with one JSON file per page."))),
                Shape("s1", 600, 40, 160, 90, "rectangle", "#7719AA", "#EBD9F5"),
                Shape("s2", 620, 160, 120, 120, "ellipse", "#2B579A", "#D6E4F7")),
            ["text"] = Page("Rich text", 1, Heading("t1", 40, 30, "Rich text", "h1"),
                Text("t2", 40, 100, 560,
                    Rich(Run("Bold", bold: true), Run(", "), Run("italic", italic: true), Run(", "), Run("underlined", underline: true), Run(", "),
                        Run("struck through", strike: true), Run(", "), Run("colored", color: "#C43E1C"), Run(", "), Run("highlighted", highlight: "#FFF176"), Run(" and a "),
                        Run("link", link: "https://www.mudex.org"), Run(".")),
                    Paragraph("Centered text", align: "center"), Paragraph("Right aligned text", align: "right")),
                Text("t3", 40, 260, 560, Paragraph("Heading 2", heading: "h2"), Paragraph("Heading 3", heading: "h3"), Paragraph("Normal text again."))),
            ["lists"] = Page("Lists and tables", 1, Heading("t1", 40, 30, "Lists and tables", "h1"),
                Text("t2", 40, 100, 300,
                    Paragraph("Bullet one", list: "bullet"), Paragraph("Nested bullet", list: "bullet", indent: 1), Paragraph("Bullet two", list: "bullet"),
                    Paragraph("First step", list: "number"), Paragraph("Sub step", list: "number", indent: 1), Paragraph("Second step", list: "number")),
                Text("t3", 40, 330, 300, Paragraph("Write the notes", list: "todo", done: true), Paragraph("Draw a sketch", list: "todo"), Paragraph("Share the file", list: "todo")),
                Table("tb", 400, 100, [["Quarter", "Revenue", "Growth"], ["Q1", "1.2 M", "+4 %"], ["Q2", "1.5 M", "+25 %"], ["Q3", "1.4 M", "-7 %"]])),
            ["shapes"] = Page("Shapes and pictures", 0, Heading("t1", 40, 30, "Shapes and pictures", "h1"),
                Shape("a", 40, 100, 160, 100, "rectangle", "#C43E1C", "#F8D9D0"), Shape("b", 240, 100, 120, 120, "ellipse", "#2B579A", "#D6E4F7"),
                Shape("c", 400, 100, 120, 110, "triangle", "#217346", "#D4ECDD"), Shape("d", 560, 100, 120, 120, "diamond", "#7719AA", null),
                Shape("e", 40, 260, 200, 0, "line", "#333333", null), Shape("f", 300, 260, 200, 60, "arrow", "#C43E1C", null),
                new { type = "image", id = "pic", x = 40, y = 360, width = 160, height = 100, mediaId = "pixel", contentType = "image/png", altText = "A single blue pixel stretched into a picture" }),
            ["ink"] = Page("Handwriting", 0, Heading("t1", 40, 30, "Handwriting", "h1"),
                Ink("wave", 40, 120, 300, 80, "#2B579A", 3, "pen", Wave(300, 80, 40)),
                Ink("under", 40, 230, 300, 40, "#C43E1C", 4, "pen", Wave(300, 40, 12)),
                Ink("mark", 60, 100, 240, 28, "#FFEB3B", 18, "highlighter", Wave(240, 28, 0)),
                Ink("dot", 400, 160, 0, 0, "#000000", 6, "pen", [0, 0, 0.5]))
        };

        var entries = new Dictionary<string, byte[]>
        {
            ["notebook.json"] = Bytes(new
            {
                formatVersion = "1.0",
                name = "Sample notebook",
                sections = sections.Select(s => new { id = s.Id, name = s.Name, color = s.Color, pages = s.Pages }).ToArray()
            }),
            ["media/pixel.png"] = Convert.FromBase64String(PictureBase64)
        };
        foreach (var (id, page) in pages) entries[$"pages/{id}.json"] = Bytes(page);
        return Zip(entries);
    }

    private static byte[] Bytes(object value) => JsonSerializer.SerializeToUtf8Bytes(value, Json);

    private static byte[] Zip(Dictionary<string, byte[]> entries)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            foreach (var (name, data) in entries)
            {
                var entry = zip.CreateEntry(name, CompressionLevel.NoCompression);
                entry.LastWriteTime = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
                using var target = entry.Open();
                target.Write(data);
            }

        return stream.ToArray();
    }

    private static object Page(string title, int level, params object[] elements) => new { title, level, created = Stamp, modified = Stamp, elements };

    private static object Run(string text, bool bold = false, bool italic = false, bool underline = false, bool strike = false, string color = null, string highlight = null, string link = null) =>
        new { text, bold, italic, underline, strike, color, highlight, link };

    private static object Paragraph(string text, string list = null, int indent = 0, bool done = false, string align = null, string heading = null) =>
        Format([Run(text)], list, indent, done, align, heading);

    private static object Rich(params object[] runs) => Format(runs, null, 0, false, null, null);

    private static object Format(object[] runs, string list, int indent, bool done, string align, string heading) =>
        new { runs, list = list ?? "none", indent, @checked = done, align = align ?? "left", heading = heading ?? "none" };

    private static object Heading(string id, double x, double y, string text, string heading) => Text(id, x, y, 520, Paragraph(text, heading: heading));

    private static object Text(string id, double x, double y, double width, params object[] paragraphs) =>
        new { type = "text", id, x, y, width, height = 0, paragraphs };

    private static object Shape(string id, double x, double y, double width, double height, string kind, string stroke, string fill) =>
        new { type = "shape", id, x, y, width, height, kind, stroke, strokeWidth = 2, fill };

    private static object Table(string id, double x, double y, string[][] cells) => new
    {
        type = "table", id, x, y, width = 360, height = 30 * cells.Length, columnWidths = new[] { 120, 120, 120 }, headerRow = true,
        rows = cells.Select(row => row.Select(cell => new { paragraphs = new[] { Paragraph(cell) } }).ToArray()).ToArray()
    };

    private static object Ink(string id, double x, double y, double width, double height, string color, double strokeWidth, string tool, double[] points) =>
        new { type = "ink", id, x, y, width, height, color, strokeWidth, tool, points };

    private static double[] Wave(double width, double height, double amplitude)
    {
        var points = new List<double>();
        for (var i = 0; i <= 30; i++)
        {
            var t = i / 30.0;
            points.AddRange([Math.Round(t * width, 2), Math.Round(height / 2 + Math.Sin(t * Math.PI * 4) * amplitude * 0.5, 2), 0.6]);
        }

        return points.ToArray();
    }
}
