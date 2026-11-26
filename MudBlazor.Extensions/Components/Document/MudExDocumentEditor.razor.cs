using MudBlazor.Extensions.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office.Document;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Word processor in the style of Word with ribbon, a web view of the document and status bar. Opens .docx, .docm, .dotx, .dotm, .doc, .rtf,
/// .odt, .html, .md and .txt files, saves the writable ones in their format and the others as .docx, and is the viewer and editor for Word,
/// RTF and OpenDocument text files in <see cref="MudExFileDisplay"/>. Without a file it edits the <see cref="TextDocument"/> parameter.
/// </summary>
public partial class MudExDocumentEditor : IMudExFileEditor
{
    private static readonly double[] ZoomLevels = [0.5, 0.75, 1, 1.25, 1.5, 2];

    private readonly EventCallback<KeyboardEventArgs> _keyDown;
    private readonly EventCallback<DocumentRange> _selectionChanged;
    private TextDocument _document;
    private TextDocument _documentParameter;
    private DocumentWordCount _words;
    private Task _changeNotification = Task.CompletedTask;
    private MudExDocumentSurface _surface;
    private Exception _reported;
    private (bool CanUndo, bool CanRedo, bool Dirty, int Words, RibbonView View, PageSetup Page) _rendered;
    private bool _trackedNoticeShown;
    private double _zoom = 1;
    private bool _collapsed;
    private bool _collapsedParameter;
    private MudExRibbonMode _mode;
    private MudExRibbonMode _modeParameter;
    private int _tabIndex;
    private bool _findOpen;
    private bool _findReplace;

    /// <summary>Creates the editor.</summary>
    public MudExDocumentEditor()
    {
        _keyDown = new EventCallback<KeyboardEventArgs>(WithoutRender.Instance, (Func<KeyboardEventArgs, Task>)OnKeyDownAsync);
        _selectionChanged = new EventCallback<DocumentRange>(WithoutRender.Instance, (Action<DocumentRange>)(_ => RenderIfChanged()));
    }

    /// <summary>
    /// The document to edit when no file is given; it is edited in place, so edits do not change this parameter (see <see cref="DocumentEdited"/>).
    /// </summary>
    [Parameter] public TextDocument TextDocument { get; set; }

    /// <summary>Raised when the editor shows another document instance: after loading a file, New and Open. Edits raise <see cref="DocumentEdited"/>.</summary>
    [Parameter] public EventCallback<TextDocument> TextDocumentChanged { get; set; }

    /// <summary>Raised after every change of the shown document, with what changed.</summary>
    [Parameter] public EventCallback<TextDocumentChangedEventArgs> DocumentEdited { get; set; }

    /// <summary>
    /// Options for every dialog the editor opens. Unset: <see cref="DialogOptionsEx.DefaultDialogOptions"/> with a small, full width dialog and a
    /// close button.
    /// </summary>
    [Parameter] public DialogOptionsEx DialogOptions { get; set; }

    /// <summary>Layout of the ribbon.</summary>
    [Parameter] public MudExRibbonMode RibbonMode { get; set; } = MudExRibbonMode.Classic;

    /// <summary>Whether the ribbon is shown.</summary>
    [Parameter] public bool ShowRibbon { get; set; } = true;

    /// <summary>Color of the ribbon header. Defaults to the Word blue.</summary>
    [Parameter] public MudExColor RibbonColor { get; set; } = "#2B579A";

    /// <summary>Whether the ribbon starts collapsed to its tab row.</summary>
    [Parameter] public bool RibbonCollapsed { get; set; }

    /// <summary>Additional ribbon tabs rendered after the built-in tabs.</summary>
    [Parameter] public RenderFragment RibbonContent { get; set; }

    /// <summary>Whether the status bar with the word count and the zoom is shown.</summary>
    [Parameter] public bool ShowStatusBar { get; set; } = true;

    /// <summary>The document currently shown, loaded from a file, created by New or given by <see cref="TextDocument"/>.</summary>
    public TextDocument CurrentDocument => _document;

    /// <summary>The selection, or the caret as an empty range.</summary>
    public DocumentRange Selection => _surface?.Selection ?? default;

    /// <summary>The zoom factor from 0.1 to 5; 1 is 100 %.</summary>
    public double Zoom => _zoom;

    /// <inheritdoc />
    public override string Name => "Document editor";

    /// <summary>Priority among the file displays for Word, RTF and OpenDocument text files.</summary>
    public int RenderPriority => 10;

    internal MudExDocumentSurface SurfaceRef => _surface;

    internal bool Locked => IsReadOnly;

    private void OpenFind(bool replace) => (_findOpen, _findReplace) = (true, replace && !IsReadOnly);

