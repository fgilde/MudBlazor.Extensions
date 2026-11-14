using System.Buffers.Binary;
using System.Text;

namespace MudBlazor.Extensions.Office.Notebook;

internal static class OneNoteReader
{
    private const uint SectionNode = 0x00060007;
    private const uint PageSeriesNode = 0x00060008;
    private const uint PageNode = 0x0006000B;
    private const uint OutlineNode = 0x0006000C;
    private const uint OutlineElementNode = 0x0006000D;
    private const uint RichTextNode = 0x0006000E;
    private const uint OutlineGroup = 0x00060019;
    private const uint PageManifestNode = 0x00060037;
    private const uint PageMetaData = 0x00020030;

    private const uint ContentChildNodes = 0x24001C1F;
    private const uint ElementChildNodes = 0x24001C20;
    private const uint ChildGraphSpaceElementNodes = 0x2C001D63;
    private const uint CachedTitleString = 0x1C001CF3;
    private const uint TopologyCreationTimeStamp = 0x18001C65;
    private const uint LastModifiedTime = 0x14001D7A;
    private const uint PageLevel = 0x14001DFF;
    private const uint OffsetFromParentHoriz = 0x14001C14;
    private const uint OffsetFromParentVert = 0x14001C15;
    private const uint LayoutMaxWidth = 0x14001C1B;
    private const uint RichEditTextUnicode = 0x1C001C22;
    private const uint TextExtendedAscii = 0x1C003498;
    private const uint TextRunIndex = 0x1C001E12;
    private const uint TextRunFormatting = 0x24001E13;
    private const uint Bold = 0x08001C04;
    private const uint Italic = 0x08001C05;
    private const uint Underline = 0x08001C06;
    private const uint Strikethrough = 0x08001C07;
    private const uint Hidden = 0x08001E16;

    private const uint ContentRoot = 1;
    private const uint MetaDataRoot = 2;
    private const double PixelsPerHalfInch = 48;
    private const double MaxCoordinate = 100_000;
    private const double DefaultWidth = 400;
    private const double LineHeight = 24;
    private const int MaxDepth = 64;
    private const string Windows1252High =
        "€\u0081‚ƒ„…†‡ˆ‰Š‹Œ\u008DŽ\u008F" +
        "\u0090‘’“”•–—˜™š›œ\u009DžŸ";
    private static readonly DateTimeOffset Time32Epoch = new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static Notebook Read(Stream stream, string fileName, NotebookLoadOptions options)
    {
        var store = OneStore.Read(stream, options);
        var name = Path.GetFileNameWithoutExtension(fileName ?? "");
        var notebook = new Notebook { Name = string.IsNullOrWhiteSpace(name) ? "Notebook" : name, Limits = options };
        var section = new NotebookSection { Name = string.IsNullOrWhiteSpace(name) ? "Section 1" : name };
        notebook.SectionList.Add(section);

        var root = store.Spaces[store.Root];
        var sectionNode = root.Root(ContentRoot, SectionNode) ?? throw OneStore.Damaged("the section node is missing");
        var pages = new HashSet<OneId>();
        foreach (var seriesId in root.Properties(sectionNode).Ids(ElementChildNodes))
        {
            if (root.Object(seriesId, PageSeriesNode) is not { } series) continue;
            foreach (var spaceId in root.Properties(series).Ids(ChildGraphSpaceElementNodes))
                if (pages.Add(spaceId) && store.Spaces.TryGetValue(spaceId, out var space) && ReadPage(space) is { } page)
                    section.PageList.Add(page);
        }

        if (section.PageList.Count == 0) section.PageList.Add(new NotebookPage());
        return notebook;
    }

    private static NotebookPage ReadPage(OneSpace space)
    {
        if (space.Root(ContentRoot, PageManifestNode) is not { } manifest) return null;
        var pageNode = space.Properties(manifest).Ids(ContentChildNodes).Select(id => space.Object(id, PageNode)).FirstOrDefault(o => o != null);
        if (pageNode == null) return null;

        var page = new NotebookPage();
        var pageProperties = space.Properties(pageNode);
        if (space.Root(MetaDataRoot, PageMetaData) is { } metaData)
        {
            var meta = space.Properties(metaData);
            page.Title = Clean(Utf16(meta.Bytes(CachedTitleString))).Trim();
            page.Level = meta.Number(PageLevel) is { } level ? (int)Math.Clamp((long)level - 1, 0, NotebookPage.MaxLevel) : 0;
            if (FileTime(meta.Number(TopologyCreationTimeStamp)) is { } created) page.Created = created;
        }

        page.LastModified = pageProperties.Number(LastModifiedTime) is { } modified ? Time32Epoch.AddSeconds((uint)modified) : page.Created;
        var nextY = LineHeight;
        var visited = new HashSet<OneId>();
        foreach (var outlineId in pageProperties.Ids(ElementChildNodes))
        {
            if (space.Object(outlineId, OutlineNode) is not { } outline) continue;
            var properties = space.Properties(outline);
            var paragraphs = new List<NoteParagraph>();
            AddElements(space, properties.Ids(ElementChildNodes), 0, paragraphs, visited, 0);
            if (NotebookEdit.IsEmpty(paragraphs)) continue;

            var x = Pixels(properties.Float(OffsetFromParentHoriz));
            var y = Pixels(properties.Float(OffsetFromParentVert));
            if (x == null || y == null) (x, y) = (PixelsPerHalfInch, nextY);
            nextY = Math.Max(nextY, y.Value + LineHeight * (paragraphs.Count + 1));
            var width = Pixels(properties.Float(LayoutMaxWidth)) is { } w and > 0 ? w : DefaultWidth;
            page.ElementList.Add(new TextElement { X = x.Value, Y = y.Value, Width = width, Paragraphs = paragraphs });
        }

        return page;
    }

