using System.Security.Cryptography;
using System.Text;

namespace MudBlazor.Extensions.Office.Document;

/// <summary>A text document of paragraphs and tables in sections, with headers, footers and an undo stack.</summary>
public sealed partial class TextDocument
{
    private readonly HashSet<string> _changedIds = new();
    private readonly Dictionary<string, bool> _existedBefore = new();
    private HashSet<string> _beforeEdits;
    private DocumentChangeKind _changeKind;
    private int _notifications;
    private int _lastId;
    private Dictionary<string, BlockLocation> _index;
    private Dictionary<string, string> _mediaByContent;

    internal TextDocument() => Undo = new DocumentUndoStack<TextDocument>(this, d => d.RaiseChanged());

    /// <summary>The blocks of the body in order, flat across sections.</summary>
    public IReadOnlyList<Block> Blocks => BlockList;

    /// <summary>The sections in order; the last one is the final section of the body.</summary>
    public IReadOnlyList<Section> Sections => SectionList;

    /// <summary>All headers and footers.</summary>
    public IReadOnlyList<HeaderFooter> HeadersFooters => HeaderFooterList;

    /// <summary>The core document properties.</summary>
    public DocumentProperties Properties { get; internal set; } = new(null, null, null, null, null, null, null, null);

    /// <summary>The comments in file order.</summary>
    public IReadOnlyList<Comment> Comments => CommentList;

    /// <summary>True if the document contains tracked insertions, deletions or formatting changes; they are shown in their final state.</summary>
    public bool HasTrackedChanges { get; internal set; }

    /// <summary>True if the document is a template (.dotx or .dotm).</summary>
    public bool IsTemplate { get; internal set; }

    /// <summary>
    /// True if the loaded file is macro-enabled (.docm or .dotm). <see cref="Save"/> keeps it macro-enabled, so the file must be saved with a .docm or
    /// .dotm extension.
    /// </summary>
    public bool HasMacros { get; internal set; }

    /// <summary>True if even pages have their own headers and footers.</summary>
    public bool EvenAndOddHeaders { get; internal set; }

    /// <summary>The undo and redo history.</summary>
    public DocumentUndoStack<TextDocument> Undo { get; }

    /// <summary>True if the document changed since it was loaded.</summary>
    public bool Modified { get; private set; }

    /// <summary>Incremented by every change of the document; use it as render key.</summary>
    public int Version { get; private set; }

    /// <summary>Raised once after each command, transaction, undo or redo with everything that changed.</summary>
    public event EventHandler<TextDocumentChangedEventArgs> Changed;

    internal List<Block> BlockList { get; } = [];

    internal List<Section> SectionList { get; } = [];

    internal List<HeaderFooter> HeaderFooterList { get; } = [];

    internal List<Comment> CommentList { get; } = [];

    internal Dictionary<string, (byte[] Data, string ContentType)> Media { get; } = new(StringComparer.OrdinalIgnoreCase);


    internal DocxPackage Package { get; set; }

    private Dictionary<string, BlockLocation> Index => _index ??= BuildIndex();

