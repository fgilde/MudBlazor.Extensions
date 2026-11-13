using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Office.Notebook;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Notebook editor in the style of OneNote with ribbon, section tabs, page list and status bar. Opens .mxnb files and the text of OneNote sections (.one,
/// saved as .mxnb) and is the viewer and editor for them in
/// <see cref="MudExFileDisplay"/>. Without a file it edits the <see cref="Notebook"/> parameter.
/// </summary>
public partial class MudExNotebookEditor : IMudExFileEditor
{
    private const string MxnbContentType = "application/vnd.mudex.notebook+zip";
    private const string OneNoteContentType = "application/msonenote";
    private static readonly MudExFileFormat Mxnb = new("Notebook", "mxnb", MxnbContentType);
    private static readonly MudExFileFormat OneNote = new("OneNote section", "one", OneNoteContentType);

    private readonly JsModule<MudExNotebookEditor> _js = new();
    private readonly EventCallback<KeyboardEventArgs> _keyDown;
    private readonly EventCallback<KeyboardEventArgs> _surfaceKeyDown;
    private readonly EventCallback<KeyboardEventArgs> _pagesKeyDown;
    private readonly EventCallback<KeyboardEventArgs> _sectionsKeyDown;
    private readonly EventCallback<ChangeEventArgs> _titleInputChanged;
    private readonly Dictionary<NotebookSection, NotebookPage> _lastPage = [];
    private Notebook _notebook;
    private Notebook _notebookParameter;
    private NotebookImageUrls _images;
    private NotebookPage _imagesPage;
    private Task _changeNotification = Task.CompletedTask;
    private NotebookSection _section;
    private NotebookPage _page;
    private int _sectionIndex;
    private int _pageIndex;
    private (NotebookPage Page, string Text)? _titleDraft;
    private bool _focusTitle;
    private ElementReference _root;
    private ElementReference _titleInput;
    private MudExNotebookSectionTabs _tabs;
    private MudExNotebookCanvas _canvas;
    private (double Width, double Height) _viewport;
    private double _zoom = 1;
    private NotebookTool _tool = NotebookTool.Text;
    private bool _pageListVisible = true;
    private bool _pageListParameter = true;
    private bool _sectionTabsVisible = true;
    private bool _sectionTabsParameter = true;
    private bool _collapsed;
    private bool _collapsedParameter;
    private MudExRibbonMode _mode;
    private MudExRibbonMode _modeParameter;
    private int _tabIndex;

    /// <summary>Creates the editor.</summary>
    public MudExNotebookEditor()
    {
        _keyDown = new EventCallback<KeyboardEventArgs>(WithoutRender.Instance, (Func<KeyboardEventArgs, Task>)OnKeyDownAsync);
        _surfaceKeyDown = ShortcutCallback(KeyArea.Surface);
        _pagesKeyDown = ShortcutCallback(KeyArea.Pages);
        _sectionsKeyDown = ShortcutCallback(KeyArea.Sections);
        _titleInputChanged = new EventCallback<ChangeEventArgs>(WithoutRender.Instance, (Func<ChangeEventArgs, Task>)OnTitleInputAsync);
    }

    /// <summary>The notebook to edit when no file is given. Changes by the user raise <see cref="NotebookChanged"/>.</summary>
    [Parameter] public Notebook Notebook { get; set; }

    /// <summary>Raised after every change of the notebook and when the editor replaces it (New, Open).</summary>
    [Parameter] public EventCallback<Notebook> NotebookChanged { get; set; }

    /// <summary>Layout of the ribbon.</summary>
    [Parameter] public MudExRibbonMode RibbonMode { get; set; } = MudExRibbonMode.Classic;

    /// <summary>Whether the ribbon is shown.</summary>
    [Parameter] public bool ShowRibbon { get; set; } = true;

    /// <summary>Color of the ribbon header. Defaults to the OneNote purple.</summary>
    [Parameter] public MudExColor RibbonColor { get; set; } = "#7719AA";

    /// <summary>Whether the ribbon starts collapsed to its tab row.</summary>
    [Parameter] public bool RibbonCollapsed { get; set; }

    /// <summary>Additional ribbon tabs rendered after the built-in tabs.</summary>
    [Parameter] public RenderFragment RibbonContent { get; set; }

    /// <summary>Whether the page list is shown. The user can toggle it on the View tab.</summary>
    [Parameter] public bool ShowPageList { get; set; } = true;

    /// <summary>Whether the section tabs are shown. The user can toggle them on the View tab.</summary>
    [Parameter] public bool ShowSectionTabs { get; set; } = true;

    /// <summary>
    /// The largest unpacked size of a notebook file in bytes that the editor opens. Pictures that would make the notebook larger are refused, so a saved
    /// notebook always opens again. Default 256 MB.
    /// </summary>
    [Parameter] public long MaxFileBytes { get; set; } = 256L * 1024 * 1024;

    /// <summary>The notebook currently shown, loaded from a file, created by New or given by <see cref="Notebook"/>.</summary>
    public Notebook CurrentNotebook => _notebook;

    /// <summary>The section whose pages are listed.</summary>
    public NotebookSection CurrentSection => _section;

    /// <summary>The page shown in the editing area.</summary>
    public NotebookPage CurrentPage => _page;

    /// <inheritdoc />
    public override string Name => "Notebook editor";

    /// <summary>Priority among the file displays for notebook files.</summary>
    public int RenderPriority => 10;

    /// <inheritdoc />
    public override IReadOnlyList<MudExFileFormat> SaveFormats => [Mxnb];

    /// <inheritdoc />
    protected override IReadOnlyList<MudExFileFormat> OpenFormats => [Mxnb, OneNote];

    private bool IsReadOnly => ReadOnly || LoadFailed || _notebook == null || _notebook.ReadOnlyFormat;

    private bool CanEditPage => !IsReadOnly && _page?.LoadError == null;

    private int PageIndex => _section?.PageList.IndexOf(_page) ?? -1;

    private NotebookLoadOptions Limits => new() { MaxEntryBytes = MaxFileBytes, MaxTotalBytes = MaxFileBytes };

    private string RootStyle => $"--mud-ex-notebook-accent:{MudExRibbonColors.BackgroundFor(RibbonColor)};{Style}";

    /// <inheritdoc />
    public override Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
        => Task.FromResult(fileDisplayInfos != null
                           && (string.Equals(Path.GetExtension(fileDisplayInfos.FileName ?? ""), ".mxnb", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(fileDisplayInfos.ContentType, MxnbContentType, StringComparison.OrdinalIgnoreCase)
                               || IsOneNote(fileDisplayInfos.FileName, fileDisplayInfos.ContentType)));

    private static bool IsOneNote(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName ?? "");
        return string.Equals(extension, ".one", StringComparison.OrdinalIgnoreCase)
               || string.Equals(contentType, OneNoteContentType, StringComparison.OrdinalIgnoreCase) && !string.Equals(extension, ".onetoc2", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
        => Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            { "Sections", _notebook?.Sections.Count ?? 0 },
            { "Pages", _notebook?.Sections.Sum(s => s.Pages.Count) ?? 0 }
        });

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!ReferenceEquals(Notebook, _notebookParameter))
        {
            _notebookParameter = Notebook;
            if (Notebook != null && _notebook != null && !ReferenceEquals(Notebook, _notebook))
                HostReplacedDocument();
            if (Notebook != null)
                Attach(Notebook);
        }

        if (_notebook == null && FileDisplayInfos == null)
        {
            var notebook = Office.Notebook.Notebook.Create();
            notebook.Limits = Limits;
            Attach(notebook);
        }
        if (ShowPageList != _pageListParameter)
            _pageListVisible = _pageListParameter = ShowPageList;
        if (ShowSectionTabs != _sectionTabsParameter)
            _sectionTabsVisible = _sectionTabsParameter = ShowSectionTabs;
        if (RibbonCollapsed != _collapsedParameter)
            _collapsed = _collapsedParameter = RibbonCollapsed;
        if (RibbonMode != _modeParameter || !IsInitialized)
            _mode = _modeParameter = RibbonMode;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
            await _js.StartAsync(JsRuntime, this, null, dotnet => [_root, dotnet]);
        if (_focusTitle && CanEditPage)
        {
            _focusTitle = false;
            try { await _titleInput.FocusAsync(); }
            catch (Exception e) when (e is InvalidOperationException or JSException or JSDisconnectedException or TaskCanceledException) { }
        }

        await ApplyPendingSelectionAsync();
        if (_page != null && !ReferenceEquals(_page, _imagesPage) && _images is { } images)
        {
            _imagesPage = _page;
            var version = images.Version;
            await RegisterImagesAsync();
            if (images.Version != version)
                StateHasChanged();
        }
    }

    /// <inheritdoc />
    protected override MudExFileContent CreateNew()
    {
        var stream = new MemoryStream();
        Office.Notebook.Notebook.Create().Save(stream);
        stream.Position = 0;
        return new() { FileName = "Notebook1.mxnb", ContentType = MxnbContentType, Stream = stream };
    }

    /// <inheritdoc />
    protected override async Task LoadContentAsync(MudExFileContent content)
    {
        Attach(null);
        using var buffer = new MemoryStream();
        await content.Stream.CopyToAsync(buffer);
        if (buffer.Length == 0)
            throw new InvalidDataException(TryLocalize("The file is empty."));
        buffer.Position = 0;
        var notebook = IsOneNote(content.FileName, content.ContentType)
            ? Office.Notebook.Notebook.LoadOneNote(buffer, content.FileName, Limits)
            : Office.Notebook.Notebook.Load(buffer, Limits);
        Attach(notebook);
        await NotebookChanged.InvokeAsync(notebook);
    }

    /// <inheritdoc />
    protected override async Task FlushPendingEditsAsync()
    {
        await CommitTextAsync();
        await CommitTitleAsync();
        await _changeNotification;
    }

    /// <inheritdoc />
    public override async Task<MudExFileContent> ExportAsync(MudExFileFormat format = null, CancellationToken cancellationToken = default)
    {
        await FlushPendingEditsAsync();
        var stream = new MemoryStream();
        _notebook.Save(stream);
        stream.Position = 0;
        return new MudExFileContent
        {
            FileName = Path.ChangeExtension(string.IsNullOrEmpty(FileName) ? "Notebook1.mxnb" : FileName, Mxnb.Extension),
            ContentType = MxnbContentType,
            Stream = stream
        };
    }

    private void Attach(Notebook notebook)
    {
        if (ReferenceEquals(notebook, _notebook))
            return;
        if (_notebook != null)
            _notebook.Changed -= OnNotebookChanged;
        _ = RevokeImagesAsync();
        _notebook = notebook;
        _images = notebook == null ? null : new NotebookImageUrls(notebook);
        _imagesPage = null;
        _titleDraft = null;
        _lastPage.Clear();
        var section = notebook?.Sections.FirstOrDefault();
        Show(section, section?.Pages.FirstOrDefault());
        if (notebook != null)
            notebook.Changed += OnNotebookChanged;
    }

    private void OnNotebookChanged(object sender, NotebookChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, _notebook))
            return;
        Reselect();
        _changeNotification = InvokeAsync(async () =>
        {
            try
            {
                await NotifyChangedAsync();
                await RegisterImagesAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(ex.Message);
            }

            StateHasChanged();
        });
    }

    private async Task NotifyChangedAsync()
    {
        SupressStateChange = true;
        try
        {
            await MarkDirtyAsync();
            await NotebookChanged.InvokeAsync(_notebook);
        }
        finally
        {
            SupressStateChange = false;
        }
    }

    private async Task RegisterImagesAsync()
    {
        if (_images is not { } images || _page is not { } page || _js.Js is not { } js)
            return;
        foreach (var id in page.Elements.OfType<ImageElement>().Select(i => i.MediaId).Distinct(StringComparer.OrdinalIgnoreCase).Where(images.NeedsBlob).ToList())
        {
            if (!ReferenceEquals(images, _images))
                return;
            if (images.Load(id) is not ({ } data, var contentType))
                continue;
            try
            {
                if (await js.InvokeAsync<string>("register", data, contentType) is { } url)
                    images.Register(id, url);
            }
            catch (Exception e) when (e is JSException or JSDisconnectedException or ObjectDisposedException or TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task RevokeImagesAsync()
    {
        if (_js.Js is not { } js)
            return;
        try { await js.InvokeVoidAsync("revokeAll"); }
        catch (Exception e) when (e is JSException or JSDisconnectedException or ObjectDisposedException or TaskCanceledException) { }
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        if ((e.CtrlKey || e.MetaKey) && !e.AltKey && e.Key is "s" or "S" && !IsReadOnly)
        {
            await SaveAsync();
            StateHasChanged();
        }
    }

    private EventCallback<KeyboardEventArgs> ShortcutCallback(KeyArea area)
        => new(WithoutRender.Instance, (Func<KeyboardEventArgs, Task>)(async e =>
        {
            if (await ShortcutAsync(e, area))
                StateHasChanged();
        }));

    private async Task OnTitleInputAsync(ChangeEventArgs e)
    {
        if (!CanEditPage)
            return;
        var first = _titleDraft == null;
        _titleDraft = (_page, e.Value as string ?? "");
        if (first)
            await MarkDirtyAsync();
    }

    private Task OnTitleChangedAsync(ChangeEventArgs e)
    {
        if (CanEditPage)
            _titleDraft = (_page, e.Value as string ?? "");
        return CommitTitleAsync();
    }

    private async Task CommitTitleAsync()
    {
        if (_titleDraft is not ({ } page, var text))
            return;
        _titleDraft = null;
        if (IsReadOnly || page.LoadError != null || text == page.Title || !_notebook.Sections.Any(s => s.Pages.Contains(page)))
            return;
        try
        {
            _notebook.Execute(new RenamePageCommand(page, text));
        }
        catch (Exception e)
        {
            await ShowErrorAsync(e.Message);
        }
    }

    /// <summary>Called by the browser when the visible size of the page area changed.</summary>
    /// <param name="width">The width in CSS pixels.</param>
    /// <param name="height">The height below the page title in CSS pixels.</param>
    /// <returns>A task that completes when the page was laid out again.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnViewportResized(double width, double height) => InvokeAsync(() =>
    {
        if (_viewport == (width, height))
            return;
        _viewport = (width, height);
        StateHasChanged();
    });

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (_notebook != null)
            _notebook.Changed -= OnNotebookChanged;
        await _js.DisposeAsync();
        await base.DisposeAsync();
    }
}
