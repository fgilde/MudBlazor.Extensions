namespace MudBlazor.Extensions.Office.Document;

/// <summary>A text document of paragraphs and tables in sections, with headers, footers and an undo stack.</summary>
public sealed partial class TextDocument
{
    private readonly HashSet<string> _changedIds = new();
    private DocumentChangeKind _changeKind;
    private int _notifications;
    private int _lastId;
    private Dictionary<string, BlockLocation> _index;

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

    /// <summary>True if the document is a template (.dotx).</summary>
    public bool IsTemplate { get; internal set; }

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

    /// <summary>Reads a document.</summary>
    /// <param name="stream">The file content.</param>
    /// <param name="format">The file format.</param>
    /// <returns>The document.</returns>
    /// <exception cref="NotSupportedException">The format cannot be read.</exception>
    /// <exception cref="InvalidDataException">The file is not a valid document of the format.</exception>
    public static TextDocument Load(Stream stream, DocumentFileFormat format = DocumentFileFormat.Docx)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (format is not (DocumentFileFormat.Docx or DocumentFileFormat.Dotx)) throw new NotSupportedException($"Reading {format} files is not supported.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return DocxReader.Read(buffer.ToArray());
    }

    /// <summary>Creates an empty document with one empty paragraph in the Normal style and one section.</summary>
    /// <param name="page">The page size and margins; null for <see cref="PageSetup.A4"/>.</param>
    /// <returns>The document.</returns>
    public static TextDocument Create(PageSetup page = null) => DocxReader.Read(BlankDocument.Build(page ?? PageSetup.A4));

    /// <summary>Writes the document.</summary>
    /// <param name="target">The stream that receives the file.</param>
    /// <param name="format">The file format.</param>
    /// <exception cref="ArgumentNullException">The stream is null.</exception>
    /// <exception cref="ArgumentException">The stream is not writable.</exception>
    /// <exception cref="NotSupportedException">The format cannot be written.</exception>
    public void Save(Stream target, DocumentFileFormat format = DocumentFileFormat.Docx)
    {
        if (format is not (DocumentFileFormat.Docx or DocumentFileFormat.Dotx)) throw new NotSupportedException($"Writing {format} files is not supported.");
        DocxWriter.Write(this, target, format == DocumentFileFormat.Dotx);
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
        var kind = (location.Container < 0 ? DocumentChangeKind.Content : DocumentChangeKind.HeadersFooters) |
                   (replacement.Id != location.Block.Id ? DocumentChangeKind.Structure : DocumentChangeKind.None);
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

    private Dictionary<string, BlockLocation> BuildIndex()
    {
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
        var args = new TextDocumentChangedEventArgs(changed, removed, _changeKind);
        _changedIds.Clear();
        _changeKind = DocumentChangeKind.None;
        Changed?.Invoke(this, args);
    }

    private readonly record struct BlockLocation(int Container, int Index, CellStep[] Path, Block Block);
}
