using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Office.Presentation;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Presentation editor in the style of PowerPoint with ribbon, slide panel, notes pane and status bar.
/// Opens pptx files and is the editor for them in <see cref="MudExFileDisplay"/>. Without a file it edits the <see cref="Presentation"/> parameter.
/// </summary>
public partial class MudExSlideEditor : IMudExFileEditor
{
    private const string PptxContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
    private static readonly MudExFileFormat Pptx = new("PowerPoint Presentation", "pptx", PptxContentType);

    private readonly EventCallback<KeyboardEventArgs> _keyDown;
    private readonly EventCallback<KeyboardEventArgs> _navigationKeyDown;
    private Presentation _presentation;
    private Presentation _presentationParameter;
    private SlideImageUrls _images;
    private bool _imagesReady;
    private Slide _current;
    private int _currentIndex;
    private IReadOnlyList<Slide> _selection = [];
    private ElementReference _root;
    private IJSObjectReference _module;
    private IJSObjectReference _js;
    private DotNetObjectReference<MudExSlideEditor> _dotnet;
    private bool _jsStarted;
    private Slide _revealed;
    private MudExSlidePanel _panel;
    private MudExSlideNotes _notes;
    private MudExSlideCanvas _canvas;
    private double? _zoom;
    private bool _notesVisible = true;
    private bool _notesParameter = true;
    private bool _panelVisible = true;
    private bool _panelParameter = true;
    private bool _collapsed;
    private bool _collapsedParameter;
    private MudExRibbonMode _mode;
    private MudExRibbonMode _modeParameter;

    /// <summary>Creates the editor.</summary>
    public MudExSlideEditor()
    {
        _keyDown = new EventCallback<KeyboardEventArgs>(WithoutRender.Instance, (Func<KeyboardEventArgs, Task>)OnKeyDownAsync);
        _navigationKeyDown = new EventCallback<KeyboardEventArgs>(WithoutRender.Instance, (Func<KeyboardEventArgs, Task>)OnNavigationKeyDownAsync);
    }

    /// <summary>The presentation to edit when no file is given. Changes by the user raise <see cref="PresentationChanged"/>.</summary>
    [Parameter] public Presentation Presentation { get; set; }

    /// <summary>Raised after every change of the presentation and when the editor replaces it (New, Open).</summary>
    [Parameter] public EventCallback<Presentation> PresentationChanged { get; set; }

    /// <summary>Layout of the ribbon.</summary>
    [Parameter] public MudExRibbonMode RibbonMode { get; set; } = MudExRibbonMode.Classic;

    /// <summary>Whether the ribbon is shown.</summary>
    [Parameter] public bool ShowRibbon { get; set; } = true;

    /// <summary>Color of the ribbon header. Defaults to the PowerPoint orange.</summary>
    [Parameter] public MudExColor RibbonColor { get; set; } = "#C43E1C";

    /// <summary>Whether the ribbon starts collapsed to its tab row.</summary>
    [Parameter] public bool RibbonCollapsed { get; set; }

    /// <summary>Additional ribbon tabs rendered after the built-in tabs.</summary>
    [Parameter] public RenderFragment RibbonContent { get; set; }

    /// <summary>Whether the notes pane is shown. The user can toggle it on the View tab.</summary>
    [Parameter] public bool ShowNotes { get; set; } = true;

    /// <summary>Whether the slide panel with the thumbnails is shown. The user can toggle it on the View tab.</summary>
    [Parameter] public bool ShowSlidePanel { get; set; } = true;

    /// <summary>The presentation currently shown, loaded from a file, created by New or given by <see cref="Presentation"/>.</summary>
    public Presentation CurrentPresentation => _presentation;

    /// <summary>The slide shown in the editing area.</summary>
    public Slide CurrentSlide => _current;

    /// <summary>The slides selected in the slide panel, in show order.</summary>
    public IReadOnlyList<Slide> SelectedSlides => _selection;

    /// <summary>The ids of the shapes selected on the current slide.</summary>
    public IReadOnlyList<uint> SelectedShapes => _canvas?.Selection ?? [];

    /// <inheritdoc />
    public override string Name => "Slide editor";

    /// <summary>Priority among the file displays for presentation files.</summary>
    public int RenderPriority => 10;

    /// <inheritdoc />
    public override IReadOnlyList<MudExFileFormat> SaveFormats => [Pptx];

