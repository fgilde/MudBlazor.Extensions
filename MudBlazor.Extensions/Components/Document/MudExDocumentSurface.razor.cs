using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The editing surface of the document editor: a content editable sheet that the browser renders from the <see cref="TextDocument"/> and keeps in
/// sync with it. Typing inside a paragraph is applied by the browser and reported in batches; everything structural (Enter, Backspace at a paragraph
/// start, Tab, undo, shortcuts) is executed as a command on the document. Changes made to the document by other code are shown as they happen.
/// </summary>
public partial class MudExDocumentSurface
{
    private const int MaxText = 8000;
    private const int MaxTyped = 10_000;
    private const long MaxTypedChars = 8L * 1024 * 1024;
    private static readonly SurfaceResult Empty = new([], null, null);

    private readonly JsModule<MudExDocumentSurface> _module = new();
    private readonly Dictionary<string, int> _changedAt = new();
    private readonly HashSet<string> _registered = [];
    private ElementReference _root;
    private TextDocument _document;
    private DocumentSurfaceRender _render;
    private DocumentRange _selection;
    private bool _live;
    private int _seq;
    private int _attachedAt;
    private List<SurfacePatch> _collecting;
    private bool _acting;
    private DocumentRange? _resultSelection;
    private (string Token, RunFormat Format, string Css)? _caretFormat;
    private int _tokens;
    private bool _readOnly;
    private double _zoom;
    private IReadOnlyCollection<string> _shortcuts;

    /// <summary>The document to show and edit.</summary>
    [Parameter] public TextDocument Document { get; set; }

    /// <summary>If true the document cannot be changed; selecting, copying and shortcuts stay available.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>The zoom factor; 1 is 100 %.</summary>
    [Parameter] public double Zoom { get; set; } = 1;

    /// <summary>Raised when <see cref="Selection"/> changes.</summary>
    [Parameter] public EventCallback<DocumentRange> SelectionChanged { get; set; }

    /// <summary>The keys the surface hands to <see cref="ShortcutPressed"/> instead of the browser, for example <c>Ctrl+B</c>.</summary>
    [Parameter] public IReadOnlyCollection<string> Shortcuts { get; set; }

    /// <summary>Called with the key after one of the <see cref="Shortcuts"/> was pressed and the typing before it was applied.</summary>
    [Parameter] public Func<string, Task> ShortcutPressed { get; set; }

    /// <summary>Raised when an edit could not be applied to the document. Without a handler the error is shown in a dialog.</summary>
    [Parameter] public EventCallback<Exception> OnError { get; set; }

    /// <summary>The selection, or the caret as an empty range. It always points into paragraphs of the body.</summary>
    public DocumentRange Selection => _selection;

    internal RunFormat CaretFormat => _caretFormat?.Format;

    internal TimeProvider Clock { get; set; } = TimeProvider.System;

    private double ZoomFactor => double.IsFinite(Zoom) && Zoom > 0 ? Zoom : 1;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (!ReferenceEquals(Document, _document))
        {
            Attach();
            if (_module.Js != null) await SendInitialAsync();
        }

