using System.IO.Compression;
using System.Text;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Notebook;

using MudBlazor.Extensions.Office.Notebook;

internal sealed class NotebookBuilder
{
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

    private readonly List<(string Id, string Extra, List<string> Pages)> _sections = [];

    public string Version { get; set; } = "1.0";

    public string ManifestExtra { get; set; } = "";

    public Dictionary<string, byte[]> Entries { get; } = new();

    public NotebookBuilder Section(string id, string extra = "")
    {
        _sections.Add((id, extra, []));
        return this;
    }

    public NotebookBuilder Page(string id, string elements = "", string extra = "") =>
        RawPage(id, $"{{ \"title\" : \"{id}\",  \"elements\" : [ {elements} ]{extra} }}");

    public NotebookBuilder RawPage(string id, string json)
    {
        if (_sections.Count == 0) Section("s1");
        _sections[^1].Pages.Add(id);
        Entries[$"pages/{id}.json"] = Encoding.UTF8.GetBytes(json);
        return this;
    }

    public NotebookBuilder PageRef(string id)
    {
        _sections[^1].Pages.Add(id);
        return this;
    }

    public NotebookBuilder Media(string id, string extension, byte[] data)
    {
        Entries[$"media/{id}.{extension}"] = data;
        return this;
    }

    public byte[] Build()
    {
        var sections = string.Join(",", _sections.Select(s =>
            $"{{\"id\":\"{s.Id}\",\"name\":\"{s.Id}\",\"color\":\"#112233\",\"pages\":[{string.Join(",", s.Pages.Select(p => $"\"{p}\""))}]{s.Extra}}}"));
        var entries = new Dictionary<string, byte[]>(Entries)
        {
            ["notebook.json"] = Encoding.UTF8.GetBytes($"{{\"formatVersion\":\"{Version}\",\"name\":\"Test\",\"sections\":[{sections}]{ManifestExtra}}}")
        };
        return Zip(entries);
    }

    public Notebook Load(NotebookLoadOptions? options = null) => Notebook.Load(new MemoryStream(Build()), options);

    public static string Image(string id, string mediaId) =>
        $"{{\"type\":\"image\",\"id\":\"{id}\",\"x\":0,\"y\":0,\"width\":10,\"height\":10,\"mediaId\":\"{mediaId}\",\"contentType\":\"image/png\"}}";

    public static string Text(string id, string runs, string paragraphExtra = "") =>
        $"{{\"type\":\"text\",\"id\":\"{id}\",\"x\":5,\"y\":5,\"width\":100,\"paragraphs\":[{{\"runs\":[{runs}]{paragraphExtra}}}]}}";

    public static byte[] Zip(IReadOnlyDictionary<string, byte[]> entries)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            foreach (var (name, data) in entries)
            {
                var entry = zip.CreateEntry(name);
                entry.LastWriteTime = new DateTimeOffset(2021, 3, 4, 5, 6, 7, TimeSpan.Zero);
                using var stream = entry.Open();
                stream.Write(data);
            }

        return ms.ToArray();
    }

    public static byte[] PatchUncompressedSize(byte[] zip, string name, int size)
    {
        var copy = (byte[])zip.Clone();
        BitConverter.GetBytes(size).CopyTo(copy, Header(copy, name, 0x04034b50, 26, 30) + 22);
        BitConverter.GetBytes(size).CopyTo(copy, Header(copy, name, 0x02014b50, 28, 46) + 24);
        return copy;
    }

    public static byte[] CorruptData(byte[] zip, string name)
    {
        var copy = (byte[])zip.Clone();
        var header = Header(copy, name, 0x04034b50, 26, 30);
        copy[header + 30 + BitConverter.ToUInt16(copy, header + 26) + BitConverter.ToUInt16(copy, header + 28)] = 0xFF;
        return copy;
    }

    public static Dictionary<string, byte[]> Unzip(byte[] zip)
    {
        using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
        return archive.Entries.ToDictionary(e => e.FullName, e =>
        {
            using var stream = e.Open();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        });
    }

    public static byte[] Save(Notebook notebook)
    {
        using var ms = new MemoryStream();
        notebook.Save(ms);
        return ms.ToArray();
    }

    public static Notebook Reload(Notebook notebook) => Notebook.Load(new MemoryStream(Save(notebook)));

    public static NotebookPage Page(Notebook notebook, string id) => notebook.Sections.SelectMany(s => s.Pages).Single(p => p.Id == id);

    public static Notebook AllKinds()
    {
        var notebook = Notebook.Create("All kinds");
        var page = notebook.Sections[0].Pages[0];
        page.Title = "Kinds";
        page.Level = 1;
        var media = notebook.AddMedia(Png, "image/png");
        page.ElementList.AddRange(
        [
            new TextElement
            {
                Id = "text", X = 10, Y = 20, Width = 300,
                Paragraphs =
                [
                    new NoteParagraph
                    {
                        Runs =
                        [
                            new NoteRun("Bold ", new NoteRunFormat(Bold: true, Italic: true, Underline: true, Strike: true, SizePt: 14, Color: "#FF0000",
                                Highlight: "#FFFF00", Font: "Arial", Link: "https://example.test/a")),
                            new NoteRun("plain")
                        ],
                        List = NoteListKind.Todo, Indent = 2, Checked = true, Align = NoteAlign.Center, Heading = NoteHeading.H2
                    },
                    new NoteParagraph { Runs = [new NoteRun("second")], List = NoteListKind.Number }
                ]
            },
            new InkElement
            {
                Id = "ink", X = 1.5, Y = 2.25, Width = 30, Height = 40, Points = [new InkPoint(0, 0, 0.5), new InkPoint(30, 40, 1)], Color = "#0000FF",
                StrokeWidth = 3, Tool = InkTool.Highlighter
            },
            new ShapeElement
            {
                Id = "shape", X = 50, Y = 60, Width = 70, Height = 80, Kind = NoteShapeKind.Arrow, Stroke = "#00FF00", StrokeWidth = 2, Fill = "#CCCCCC",
                FlipX = true
            },
            new ImageElement { Id = "image", X = 100, Y = 110, Width = 64, Height = 48, MediaId = media, ContentType = "image/png", AltText = "Alt" },
            new TableElement
            {
                Id = "table", X = 200, Y = 300, Width = 220, ColumnWidths = [100, 120], HeaderRow = true,
                Rows = [[Cell("a"), Cell("b")], [Cell("c"), Cell("d")]]
            }
        ]);

        var second = new NotebookSection { Id = "second", Name = "Second", Color = "#00AA00" };
        second.PageList.Add(new NotebookPage { Id = "p2", Title = "Two", Created = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.FromHours(2)) });
        second.PageList.Add(new NotebookPage { Id = "p3", Title = "Three", Level = 2 });
        notebook.SectionList.Add(second);
        return notebook;
    }

    private static int Header(byte[] zip, string name, uint signature, int nameLengthOffset, int nameOffset)
    {
        var bytes = Encoding.UTF8.GetBytes(name);
        for (var i = 0; i < zip.Length - nameOffset; i++)
            if (BitConverter.ToUInt32(zip, i) == signature && BitConverter.ToUInt16(zip, i + nameLengthOffset) == bytes.Length
                                                          && zip.AsSpan(i + nameOffset, bytes.Length).SequenceEqual(bytes))
                return i;
        throw new InvalidOperationException($"No header for {name}.");
    }

    private static NoteTableCell Cell(string text) => new() { Paragraphs = [new NoteParagraph { Runs = [new NoteRun(text)] }] };
}