    private bool IsReadOnly => ReadOnly || LoadFailed || _presentation == null;

    private bool HasShapeSelection => !IsReadOnly && SelectedShapes.Count > 0;

    private string RootStyle => $"--mud-ex-slide-accent:{MudExRibbonColors.BackgroundFor(RibbonColor)};{Style}";

    /// <inheritdoc />
    public override Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
        => Task.FromResult(fileDisplayInfos != null
                           && (string.Equals(Path.GetExtension(fileDisplayInfos.FileName ?? ""), ".pptx", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(fileDisplayInfos.ContentType, PptxContentType, StringComparison.OrdinalIgnoreCase)));

    /// <inheritdoc />
    public override Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
        => Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object> { { "Slides", _presentation?.Slides.Count ?? 0 } });

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!ReferenceEquals(Presentation, _presentationParameter))
        {
            _presentationParameter = Presentation;
            if (Presentation != null)
                Attach(Presentation);
        }

        if (_presentation == null && FileDisplayInfos == null)
            Attach(Office.Presentation.Presentation.Create());
        if (ShowNotes != _notesParameter)
            _notesVisible = _notesParameter = ShowNotes;
        if (ShowSlidePanel != _panelParameter)
            _panelVisible = _panelParameter = ShowSlidePanel;
        if (RibbonCollapsed != _collapsedParameter)
            _collapsed = _collapsedParameter = RibbonCollapsed;
        if (RibbonMode != _modeParameter || !IsInitialized)
            _mode = _modeParameter = RibbonMode;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (!_jsStarted)
        {
            _jsStarted = true;
            await StartJsAsync();
        }

        if (!_imagesReady && _presentation != null)
        {
            await RegisterImagesAsync(_presentation.Slides);
            _imagesReady = true;
            StateHasChanged();
        }
        else if (_js != null && _current != null && !ReferenceEquals(_current, _revealed))
        {
            _revealed = _current;
            try { await _js.InvokeVoidAsync("reveal", _current.Key); }
            catch (Exception e) when (e is JSException or JSDisconnectedException) { }
        }
    }

    private async Task StartJsAsync()
    {
        try
        {
            await JsRuntime.InitializeMudBlazorExtensionsCoreAsync();
            _dotnet = DotNetObjectReference.Create(this);
            (_module, _js) = await JsRuntime.ImportModuleAndCreateJsAsync<MudExSlideEditor>(_root, _dotnet);
        }
        catch (Exception e) when (e is JSException or JSDisconnectedException)
        {
            _js = null;
        }
    }

    /// <inheritdoc />
    protected override MudExFileContent CreateNew()
    {
        var stream = new MemoryStream();
        Office.Presentation.Presentation.Create().Save(stream);
        stream.Position = 0;
        return new() { FileName = "Presentation1.pptx", ContentType = PptxContentType, Stream = stream };
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
        var presentation = Office.Presentation.Presentation.Load(buffer);
        Attach(presentation);
        await PresentationChanged.InvokeAsync(presentation);
    }

    /// <inheritdoc />
    public override async Task<MudExFileContent> ExportAsync(MudExFileFormat format = null, CancellationToken cancellationToken = default)
    {
        if (_notes != null)
            await _notes.CommitAsync();
        if (_canvas != null)
            await _canvas.CommitTextAsync();
        var stream = new MemoryStream();
        _presentation.Save(stream);
        stream.Position = 0;
        return new MudExFileContent
        {
            FileName = Path.ChangeExtension(string.IsNullOrEmpty(FileName) ? "Presentation1.pptx" : FileName, Pptx.Extension),
            ContentType = PptxContentType,
            Stream = stream
        };
    }

    private void Attach(Presentation presentation)
    {
        if (ReferenceEquals(presentation, _presentation))
            return;
        if (_presentation != null)
            _presentation.Changed -= OnPresentationChanged;
        _ = RevokeImagesAsync();
        _presentation = presentation;
        _images = presentation == null ? null : new SlideImageUrls(presentation);
        _imagesReady = false;
        _currentIndex = 0;
        _current = presentation?.Slides.FirstOrDefault();
        _selection = _current == null ? [] : [_current];
        if (presentation != null)
            presentation.Changed += OnPresentationChanged;
    }

    private void OnPresentationChanged(object sender, PresentationChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, _presentation))
            return;
        var slides = _presentation.Slides;
        if (_current == null || !slides.Contains(_current))
            Show(slides.Count == 0 ? null : slides[Math.Clamp(_currentIndex, 0, slides.Count - 1)]);
        else
            Show(_current, _selection.Where(slides.Contains).OrderBy(IndexOf).DefaultIfEmpty(_current).ToList());

        _ = InvokeAsync(async () =>
        {
            try
            {
                await RegisterImagesAsync(e.Slides.Where(slides.Contains));
                await NotifyChangedAsync();
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
            await PresentationChanged.InvokeAsync(_presentation);
        }
        finally
        {
            SupressStateChange = false;
        }
    }

    private int IndexOf(Slide slide) => _presentation?.SlideList.IndexOf(slide) ?? -1;

    private void Show(Slide slide, IReadOnlyList<Slide> selection = null)
    {
        _current = slide;
        _currentIndex = slide == null ? 0 : IndexOf(slide);
        _selection = selection ?? (slide == null ? [] : [slide]);
    }

    private async Task RegisterImagesAsync(IEnumerable<Slide> slides)
    {
        if (_js == null || _images == null)
            return;
        var images = _images;
        foreach (var part in slides.SelectMany(ImageParts).Distinct(StringComparer.OrdinalIgnoreCase).Where(p => !images.IsRegistered(p)).ToList())
        {
            if (images.Load(part) is not ({ } data, var contentType))
                continue;
            try
            {
                if (await _js.InvokeAsync<string>("register", data, contentType) is { } url)
                    images.Register(part, url);
            }
            catch (Exception e) when (e is JSException or JSDisconnectedException)
            {
                return;
            }
        }
    }

    private static IEnumerable<string> ImageParts(Slide slide) =>
        SlideShape.Flatten(slide.InheritedShapes.Concat(slide.Shapes)).OfType<PictureShape>().Select(p => p.ImagePart)
            .Append(SlideBackground.EffectiveBackground(slide).ImagePart)
            .Where(p => p != null);

    private async Task RevokeImagesAsync()
    {
        if (_js == null)
            return;
        try { await _js.InvokeVoidAsync("revokeAll"); }
        catch (JSException) { }
        catch (JSDisconnectedException) { }
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        if ((e.CtrlKey || e.MetaKey) && !e.AltKey && e.Key is "s" or "S" && !IsReadOnly)
        {
            await SaveAsync();
            StateHasChanged();
        }
    }

    private Task OnNavigationKeyDownAsync(KeyboardEventArgs e)
    {
        if (_presentation == null || _presentation.Slides.Count == 0 || e.CtrlKey || e.AltKey || e.MetaKey)
            return Task.CompletedTask;
        var last = _presentation.Slides.Count - 1;
        int? target = e.Key switch
        {
            "ArrowDown" or "ArrowRight" or "PageDown" => Math.Min(_currentIndex + 1, last),
            "ArrowUp" or "ArrowLeft" or "PageUp" => Math.Max(_currentIndex - 1, 0),
            "Home" => 0,
            "End" => last,
            _ => null
        };
        if (target is not { } index || index == _currentIndex)
            return Task.CompletedTask;
        Show(_presentation.Slides[index]);
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>Called by the browser when thumbnails in the slide panel scrolled into view.</summary>
    /// <param name="keys">The <see cref="Slide.Key"/> of each visible slide.</param>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public void OnThumbnailsVisible(string[] keys) => _ = InvokeAsync(() => _panel?.MarkVisible(keys));

    /// <summary>Called by the browser when slides were dragged to a new position in the slide panel.</summary>
    /// <param name="from">The index of the dragged slide.</param>
    /// <param name="target">The insertion index, from 0 to the slide count.</param>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnSlidesDropped(int from, int target) => InvokeAsync(async () =>
    {
        await MoveSlidesAsync(from, target);
        StateHasChanged();
    });

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (_presentation != null)
            _presentation.Changed -= OnPresentationChanged;
        if (_js != null)
        {
            try { await _js.InvokeVoidAsync("dispose"); } catch { }
            try { await _js.DisposeAsync(); } catch { }
        }

        if (_module != null)
            try { await _module.DisposeAsync(); } catch { }
        _dotnet?.Dispose();
        await base.DisposeAsync();
    }
}