    /// <summary>
    /// Reads a document. HTML is decoded by its byte order mark, else by the charset of a <c>meta</c> element in its first 4 KB, else as UTF-8, and may
    /// have at most 32 Mi characters; scripts, event attributes and remote images are dropped, only PNG, JPEG, GIF and BMP <c>data:</c> images are
    /// imported and only <c>http</c>, <c>https</c>, <c>mailto</c> and <c>#anchor</c> links are kept. Plain text is decoded by its byte order mark,
    /// else as UTF-8 if it is valid UTF-8, else as Windows-1252, and may have at most 32 Mi characters; every line becomes a paragraph and a form feed
    /// a page break. Markdown is decoded like plain text and read as CommonMark with pipe tables, strikethrough and autolinks; raw HTML in it is
    /// sanitised like HTML, a YAML front matter block at the start is skipped (its <c>title:</c> becomes the document title), images that are not
    /// <c>data:</c> URIs become their alternative text, and Markdown nested deeper than 128 levels is read as plain text. RTF must start with
    /// <c>{\rtf</c> (after an optional UTF-8 byte order mark and white space) and is converted with RtfPipe and read like HTML: only PNG and JPEG
    /// pictures are imported (metafiles and bitmaps are dropped), nested list levels become separate lists, vertically merged cells become
    /// separate cells, section breaks become page breaks, headers, footers and the title are dropped, and an unknown code page is read as
    /// Windows-1252; Unicode alternatives (<c>\upr</c>) are read as their ANSI text and list levels above 8 as level 8. RTF is rejected when it
    /// contains binary data (<c>\binN</c> with N above 0) or tables nested deeper than 32 levels, or has more than 32 Mi characters, 256 nested groups,
    /// 4 Mi control words, braces and tabs, or 128 Ki paragraphs, cells, rows, page and section breaks, when its HTML would exceed 32 Mi characters,
    /// when the runs times the larger of runs and characters (a date or time field counts 32 characters) of each paragraph sum to more than 2^33,
    /// or when its table work exceeds 2^27: every cell end, row end and table paragraph costs 1500 plus the rows and paragraphs before it in its
    /// section (about 3600 rows of 10 cells, or 10000 rows of one cell, in one section). ODT is read from <c>content.xml</c>, <c>styles.xml</c> and
    /// <c>meta.xml</c>: automatic and named styles become direct formatting, and a named paragraph style whose name is that of a built-in style such as
    /// <c>Heading 1</c> or <c>Title</c> also gets that style; start and end alignment follow a right-to-left writing mode, which itself is not kept; headings get Heading 1 to 6 by their outline level; each list style becomes one list
    /// definition and each top-level list restarts unless it continues another; PNG, JPEG, GIF and BMP pictures in the package are imported (as-char
    /// pictures inline, anchored ones floating at the left) and the text of text boxes follows their paragraph; footnotes and endnotes become tokens
    /// showing their citation, fields their displayed text, tracked changes their final state; comments are dropped; the first master page gives the
    /// page size, margins, headers and footers, and <c>meta.xml</c> the title. ODT is rejected when it is encrypted, has a DTD, more than 10000
    /// entries, more than 1 GiB uncompressed, an entry above 256 MiB or an XML part above 64 MiB, XML nested deeper than 256 levels, content nested
    /// deeper than 100 levels, or when reading it costs more than 32 Mi units: an XML element 8, a text character, inline element or <c>text:s</c>
    /// space 1, a paragraph 8, a table cell 32 and every 16 bytes of a distinct picture 1, with repeated rows and cells counted once per repetition.
    /// Word 97-2003 (.doc) is read from the main text of its piece table: paragraphs, tabs, line and page breaks (a section break becomes a page
    /// break), the displayed result of fields (a field code without an end ends with its paragraph), symbol characters with their font, table cells
    /// as separate paragraphs and footnote and endnote references as numbered footnote tokens; bold, italic, strike, underline, size, colour and
    /// font come from the direct formatting of each run (a toggle set to the opposite of the style is resolved against the built-in style of the
    /// paragraph) and the alignment from that of each
    /// paragraph, the built-in headings 1 to 6 get their style and headings 7 to 9 their outline level. Style formatting, lists, pictures, drawn
    /// objects, headers, footers, the text of footnotes and comments and the properties are not read, and hidden text is shown. An encrypted .doc and a
    /// Word 6/95 file are not supported; a .doc is rejected when its compound file has an invalid header, a broken or cyclic sector chain or a stream above
    /// 256 MiB, when its piece table or a formatting property list is broken or when its text has more than 32 Mi characters.
    /// </summary>
    /// <param name="stream">The file content.</param>
    /// <param name="format">The file format.</param>
    /// <returns>The document.</returns>
    /// <exception cref="NotSupportedException">The format cannot be read, or the .doc file is encrypted or older than Word 97.</exception>
    /// <exception cref="InvalidDataException">The file is not a valid document of the format.</exception>
    public static TextDocument Load(Stream stream, DocumentFileFormat format = DocumentFileFormat.Docx)
    {
        ArgumentNullException.ThrowIfNull(stream);
        switch (format)
        {
            case DocumentFileFormat.Docx or DocumentFileFormat.Dotx or DocumentFileFormat.Odt or DocumentFileFormat.Doc:
                using (var buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);
                    return format switch
                    {
                        DocumentFileFormat.Odt => OdtReader.Read(buffer.ToArray()),
                        DocumentFileFormat.Doc => DocReader.Read(buffer.ToArray()),
                        _ => DocxReader.Read(buffer.ToArray())
                    };
                }
            case DocumentFileFormat.Html:
                return HtmlReader.Read(stream);
            case DocumentFileFormat.Text:
                return TextFormat.Read(stream);
            case DocumentFileFormat.Markdown:
                return MarkdownFormat.Read(stream);
            case DocumentFileFormat.Rtf:
                return RtfFormat.Read(stream);
            default:
                throw new NotSupportedException($"Reading {format} files is not supported.");
        }
    }

    /// <summary>Creates an empty document with one empty paragraph in the Normal style and one section.</summary>
    /// <param name="page">The page size and margins; null for <see cref="PageSetup.A4"/>.</param>
    /// <returns>The document.</returns>
    public static TextDocument Create(PageSetup page = null) => DocxReader.Read(BlankDocument.Build(page ?? PageSetup.A4));

    /// <summary>
    /// Writes the document. A macro-enabled source stays macro-enabled: <see cref="DocumentFileFormat.Docx"/> then writes a .docm and
    /// <see cref="DocumentFileFormat.Dotx"/> a .dotm file, and the caller chooses the matching file extension (see <see cref="HasMacros"/>).
    /// <see cref="DocumentFileFormat.Html"/> writes a standalone UTF-8 page with images as <c>data:</c> URIs and only <c>http</c>, <c>https</c>,
    /// <c>mailto</c> and <c>#anchor</c> links; headers, footers and comments are left out. <see cref="DocumentFileFormat.Text"/> writes UTF-8 without
    /// byte order mark and with CRLF line ends: list items start with their label and a tab, table rows become tab-separated lines and fields their
    /// displayed text. <see cref="DocumentFileFormat.Markdown"/> writes UTF-8 CommonMark with GFM pipe tables (first row as header, merged cells
    /// empty except the first), headings by outline level, bold, italic and strikethrough (as <c>strong</c>, <c>em</c> and <c>del</c> tags where
    /// Markdown delimiters cannot open or close), safe links, images as <c>data:</c> URIs, lists, monospace text as code spans and monospace
    /// paragraphs with line breaks, tabs or significant spaces as fenced code. <see cref="DocumentFileFormat.Rtf"/> writes pure ASCII RTF with the effective character and paragraph
    /// formatting of every run and paragraph, the page size and margins of the last section, list labels as text with a tab and a hanging indent,
    /// tables with merged cells and single borders, nested tables as tab-separated paragraphs, PNG and JPEG images up to 16 MiB and safe links; headers,
    /// footers, comments and other images are left out. <see cref="DocumentExport.Losses"/> lists what a format cannot keep.
    /// </summary>
    /// <param name="target">The stream that receives the file.</param>
    /// <param name="format">The file format.</param>
    /// <exception cref="ArgumentNullException">The stream is null.</exception>
    /// <exception cref="ArgumentException">The stream is not writable.</exception>
    /// <exception cref="NotSupportedException">The format cannot be written.</exception>
    public void Save(Stream target, DocumentFileFormat format = DocumentFileFormat.Docx)
    {
        if (format is DocumentFileFormat.Docx or DocumentFileFormat.Dotx)
        {
            DocxWriter.Write(this, target, format == DocumentFileFormat.Dotx);
            return;
        }

        if (!DocumentFileFormats.CanWrite(format)) throw new NotSupportedException($"Writing {format} files is not supported.");
        ArgumentNullException.ThrowIfNull(target);
        if (!target.CanWrite) throw new ArgumentException("The stream is not writable.", nameof(target));
        if (format == DocumentFileFormat.Html) target.Write(new UTF8Encoding(false).GetBytes(HtmlWriter.Write(this)));
        else if (format == DocumentFileFormat.Text) TextFormat.Write(this, target);
        else if (format == DocumentFileFormat.Rtf) RtfWriter.Write(this, target);
        else MarkdownFormat.Write(this, target);
    }

    /// <summary>Gets the content of an image or other media part.</summary>
    /// <param name="mediaPart">The part name, for example <see cref="InlineImage.MediaPart"/>.</param>
    /// <returns>The bytes, or null if the document has no such part.</returns>
    public byte[] GetMedia(string mediaPart) => Media.TryGetValue(mediaPart, out var media) ? media.Data : null;

    /// <summary>Gets the content type of a media part.</summary>
    /// <param name="mediaPart">The part name.</param>
    /// <returns>The content type, or null if the document has no such part.</returns>
    public string MediaContentType(string mediaPart) => Media.TryGetValue(mediaPart, out var media) ? media.ContentType : null;

    /// <summary>Applies a command and records it as one undo step (or adds it to the open transaction). A command that changes nothing records nothing.</summary>
    /// <param name="command">The command.</param>
    public void Execute(IDocumentCommand<TextDocument> command) => Execute(command, null);

    /// <summary>Finds a block of the body, of a table cell at any depth or of a header or footer.</summary>
    /// <param name="id">The block id.</param>
    /// <returns>The block, or null if there is none with this id.</returns>
    public Block FindBlock(string id) => TryLocate(id, out var location) ? location.Block : null;

    /// <summary>Gets a header or footer.</summary>
    /// <param name="id">Its id.</param>
    /// <returns>The header or footer, or null if there is none with this id.</returns>
    public HeaderFooter HeaderFooter(string id) => HeaderFooterList.FirstOrDefault(h => h.Id == id);

    /// <summary>Gets the header or footer that applies to a section; a section without its own reference inherits the previous section's.</summary>
    /// <param name="section">The section.</param>
    /// <param name="kind">Header or footer.</param>
    /// <param name="type">The pages it applies to.</param>
    /// <returns>The header or footer, or null if neither the section nor a previous one references one.</returns>
    public HeaderFooter HeaderFor(Section section, HeaderFooterKind kind, HeaderFooterType type)
    {
        for (var i = SectionList.IndexOf(section); i >= 0; i--)
            if ((kind == HeaderFooterKind.Header ? SectionList[i].Headers : SectionList[i].Footers).TryGetValue(type, out var id))
                return HeaderFooter(id);
        return null;
    }

    internal static TextDocument FromBlocks(IEnumerable<Block> blocks)
    {
        var document = new TextDocument();
        document.BlockList.AddRange(blocks.Select(document.Identify));
        return document;
    }

    internal string NewId() => $"b{++_lastId}";

    internal string AddMedia(byte[] data, string contentType, Func<string, string> newPart)
    {
        _mediaByContent ??= Media.GroupBy(m => MediaKey(m.Value.Data, m.Value.ContentType)).ToDictionary(g => g.Key, g => g.First().Key);
        var hash = Convert.ToHexString(SHA256.HashData(data));
        if (_mediaByContent.TryGetValue(hash + " " + contentType, out var part)) return part;
        part = newPart(hash);
        if (Media.TryAdd(part, (data, contentType))) _mediaByContent.TryAdd(hash + " " + contentType, part);
        return part;
    }

    internal void PutMedia(string part, byte[] data, string contentType)
    {
        if (Media.TryAdd(part, (data, contentType))) _mediaByContent?.TryAdd(MediaKey(data, contentType), part);
    }

    private static string MediaKey(byte[] data, string contentType) => Convert.ToHexString(SHA256.HashData(data)) + " " + contentType;

    internal Block Identify(Block block)
    {
        block = block with { Id = NewId() };
        return block switch
        {
            Table table => table with { Rows = table.Rows.Select(r => r with { Cells = r.Cells.Select(c => c with { Blocks = c.Blocks.Select(Identify).ToList() }).ToList() }).ToList() },
            OpaqueBlock { Preview: not null } opaque => opaque with { Preview = opaque.Preview.Select(Identify).ToList() },
            _ => block
        };
    }

    internal Block EnsureIds(Block block)
    {
        if (Identified([block])) return block;
        var result = block.Id == null ? block with { Id = NewId() } : block;
        return result is Table table ? table with { Rows = table.Rows.Select(EnsureIds).ToList() } : result;
    }

    /// <summary>
    /// Optional: drops the block index so the next lookup rebuilds it. Lookups detect stale and missing entries by themselves, so no caller needs
    /// this for correctness; it only saves the staleness check after a bulk change.
    /// </summary>
    internal void InvalidateIndex() => _index = null;

    internal void Notify(string blockId, DocumentChangeKind kind) =>
        Track(TryLocate(blockId, out var location) ? Container(location.Container)[location.Index].Id : blockId, kind);

    internal Block ReplaceBlock(Block old, Block replacement)
    {
        if (ReferenceEquals(old, replacement)) return replacement;
        InvalidateIndex();
        if (!TryLocate(old.Id, out var location)) throw new ArgumentException($"Block {old.Id} is not part of the document.", nameof(old));
        replacement = EnsureIds(replacement);
        var ownIds = Subtree(location.Block).Select(b => b.Id).ToHashSet();
        var seen = new HashSet<string>();
        foreach (var block in Subtree(replacement))
            if (!seen.Add(block.Id) || !ownIds.Contains(block.Id) && Index.ContainsKey(block.Id))
                throw new ArgumentException($"Block {block.Id} occurs twice in the replacement or already exists elsewhere in the document.", nameof(replacement));
        var top = Container(location.Container)[location.Index];
        var newTop = Replace(top, location.Path, replacement);
        if (location.Container < 0)
            BlockList[location.Index] = newTop;
        else
            HeaderFooterList[location.Container] = HeaderFooterList[location.Container] with { Blocks = With(Container(location.Container), location.Index, newTop) };
        InvalidateIndex();
        var structure = TrackBlocks([location.Block], [replacement]);
        var kind = (location.Container < 0 ? DocumentChangeKind.Content : DocumentChangeKind.HeadersFooters) |
                   (structure || replacement.Id != location.Block.Id ? DocumentChangeKind.Structure : DocumentChangeKind.None);
        _changedIds.UnionWith(ownIds);
        Track(top.Id, kind);
        if (newTop.Id != top.Id) Track(newTop.Id, kind);
        return replacement;
    }

    private TableRow EnsureIds(TableRow row) =>
        Identified(row.Cells.SelectMany(c => c.Blocks))
            ? row
            : row with { Cells = row.Cells.Select(c => Identified(c.Blocks) ? c : c with { Blocks = c.Blocks.Select(EnsureIds).ToList() }).ToList() };

    private static bool Identified(IEnumerable<Block> blocks) => blocks.SelectMany(Subtree).All(b => b.Id != null);

    private static IEnumerable<Block> Subtree(Block block) =>
        block is Table table ? table.Rows.SelectMany(r => r.Cells).SelectMany(c => c.Blocks).SelectMany(Subtree).Prepend(block) : [block];

    private bool TryLocate(string id, out BlockLocation location)
    {
        location = default;
        if (id == null) return false;
        var fresh = _index == null;
        if (Index.TryGetValue(id, out location) && ReferenceEquals(Walk(TopAt(location.Container, location.Index), location.Path), location.Block)) return true;
        if (fresh) return false;
        InvalidateIndex();
        return Index.TryGetValue(id, out location);
    }

    private Block TopAt(int container, int index) =>
        container < HeaderFooterList.Count && index < Container(container).Count ? Container(container)[index] : null;

    private static Block Walk(Block block, IEnumerable<CellStep> path)
    {
        foreach (var (r, c, b) in path)
        {
            if (block is not Table table || r < 0 || r >= table.Rows.Count || c < 0 || c >= table.Rows[r].Cells.Count || b < 0 || b >= table.Rows[r].Cells[c].Blocks.Count)
                return null;
            block = table.Rows[r].Cells[c].Blocks[b];
        }

        return block;
    }

    private IReadOnlyList<Block> Container(int container) => container < 0 ? BlockList : HeaderFooterList[container].Blocks;

    private static Block Replace(Block block, ReadOnlySpan<CellStep> path, Block replacement)
    {
        if (path.IsEmpty) return replacement;
        var (r, c, b) = path[0];
        var table = (Table)block;
        var row = table.Rows[r];
        var cell = row.Cells[c];
        var newCell = cell with { Blocks = With(cell.Blocks, b, Replace(cell.Blocks[b], path[1..], replacement)) };
        return table with { Rows = With(table.Rows, r, row with { Cells = With(row.Cells, c, newCell) }) };
    }

    private static IReadOnlyList<T> With<T>(IReadOnlyList<T> list, int index, T item)
    {
        var copy = list.ToArray();
        copy[index] = item;
        return copy;
    }

    internal int IndexBuilds { get; private set; }

    private Dictionary<string, BlockLocation> BuildIndex()
    {
        IndexBuilds++;
        var index = new Dictionary<string, BlockLocation>();
        for (var container = -1; container < HeaderFooterList.Count; container++)
        {
            var blocks = Container(container);
            for (var i = 0; i < blocks.Count; i++) AddToIndex(index, container, i, [], blocks[i]);
        }

        return index;
    }

    private static void AddToIndex(Dictionary<string, BlockLocation> index, int container, int top, CellStep[] path, Block block)
    {
        if (block.Id != null) index[block.Id] = new BlockLocation(container, top, path, block);
        if (block is not Table table) return;
        for (var r = 0; r < table.Rows.Count; r++)
        for (var c = 0; c < table.Rows[r].Cells.Count; c++)
        {
            var blocks = table.Rows[r].Cells[c].Blocks;
            for (var b = 0; b < blocks.Count; b++) AddToIndex(index, container, top, [.. path, new CellStep(r, c, b)], blocks[b]);
        }
    }

    private void Track(string topId, DocumentChangeKind kind)
    {
        _notifications++;
        Modified = true;
        Version++;
        if (topId != null) _changedIds.Add(topId);
        _changeKind |= kind;
    }

    private void RaiseChanged()
    {
        if (_changeKind == DocumentChangeKind.None && _changedIds.Count == 0) return;
        InvalidateIndex();
        var changed = new HashSet<string>();
        var removed = new List<string>();
        foreach (var id in _changedIds)
            if (TryLocate(id, out var location))
                changed.Add(Container(location.Container)[location.Index].Id);
            else
                removed.Add(id);
        var (changedNested, addedNested, removedNested) = (new List<string>(), new List<string>(), new List<string>());
        foreach (var (id, existed) in _existedBefore)
            (TryLocate(id, out _) ? existed ? changedNested : addedNested : existed ? removedNested : null)?.Add(id);
        if (addedNested.Count > 0 || removedNested.Count > 0) _changeKind |= DocumentChangeKind.Structure;
        var args = new TextDocumentChangedEventArgs(changed, removed, _changeKind)
        {
            ChangedNestedBlockIds = changedNested, AddedNestedBlockIds = addedNested, RemovedNestedBlockIds = removedNested
        };
        _changedIds.Clear();
        _existedBefore.Clear();
        _changeKind = DocumentChangeKind.None;
        Changed?.Invoke(this, args);
    }

    private bool TrackBlocks(IEnumerable<Block> removed, IEnumerable<Block> inserted)
    {
        var before = removed.SelectMany(Subtree).ToHashSet<Block>(ReferenceEqualityComparer.Instance);
        var after = inserted.SelectMany(Subtree).ToHashSet<Block>(ReferenceEqualityComparer.Instance);
        var beforeIds = before.Select(b => b.Id).ToHashSet();
        foreach (var block in before.Where(b => !after.Contains(b)).Concat(after.Where(b => !before.Contains(b))))
            if (block.Id != null)
            {
                _changedIds.Add(block.Id);
                _existedBefore.TryAdd(block.Id, beforeIds.Contains(block.Id) || _beforeEdits?.Contains(block.Id) == true);
            }

        return !beforeIds.SetEquals(after.Select(b => b.Id));
    }

    private readonly record struct BlockLocation(int Container, int Index, CellStep[] Path, Block Block);
}
