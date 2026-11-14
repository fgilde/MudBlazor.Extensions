using System.Security.Cryptography;
using System.Text.Json;

namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>A notebook of sections and pages in the style of OneNote, with an undo stack.</summary>
public sealed class Notebook
{
    internal const string CurrentFormatVersion = "1.0";

    private readonly HashSet<NotebookPage> _changedPages = new();
    private NotebookChangeKind _changeKind;
    private int _notifications;

    internal Notebook() => Undo = new DocumentUndoStack<Notebook>(this, n => n.RaiseChanged());

    /// <summary>The name of the notebook.</summary>
    public string Name { get; internal set; } = "Notebook";

    /// <summary>The sections in order; a notebook always has at least one.</summary>
    public IReadOnlyList<NotebookSection> Sections => SectionList;

    /// <summary>The undo and redo history.</summary>
    public DocumentUndoStack<Notebook> Undo { get; }

    /// <summary>True if the file was written by a newer major format version; the notebook can be viewed but not edited or saved.</summary>
    public bool ReadOnlyFormat { get; internal set; }

    /// <summary>True if the notebook changed since it was loaded or created.</summary>
    public bool Modified { get; internal set; }

    /// <summary>Raised once after each command, transaction, undo or redo with everything that changed.</summary>
    public event EventHandler<NotebookChangedEventArgs> Changed;

    internal List<NotebookSection> SectionList { get; } = [];

    internal NotebookPackage Package { get; set; }

    internal string FormatVersion { get; set; } = CurrentFormatVersion;

    internal Dictionary<string, JsonElement> Extra { get; set; }

    internal Dictionary<string, (byte[] Data, string Extension)> PendingMedia { get; } = new(StringComparer.OrdinalIgnoreCase);

    internal NotebookLoadOptions Limits { get; set; } = new();

    internal int Revision => _notifications;

    /// <summary>Creates a notebook with one section and one empty page.</summary>
    /// <param name="name">The name of the notebook.</param>
    /// <returns>The notebook.</returns>
    public static Notebook Create(string name = "Notebook")
    {
        var notebook = new Notebook { Name = name };
        var section = new NotebookSection { Name = "Section 1" };
        section.PageList.Add(new NotebookPage());
        notebook.SectionList.Add(section);
        return notebook;
    }

    /// <summary>Reads a notebook file (.mxnb). A page whose content cannot be read opens empty with <see cref="NotebookPage.LoadError"/> set.</summary>
    /// <param name="stream">The file content.</param>
    /// <param name="options">
    /// Limits for the unpacked size, or null for the defaults. They also apply to pictures added later and to saving, so the notebook can always be
    /// opened again with the same limits.
    /// </param>
    /// <returns>The notebook.</returns>
    /// <exception cref="InvalidDataException">The file is not a notebook, contains unsafe entry paths or exceeds the size limits.</exception>
    public static Notebook Load(Stream stream, NotebookLoadOptions options = null) => NotebookReader.Read(stream, options ?? new NotebookLoadOptions());

    /// <summary>
    /// Reads the text of a OneNote section file (.one) into a notebook with one section. Each OneNote page becomes a page with its title and creation
    /// date, and each outline becomes a text block with its paragraphs at the outline's position. Pictures, ink, tables and attached files are left out.
    /// </summary>
    /// <param name="stream">The file content.</param>
    /// <param name="fileName">The file name; without extension it names the notebook and its section. Null names the section "Section 1".</param>
    /// <param name="options">Limits for the file size, or null for the defaults. The smaller of the two limits applies to the .one file.</param>
    /// <returns>The notebook. It is not linked to the file and is saved as .mxnb.</returns>
    /// <exception cref="InvalidDataException">
    /// The file is not a OneNote section, is damaged, is password-protected, is in the packaged SharePoint/OneDrive format or exceeds the size limit.
    /// </exception>
    public static Notebook LoadOneNote(Stream stream, string fileName = null, NotebookLoadOptions options = null) =>
        OneNoteReader.Read(stream, fileName, options ?? new NotebookLoadOptions());