    private static void AddElements(OneSpace space, IReadOnlyList<OneId> ids, int indent, List<NoteParagraph> paragraphs, HashSet<OneId> visited, int depth)
    {
        if (depth > MaxDepth) throw OneStore.Damaged("outlines nested too deeply");
        foreach (var id in ids)
        {
            if (!visited.Add(id) || !space.Objects.TryGetValue(id, out var element)) continue;
            var properties = space.Properties(element);
            if (element.Jcid == OutlineGroup)
            {
                AddElements(space, properties.Ids(ElementChildNodes), indent, paragraphs, visited, depth + 1);
                continue;
            }

            if (element.Jcid != OutlineElementNode) continue;
            foreach (var contentId in properties.Ids(ContentChildNodes))
                if (space.Object(contentId, RichTextNode) is { } text)
                    AddParagraphs(space, space.Properties(text), Math.Min(indent, NoteParagraph.MaxIndent), paragraphs);
            AddElements(space, properties.Ids(ElementChildNodes), indent + 1, paragraphs, visited, depth + 1);
        }
    }

    private static void AddParagraphs(OneSpace space, OneProperties properties, int indent, List<NoteParagraph> paragraphs)
    {
        var text = properties.Bytes(RichEditTextUnicode) is { } unicode ? Utf16(unicode)
            : properties.Bytes(TextExtendedAscii) is { } ascii ? Windows1252(ascii) : "";
        var ends = properties.Bytes(TextRunIndex) ?? [];
        var formats = properties.Ids(TextRunFormatting);
        var runs = new List<NoteRun>();
        var start = 0;
        for (var i = 0; i <= ends.Length / 4 && start <= text.Length; i++)
        {
            var end = i < ends.Length / 4 ? (int)Math.Clamp(BinaryPrimitives.ReadUInt32LittleEndian(ends.AsSpan(i * 4)), start, text.Length) : text.Length;
            var format = i < formats.Count && space.Objects.TryGetValue(formats[i], out var style) ? space.Properties(style) : null;
            var lines = text[start..end].Split(['\v', '\r', '\n']);
            for (var line = 0; line < lines.Length; line++)
            {
                if (line > 0) Close(runs, indent, paragraphs);
                if (format?.Bool(Hidden) != true && Clean(lines[line]) is { Length: > 0 } clean)
                    runs.Add(new NoteRun(clean, format == null ? new NoteRunFormat()
                        : new NoteRunFormat(format.Bool(Bold), format.Bool(Italic), format.Bool(Underline), format.Bool(Strikethrough))));
            }

            start = end;
            if (end == text.Length) break;
        }

        Close(runs, indent, paragraphs);
    }

    private static void Close(List<NoteRun> runs, int indent, List<NoteParagraph> paragraphs)
    {
        paragraphs.Add(new NoteParagraph { Runs = [.. runs], Indent = indent });
        runs.Clear();
    }

    private static double? Pixels(float? halfInches) =>
        halfInches is { } value ? Math.Clamp(Math.Round(value * PixelsPerHalfInch, 2), 0, MaxCoordinate) : null;

    private static DateTimeOffset? FileTime(ulong? value) =>
        value is > 0 and < 2650467743999999999 ? new DateTimeOffset(DateTime.FromFileTimeUtc((long)value.Value)) : null;

    private static string Utf16(byte[] bytes) => bytes == null ? "" : Encoding.Unicode.GetString(bytes, 0, bytes.Length & ~1).TrimEnd('\0');

    private static string Windows1252(byte[] bytes) =>
        string.Create(bytes.Length, bytes, (chars, source) =>
        {
            for (var i = 0; i < source.Length; i++) chars[i] = source[i] is >= 0x80 and < 0xA0 ? Windows1252High[source[i] - 0x80] : (char)source[i];
        }).TrimEnd('\0');

    private static string Clean(string text) =>
        text.Any(c => c < ' ' && c != '\t' || c == '﷟') ? new string(text.Where(c => !(c < ' ' && c != '\t' || c == '﷟')).ToArray()) : text;
}
