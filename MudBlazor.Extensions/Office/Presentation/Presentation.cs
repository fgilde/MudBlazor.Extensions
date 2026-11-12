namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>A presentation of slides with their layouts and masters, and an undo stack.</summary>
public sealed class Presentation
{
    private readonly HashSet<Slide> _changedSlides = new();
    private PresentationChangeKind _changeKind;

    internal Presentation() => Undo = new DocumentUndoStack<Presentation>(this, p => p.RaiseChanged());

    /// <summary>Slide width in EMU.</summary>
    public long SlideWidth { get; internal set; } = 12192000;

    /// <summary>Slide height in EMU.</summary>
    public long SlideHeight { get; internal set; } = 6858000;

    /// <summary>The slides in show order.</summary>
    public IReadOnlyList<Slide> Slides => SlideList;

    /// <summary>The slide masters.</summary>
    public IReadOnlyList<SlideMaster> Masters => MasterList;

    /// <summary>The layouts of all masters.</summary>
    public IReadOnlyList<SlideLayout> Layouts => LayoutList;

    /// <summary>The undo and redo history.</summary>
    public DocumentUndoStack<Presentation> Undo { get; }

    /// <summary>True if a slide contains content the editor keeps but does not model, such as charts, SmartArt, tables or media.</summary>
    public bool HasOpaqueContent => SlideList.Any(s => SlideShape.Flatten(s.Shapes).Any(shape => shape is OpaqueShape));

    /// <summary>Raised once after each command, transaction, undo or redo with everything that changed.</summary>
    public event EventHandler<PresentationChangedEventArgs> Changed;

    internal List<Slide> SlideList { get; } = [];

    internal List<SlideMaster> MasterList { get; } = [];

    internal List<SlideLayout> LayoutList { get; } = [];

    internal PptxPackage Package { get; set; }

    internal int FirstSlideNumber { get; set; } = 1;

    internal Dictionary<string, (byte[] Data, string ContentType)> PendingMedia { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Reads a .pptx file.</summary>
    /// <param name="stream">The file content.</param>
    /// <returns>The presentation.</returns>
    public static Presentation Load(Stream stream) => PptxReader.Read(stream);

    /// <summary>Creates a blank 16:9 presentation with one title slide.</summary>
    /// <returns>The presentation.</returns>
    public static Presentation Create() => PptxReader.Read(new MemoryStream(BlankPresentation.Build()));

    /// <summary>Writes the presentation as a .pptx file. Unchanged parts are copied from the loaded file.</summary>
    /// <param name="target">The stream that receives the file.</param>
    public void Save(Stream target) => PptxWriter.Write(this, target);

    /// <summary>Applies a command and records it as one undo step (or adds it to the open transaction).</summary>
    /// <param name="command">The command.</param>
    public void Execute(IDocumentCommand<Presentation> command)
    {
        command.Apply(this);
        Undo.Record(command);
        if (!Undo.InTransaction) RaiseChanged();
    }

    internal bool CanHaveNotes(Slide slide) => slide.HasNotesPart || Package?.NotesMasterPart != null;

    internal string PartOf(Slide slide) => Package?.Slides.FirstOrDefault(p => p.Slide == (slide.CopyOf ?? slide))?.PartName;

    internal void Notify(Slide slide, PresentationChangeKind kind)
    {
        if (slide != null)
        {
            slide.Modified = true;
            slide.Version++;
            _changedSlides.Add(slide);
        }

        _changeKind |= kind;
    }

    private void RaiseChanged()
    {
        if (_changeKind == PresentationChangeKind.None && _changedSlides.Count == 0) return;
        var args = new PresentationChangedEventArgs(_changedSlides.ToList(), _changeKind);
        _changedSlides.Clear();
        _changeKind = PresentationChangeKind.None;
        Changed?.Invoke(this, args);
    }
}