    /// <summary>Writes the notebook as a .mxnb file. Unchanged pages and media are copied from the loaded file; media no page uses are left out.</summary>
    /// <param name="target">The stream that receives the file; it is not written if saving fails.</param>
    /// <exception cref="InvalidOperationException">
    /// The notebook has a newer format version (<see cref="ReadOnlyFormat"/>), or the file would exceed the limits it was loaded with.
    /// </exception>
    public void Save(Stream target) => NotebookWriter.Write(this, target);

    /// <summary>Applies a command and records it as one undo step (or adds it to the open transaction). A command that changes nothing records nothing.</summary>
    /// <param name="command">The command.</param>
    /// <exception cref="InvalidOperationException">The notebook has a newer format version (<see cref="ReadOnlyFormat"/>).</exception>
    public void Execute(IDocumentCommand<Notebook> command)
    {
        if (ReadOnlyFormat) throw new InvalidOperationException("The notebook was written by a newer version and is read-only.");
        var notifications = _notifications;
        command.Apply(this);
        if (notifications == _notifications) return;
        Undo.Record(command);
        if (!Undo.InTransaction) RaiseChanged();
    }

    /// <summary>Gets the bytes of a medium, for example an image.</summary>
    /// <param name="id">The media id of an <see cref="ImageElement"/>.</param>
    /// <returns>The bytes, or null if the notebook has no such medium.</returns>
    public byte[] GetMedia(string id)
    {
        if (id == null) return null;
        if (PendingMedia.TryGetValue(id, out var pending)) return pending.Data;
        return Package?.MediaType(id) != null ? Package.Read(Package.MediaEntry(id)) : null;
    }

    internal string GetMediaType(string id) =>
        id == null ? null : PendingMedia.TryGetValue(id, out var pending) ? NotebookMediaTypes.TypeOf(pending.Extension) : Package?.MediaType(id);

    internal static string MediaIdOf(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    internal string AddMedia(byte[] data, string contentType)
    {
        ArgumentNullException.ThrowIfNull(data);
        var extension = NotebookMediaTypes.ExtensionOf(contentType) ?? throw new ArgumentException($"The content type {contentType} is not supported.", nameof(contentType));
        var id = MediaIdOf(data);
        if (!PendingMedia.ContainsKey(id) && Package?.MediaEntry(id) == null) PendingMedia[id] = (data, extension);
        return id;
    }

    internal void CheckRoom(long bytes)
    {
        var media = SectionList.SelectMany(s => s.PageList).SelectMany(p => p.ElementList).OfType<ImageElement>().Select(i => i.MediaId).Where(id => id != null)
            .Distinct(StringComparer.OrdinalIgnoreCase).Sum(MediaSize);
        var other = Package?.Names.Where(n => !NotebookPackage.IsMedia(n)).Sum(n => Package.Entry(n).Length) ?? 0;
        if (bytes > Limits.MaxEntryBytes || media + other + bytes > Limits.MaxTotalBytes)
            throw new InvalidOperationException($"The picture does not fit into the notebook: a picture may have at most {Limits.MaxEntryBytes} bytes and the whole notebook at most {Limits.MaxTotalBytes} bytes.");
    }

    internal void Notify(NotebookPage page, NotebookChangeKind kind)
    {
        _notifications++;
        Modified = true;
        if (page != null)
        {
            page.Modified = true;
            page.Version++;
            page.LastModified = DateTimeOffset.UtcNow;
            _changedPages.Add(page);
        }

        _changeKind |= kind;
    }

    private long MediaSize(string id) =>
        PendingMedia.TryGetValue(id, out var pending) ? pending.Data.Length : Package?.MediaEntry(id) is { } name ? Package.Entry(name).Length : 0;

    private void RaiseChanged()
    {
        if (_changeKind == NotebookChangeKind.None && _changedPages.Count == 0) return;
        var args = new NotebookChangedEventArgs(_changedPages.ToList(), _changeKind);
        _changedPages.Clear();
        _changeKind = NotebookChangeKind.None;
        Changed?.Invoke(this, args);
    }
}