        if (_module.Js == null) return;
        if (ReadOnly != _readOnly) await CallAsync("setReadOnly", _readOnly = ReadOnly);
        if (ZoomFactor != _zoom) await CallAsync("setZoom", _zoom = ZoomFactor);
        if (!ReferenceEquals(Shortcuts, _shortcuts)) await CallAsync("setShortcuts", (_shortcuts = Shortcuts) ?? []);
    }

    /// <inheritdoc />
    protected override bool ShouldRender() => false;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (!firstRender) return;
        (_readOnly, _zoom, _shortcuts) = (ReadOnly, ZoomFactor, Shortcuts);
        var options = new { ReadOnly = _readOnly, Zoom = _zoom, Shortcuts = _shortcuts ?? [] };
        if (await _module.StartAsync(JsRuntime, this, "MudExDocumentSurface", dotnet => [_root, dotnet, options]) != null) await SendInitialAsync();
    }

    internal async Task RunAsync(Action<DocumentRange> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SurfaceSync sync = null;
        if (_module.Js is { } js)
            try { sync = await js.InvokeAsync<SurfaceSync>("suspend"); }
            catch (Exception e) when (e is JSException or JSDisconnectedException or TaskCanceledException or ObjectDisposedException) { }
        var result = await HandleAsync(WithinCaps(sync) ? sync : null, () => action(_selection));
        await CallAsync("resume", result);
    }

    internal void SetCaretFormat(RunFormat format) =>
        _caretFormat = format == null || _document == null
            ? null
            : ($"f{++_tokens}", format, Point(_selection.End) is { } caret ? _render.RunCss((Paragraph)_document.FindBlock(caret.P), format) : "");

    internal Task FlushAsync() => RunAsync(_ => { });

    internal async Task SelectAsync(DocumentRange range, bool scroll = true)
    {
        if (_document == null || ToSurface(range) is not { } selection) return;
        var changed = Select(ToRange(selection).Value);
        await CallAsync("select", selection, scroll);
        if (changed) await SelectionChanged.InvokeAsync(_selection);
    }

    internal Task FocusAsync() => CallAsync("focus");

    /// <summary>Called by the browser with the typing and the selection since the last message.</summary>
    /// <param name="sync">The state of the browser, or null if it comes as <paramref name="stream"/>.</param>
    /// <param name="stream">The state as JSON when it is too large for one message.</param>
    /// <returns>The patches to apply.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task<SurfaceResult> OnSyncAsync(SurfaceSync sync, IJSStreamReference stream) =>
        await ReadAsync(sync, stream) is { } read ? await HandleAsync(read, null) : Empty;

    /// <summary>Called by the browser for a structural edit, undo, redo or a shortcut, with the typing and the selection before it.</summary>
    /// <param name="sync">The state of the browser, or null if it comes as <paramref name="stream"/>.</param>
    /// <param name="stream">The state as JSON when it is too large for one message.</param>
    /// <param name="name">The edit: an edit action name, <c>undo</c>, <c>redo</c> or <c>shortcut</c>.</param>
    /// <param name="text">The text of a <c>replace</c> or the key of a <c>shortcut</c>; ignored if longer than 8000 characters.</param>
    /// <returns>The patches to apply and the selection to set.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task<SurfaceResult> OnCommandAsync(SurfaceSync sync, IJSStreamReference stream, string name, string text)
    {
        if (await ReadAsync(sync, stream) is not { } read) return Empty;
        var result = await HandleAsync(read, text?.Length > MaxText || ReadOnly ? null : name switch
        {
            "undo" => () => Step(true),
            "redo" => () => Step(false),
            "shortcut" => null,
            _ => () => Execute(name, text)
        });
        if (name == "shortcut" && text?.Length <= MaxText && ShortcutPressed != null && Shortcuts?.Contains(text) == true) _ = ShortcutAsync(text);
        return result;
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (_document != null) _document.Changed -= OnChanged;
        await _module.DisposeAsync();
        await base.DisposeAsync();
    }

    private void Attach()
    {
        if (_document != null) _document.Changed -= OnChanged;
        _document = Document;
        _render = _document == null ? null : new DocumentSurfaceRender(_document);
        (_live, _caretFormat) = (false, null);
        _changedAt.Clear();
        _registered.Clear();
        _selection = _document?.AllParagraphs().FirstOrDefault() is { } first ? Caret(_document.PositionOf(first.Id, 0)) : default;
        if (_document != null) _document.Changed += OnChanged;
    }

    private async Task SendInitialAsync()
    {
        if (_document == null) return;
        var patches = DocumentSurfacePatches.Initial(_document, _render, () => ++_seq);
        (_live, _attachedAt) = (true, patches[0].Seq);
        foreach (var patch in patches) Track(patch);
        await RegisterAsync(_render.TakeMedia());
        foreach (var patch in patches) await CallAsync("patch", (object)new[] { patch });
    }

    private void OnChanged(object sender, TextDocumentChangedEventArgs change)
    {
        if (!_live || !ReferenceEquals(sender, _document)) return;
        var patches = DocumentSurfacePatches.Changes(_document, _render, change, () => ++_seq);
        foreach (var patch in patches) Track(patch, change);
        if (_acting && change.Selection != null) _resultSelection = change.Selection;
        if (_collecting != null)
            _collecting.AddRange(patches);
        else
            _ = PushAsync(_render.TakeMedia(), patches);
    }

    private async Task PushAsync(IReadOnlyCollection<string> media, IReadOnlyList<SurfacePatch> patches)
    {
        await RegisterAsync(media);
        foreach (var patch in patches) await CallAsync("patch", (object)new[] { patch });
    }

    private Task RegisterAsync(IEnumerable<string> media) =>
        Task.WhenAll(media.Where(_registered.Add).ToList().Select(part => CallAsync("registerMedia", part, _document.GetMedia(part), _document.MediaContentType(part))));

    private SurfacePatch Track(SurfacePatch patch, TextDocumentChangedEventArgs change = null)
    {
        foreach (var id in change == null ? DocumentSurfacePatches.Ids(patch) : DocumentSurfacePatches.Ids(patch, change)) _changedAt[id] = patch.Seq;
        return patch;
    }

    private static async Task<SurfaceSync> ReadAsync(SurfaceSync sync, IJSStreamReference stream)
    {
        try { sync = await JsPayload.ReadAsync(sync, stream); }
        catch (Exception) { return null; }
        return WithinCaps(sync) ? sync : null;
    }

    private static bool WithinCaps(SurfaceSync sync) =>
        sync?.Typed is not { } typed || typed.Count <= MaxTyped && typed.Sum(t => (long)(t?.T?.Length ?? 0)) <= MaxTypedChars;

    private async Task<SurfaceResult> HandleAsync(SurfaceSync sync, Action action)
    {
        if (_document == null) return Empty;
        var before = _selection;
        Exception error = null;
        _collecting = [];
        try
        {
            if (sync != null) Apply(sync);
            _acting = true;
            action?.Invoke();
        }
        catch (Exception e)
        {
            error = e;
        }

        var patches = _collecting;
        var selection = _resultSelection is { } moved ? ToSurface(moved) : null;
        (_collecting, _acting, _resultSelection) = (null, false, null);
        if (selection != null) Select(ToRange(selection).Value);
        var result = new SurfaceResult(patches, selection, _caretFormat is { } caret ? new SurfaceCaretFormat(caret.Token, caret.Css) : null);
        await RegisterAsync(_render.TakeMedia());
        if (error != null) _ = ReportAsync(error);
        if (!Equals(before, _selection)) await SelectionChanged.InvokeAsync(_selection);
        return result;
    }

    private void Apply(SurfaceSync sync)
    {
        var typed = sync.Typed ?? [];
        var last = new Dictionary<string, int>();
        for (var i = 0; i < typed.Count; i++)
            if (typed[i] is { P: not null, T: not null } entry) last[entry.P] = i;
        var accepted = new List<TypedParagraph>();
        var refresh = new List<string>();
        for (var i = 0; i < typed.Count; i++)
        {
            var entry = typed[i];
            if (entry?.P == null || entry.T == null || last[entry.P] != i || BodyParagraph(entry.P) == null) continue;
            if (ReadOnly || !_changedAt.TryGetValue(entry.P, out var at) || at > sync.Seen)
                refresh.Add(entry.P);
            else
                accepted.Add(new TypedParagraph(entry.P, entry.T, entry.C is { } c ? Math.Clamp(c, 0, entry.T.Length) : null, Token(entry.F)));
        }

        if (accepted.Count > 0) refresh.AddRange(_document.ApplyTyping(accepted, Clock.GetUtcNow()));
        var rendered = _collecting.SelectMany(DocumentSurfacePatches.Ids).ToHashSet();
        refresh.RemoveAll(rendered.Contains);
        if (refresh.Count > 0 && _live) _collecting.Add(Track(DocumentSurfacePatches.Refresh(_document, _render, refresh, ++_seq)));
        if (sync.Seen >= _attachedAt && ToRange(sync.Selection) is { } selection) Select(selection);
        if (sync.EndTyping) _document.EndTyping();
    }

    private RunFormat Token(string token) => token != null && _caretFormat is { } caret && caret.Token == token ? caret.Format : null;

    private void Step(bool undo)
    {
        _document.EndTyping();
        if (undo && _document.Undo.CanUndo) _document.Undo.Undo();
        else if (!undo && _document.Undo.CanRedo) _document.Undo.Redo();
    }

    private void Execute(string name, string text)
    {
        if (_selection.Start != null && DocumentEditActions.For(_document, name, _selection, text) is { } command) _document.Execute(command, _selection);
    }

    private async Task ShortcutAsync(string key)
    {
        try { await ShortcutPressed(key); }
        catch (Exception e) { await ReportAsync(e); }
    }

    private async Task ReportAsync(Exception error)
    {
        try
        {
            if (OnError.HasDelegate)
                await OnError.InvokeAsync(error);
            else
                await DialogService.ShowInformationAsync(TryLocalize("Error"), error.Message, Icons.Material.Filled.Error);
        }
        catch (Exception) { }
    }

    private bool Select(DocumentRange range)
    {
        if (Equals(range, _selection)) return false;
        (_selection, _caretFormat) = (range, null);
        return true;
    }

    private Paragraph BodyParagraph(string id)
    {
        if (_document.FindBlock(id) is not Paragraph paragraph) return null;
        var top = _document.PositionOf(id, 0).BlockId;
        var index = _document.BlockIndex(top);
        return index >= 0 && index < _document.Blocks.Count && _document.Blocks[index].Id == top ? paragraph : null;
    }

    private SurfacePoint Point(DocumentPosition position)
    {
        if (position == null) return null;
        Paragraph paragraph;
        try { paragraph = _document.Resolve(position with { Offset = 0 }).Paragraph; }
        catch (ArgumentException) { return null; }
        return BodyParagraph(paragraph.Id) == null ? null : new SurfacePoint(paragraph.Id, Math.Clamp(position.Offset, 0, paragraph.Length));
    }

    private SurfaceSelection ToSurface(DocumentRange range) => Point(range.Start) is { } anchor && Point(range.End) is { } focus ? new SurfaceSelection(anchor, focus) : null;

    private DocumentPosition Position(SurfacePoint point) =>
        point?.P != null && BodyParagraph(point.P) is { } paragraph ? _document.PositionOf(point.P, Math.Clamp(point.O, 0, paragraph.Length)) : null;

    private DocumentRange? ToRange(SurfaceSelection selection) =>
        selection != null && Position(selection.Anchor) is { } anchor && Position(selection.Focus) is { } focus ? new DocumentRange(anchor, focus) : null;

    private static DocumentRange Caret(DocumentPosition position) => new(position, position);

    private async Task CallAsync(string method, params object[] arguments)
    {
        if (_module.Js is not { } js) return;
        try { await js.InvokeVoidAsync(method, arguments); }
        catch (Exception e) when (e is JSException or JSDisconnectedException or TaskCanceledException or ObjectDisposedException) { }
    }
}