    private Task OpenFindAsync(bool replace)
    {
        OpenFind(replace);
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void CloseFind() => _findOpen = false;

    private bool IsReadOnly => ReadOnly || LoadFailed || _document == null;

    private int Words => _words?.Total ?? 0;

    private (bool CanUndo, bool CanRedo, bool Dirty, int Words, RibbonView View, PageSetup Page) RenderState =>
        (_document?.Undo.CanUndo == true, _document?.Undo.CanRedo == true, IsDirty, Words, View, CurrentPage);

    private IReadOnlyList<double> ZoomItems => ZoomLevels.Contains(_zoom) ? ZoomLevels : [.. ZoomLevels.Append(_zoom).Order()];

    private string RootStyle => $"--mud-ex-doc-accent:{MudExRibbonColors.BackgroundFor(RibbonColor)};{Style}";

    /// <inheritdoc />
    public override Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
        => Task.FromResult(fileDisplayInfos != null && DocumentFileFormats.Detect(fileDisplayInfos.FileName, fileDisplayInfos.ContentType) is
            DocumentFileFormat.Docx or DocumentFileFormat.Dotx or DocumentFileFormat.Doc or DocumentFileFormat.Rtf or DocumentFileFormat.Odt);

    /// <inheritdoc />
    public override Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
        => Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object> { { "Words", Words } });

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!ReferenceEquals(TextDocument, _documentParameter))
        {
            _documentParameter = TextDocument;
            if (TextDocument != null && _document != null && !ReferenceEquals(TextDocument, _document))
                HostReplacedDocument();
            if (TextDocument != null)
                Attach(TextDocument, null, null);
        }

        if (_document == null && FileDisplayInfos == null)
            Attach(Office.Document.TextDocument.Create(), null, null);
        if (RibbonCollapsed != _collapsedParameter)
            _collapsed = _collapsedParameter = RibbonCollapsed;
        if (RibbonMode != _modeParameter || !IsInitialized)
            _mode = _modeParameter = RibbonMode;
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        _rendered = RenderState;
    }

    internal static int CountWords(string text)
    {
        var words = 0;
        var inWord = false;
        foreach (var c in text)
        {
            var letter = c != '\uFFFC' && !char.IsWhiteSpace(c);
            if (letter && !inWord)
                words++;
            inWord = letter;
        }

        return words;
    }

    private void Attach(TextDocument document, DocumentFileFormat? loaded, string extension)
    {
        if (ReferenceEquals(document, _document))
            return;
        if (_document != null)
            _document.Changed -= OnDocumentChanged;
        (_document, _loaded, _loadedExtension) = (document, loaded, extension);
        _words = document == null ? null : new DocumentWordCount(document);
        _kept.Clear();
        _pendingKeep = null;
        _trackedNoticeShown = false;
        if (document != null)
            document.Changed += OnDocumentChanged;
    }

    private void OnDocumentChanged(object sender, TextDocumentChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, _document))
            return;
        _words.Apply(e);
        _changeNotification = InvokeAsync(async () =>
        {
            try
            {
                await NotifyEditedAsync(e);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(ex.Message);
            }

            ShowTrackedChangesNotice();
            RenderIfChanged();
        });
    }

    private async Task NotifyEditedAsync(TextDocumentChangedEventArgs e)
    {
        SupressStateChange = true;
        try
        {
            if (!IsDirty || IsSaving)
                await MarkDirtyAsync();
            else
                await ContentChanged.InvokeAsync();
            await DocumentEdited.InvokeAsync(e);
        }
        finally
        {
            SupressStateChange = false;
        }
    }

    private void ShowTrackedChangesNotice()
    {
        if (_document?.HasTrackedChanges != true || _trackedNoticeShown || IsReadOnly || DialogService == null)
            return;
        _trackedNoticeShown = true;
        _ = ShowNoticeAsync(TryLocalize("This document contains tracked changes. They are shown accepted; paragraphs you edit are saved without their marks."));
    }

    private async Task ShowNoticeAsync(string message)
    {
        try
        {
            await DialogService.ShowInformationAsync(TryLocalize("Note"), message, Icons.Material.Filled.Info);
        }
        catch (Exception)
        {
        }
    }

    private async Task RunAsync(Action<DocumentRange> action)
    {
        if (IsReadOnly || _surface == null)
            return;
        try
        {
            await _surface.RunAsync(action);
        }
        catch (Exception e) when (ReferenceEquals(e, _reported))
        {
        }

        RenderIfChanged();
    }

    private void RenderIfChanged()
    {
        if (RenderState != _rendered)
            StateHasChanged();
    }

    private Task UndoAsync(bool redo) => RunAsync(_ =>
    {
        _document.EndTyping();
        if (redo && _document.Undo.CanRedo)
            _document.Undo.Redo();
        else if (!redo && _document.Undo.CanUndo)
            _document.Undo.Undo();
    });

    private Task OnSurfaceErrorAsync(Exception e)
    {
        _reported = e;
        return ShowErrorAsync(e.Message);
    }

    private void SetZoom(double zoom) => _zoom = MudExDocumentStatusBar.ClampZoom(zoom);

    private async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
            (_menu, _mini) = (null, null);
        if ((e.CtrlKey || e.MetaKey) && !e.AltKey && e.Key is "s" or "S")
            await SaveFromKeyAsync();
    }

    private async Task SaveFromKeyAsync()
    {
        if (IsReadOnly)
            return;
        await SaveAsync();
        StateHasChanged();
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (_document != null)
            _document.Changed -= OnDocumentChanged;
        await base.DisposeAsync();
    }
}
