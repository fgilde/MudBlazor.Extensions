using System.ComponentModel;
using System.Runtime.ExceptionServices;
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
    private const int MaxTyped = SurfaceTypedListConverter.MaxTyped;
    private const long MaxTypedChars = 8L * 1024 * 1024;
    private const long MaxSyncBytes = MaxTypedChars * 4 + 1024 * 1024;
    private const int InitialChunk = 500;
    private static readonly SurfaceResult Empty = new([], null, null);
    private static readonly SurfaceResult Unreadable = new([], null, null, true);

    private readonly JsModule<MudExDocumentSurface> _module = new();
    private readonly Dictionary<string, int> _changedAt = new();
    private readonly HashSet<string> _registered = [];
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly DocumentClipboard _clipboard = new();
    private ElementReference _root;
    private TextDocument _document;
    private DocumentSurfaceRender _render;
    private DocumentRange _selection;
    private bool _live;
    private int _seq;
    private int _attachedAt;
    private int _generation;
    private bool _chunking;
    private bool _restart;
    private List<SurfacePatch> _collecting;
    private bool _acting;
    private bool _inAction;
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

    /// <summary>
    /// The selection, or the caret as an empty range: the last selection the surface reported or a command set. It points into paragraphs of the
    /// body when it is set, but may name a block that a later direct edit of the host removed.
    /// </summary>
    public DocumentRange Selection => _selection;

    internal RunFormat CaretFormat => _caretFormat?.Format;

    internal TimeProvider Clock { get; set; } = TimeProvider.System;

    internal Func<Task> Pause { get; set; } = YieldAsync;

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

        if (_module.Js != null) await SendOptionsAsync();
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
        if (await _module.StartAsync(JsRuntime, this, "MudExDocumentSurface", dotnet => [_root, dotnet, options]) == null) return;
        await SendInitialAsync();
        await SendOptionsAsync();
    }

    /// <summary>
    /// Runs an edit of the host (ribbon, dialog, find panel) on the document: suspends the browser, applies its pending typing and selection, runs
    /// <paramref name="action"/> with the selection and resumes the browser with the resulting patches and selection. Calls are serialised; calling
    /// this from inside an <paramref name="action"/> throws. The caller is responsible for honouring <see cref="ReadOnly"/>. If <see cref="Document"/>
    /// is replaced while the browser is being suspended, the action is not run and the browser is resumed with an empty result.
    /// </summary>
    /// <param name="action">The edit; it gets the current selection.</param>
    /// <returns>
    /// A task that completes when the browser was resumed; it fails with the exception of the action, which is also reported through
    /// <see cref="OnError"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Called from inside an action.</exception>
    internal Task RunAsync(Action<DocumentRange> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ThrowIfInAction();
        return OperateAsync(() => action(_selection), true);
    }

    internal void SetCaretFormat(RunFormat format) =>
        _caretFormat = format == null || _document == null
            ? null
            : ($"f{++_tokens}", format, Point(_selection.End) is { } caret ? _render.RunCss((Paragraph)_document.FindBlock(caret.P), format) : "");

    internal Task FlushAsync() => RunAsync(_ => { });

    internal Task SelectAsync(DocumentRange range, bool scroll = true)
    {
        ThrowIfInAction();
        return OperateAsync(() => _resultSelection = range, scroll);
    }

    internal Task HighlightAsync(IReadOnlyList<DocumentRange> ranges) =>
        _document == null ? Task.CompletedTask : CallAsync("highlight", ranges.Take(1000).Select(ToSurface).Where(r => r != null).ToList());

    internal Task FocusAsync() => CallAsync("focus");

    internal Task CopyAsync(bool cut) => CallAsync("copy", cut);

    internal Task PasteAsync() => CallAsync("paste");

    /// <summary>Called by the browser with the typing and the selection since the last message.</summary>
    /// <param name="sync">The state of the browser, or null if it comes as <paramref name="stream"/>.</param>
    /// <param name="stream">The state as JSON when it is too large for one message.</param>
    /// <returns>The patches to apply.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task<SurfaceResult> OnSyncAsync(SurfaceSync sync, IJSStreamReference stream)
    {
        var (read, rejected, unreadable) = await ReadAsync(sync, stream);
        if (unreadable) return Unreadable;
        return Current(read) ? await HandleAsync(read, rejected, null, false) : Empty;
    }

    /// <summary>Called by the browser for a structural edit, undo, redo or a shortcut, with the typing and the selection before it.</summary>
    /// <param name="sync">The state of the browser, or null if it comes as <paramref name="stream"/>.</param>
    /// <param name="stream">The state as JSON when it is too large for one message.</param>
    /// <param name="name">The edit: an edit action name, <c>undo</c>, <c>redo</c>, <c>shortcut</c>, or <c>resync</c> to render the whole document again.</param>
    /// <param name="text">The text of a <c>replace</c> or the key of a <c>shortcut</c>; ignored if longer than 8000 characters.</param>
    /// <returns>The patches to apply and the selection to set.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task<SurfaceResult> OnCommandAsync(SurfaceSync sync, IJSStreamReference stream, string name, string text)
    {
        var (read, rejected, unreadable) = await ReadAsync(sync, stream);
        if (unreadable) return Unreadable;
        if (!Current(read)) return Empty;
        var result = await HandleAsync(read, rejected, name == "resync" ? Resync : rejected || text?.Length > MaxText || ReadOnly ? null : name switch
        {
            "undo" => () => Step(true),
            "redo" => () => Step(false),
            "shortcut" => null,
            _ => () => Execute(name, text)
        }, name is not ("undo" or "redo" or "shortcut" or "resync"));
        if (!rejected && name == "shortcut" && text?.Length <= MaxText && ShortcutPressed != null && Shortcuts?.Contains(text) == true) _ = ShortcutAsync(text);
        return result;
    }

    /// <summary>Called by the browser after a copy or cut with the typing and the selection before it; the clip is remembered for pasting inside the editor.</summary>
    /// <param name="sync">The state of the browser.</param>
    /// <param name="clip">The marker the browser put into the clipboard.</param>
    /// <param name="cut">True to delete the selection as well.</param>
    /// <returns>The patches to apply and the selection to set.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task<SurfaceResult> OnCopyAsync(SurfaceSync sync, string clip, bool cut)
    {
        var (read, rejected, unreadable) = await ReadAsync(sync, null);
        if (unreadable) return Unreadable;
        if (!Current(read)) return Empty;
        return await HandleAsync(read, rejected, rejected || cut && ReadOnly ? null : () => Copy(clip, cut), true);
    }

    /// <summary>Called by the browser on paste with the typing and the selection before it and the clipboard content.</summary>
    /// <param name="sync">The state of the browser.</param>
    /// <param name="html">The <c>text/html</c> part as UTF-8, or null.</param>
    /// <param name="text">The <c>text/plain</c> part as UTF-8, or null.</param>
    /// <param name="image">The first image of the clipboard, or null.</param>
    /// <param name="imageType">The content type of <paramref name="image"/>.</param>
    /// <param name="clip">The marker found in the <c>text/html</c> part, or null.</param>
    /// <returns>The patches to apply and the selection to set.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task<SurfaceResult> OnPasteAsync(SurfaceSync sync, IJSStreamReference html, IJSStreamReference text, IJSStreamReference image, string imageType, string clip)
    {
        var (read, rejected, unreadable) = await ReadAsync(sync, null);
        if (unreadable || !Current(read) || rejected || ReadOnly)
        {
            foreach (var stream in new[] { html, text, image }) await DisposeQuietlyAsync(stream);
            return unreadable ? Unreadable : !Current(read) ? Empty : await HandleAsync(read, rejected, null, true);
        }

        (string Html, string Text, byte[] Image) data = default;
        Exception failure = null;
        try
        {
            data = (_clipboard.Holds(clip) ? null : await DocumentClipboard.ReadTextAsync(html), await DocumentClipboard.ReadTextAsync(text), await DocumentClipboard.ReadBytesAsync(image));
        }
        catch (Exception e)
        {
            failure = e is InvalidDataException ? e : new InvalidDataException(DocumentClipboard.Unreadable);
            foreach (var stream in new[] { html, text, image }) await DisposeQuietlyAsync(stream);
        }

        return await HandleAsync(read, false, () => PasteInto(failure, data.Html, data.Text, data.Image, imageType, clip), true);
    }

    private void Copy(string clip, bool cut)
    {
        if (_selection.Start == null || _selection.IsEmpty) return;
        _clipboard.Store(clip, _document.Extract(_selection));
        if (cut) _document.Execute(new ReplaceTextCommand(_selection, ""), _selection);
    }

    private void PasteInto(Exception failure, string html, string text, byte[] image, string imageType, string clip)
    {
        if (failure != null) throw failure;
        if (_selection.Start != null && _clipboard.Paste(_document, _selection, html, text, image, imageType, clip) is { } command) _document.Execute(command, _selection);
    }

    private static async Task DisposeQuietlyAsync(IJSStreamReference stream)
    {
        try
        {
            if (stream != null) await stream.DisposeAsync();
        }
        catch (Exception) { }
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        (_generation, _chunking) = (_generation + 1, false);
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
        var generation = ++_generation;
        _chunking = false;
        if (_document == null)
        {
            var reset = new SurfacePatch(++_seq, true, null, [], []);
            _attachedAt = reset.Seq;
            await SendPatchesAsync([reset]);
            return;
        }

        _live = true;
        for (var (from, restarted) = (0, false); ;)
        {
            if (from == 0) _restart = false;
            var patch = DocumentSurfacePatches.Chunk(_document, _render, from, ++_seq, InitialChunk);
            foreach (var id in DocumentSurfacePatches.Ids(patch))
                if (!restarted || !_changedAt.ContainsKey(id)) _changedAt[id] = patch.Seq;
            if (from == 0 && !restarted) _attachedAt = patch.Seq;
            from += InitialChunk;
            _chunking = from < _document.Blocks.Count;
            await RegisterQuietlyAsync(_render.TakeMedia());
            await SendPatchesAsync([patch]);
            if (generation != _generation || !_chunking && !_restart) return;
            await Pause();
            if (generation != _generation) return;
            if (!_restart) continue;
            await FlushQuietlyAsync();
            if (generation != _generation) return;
            (from, restarted) = (0, true);
        }
    }

    private async Task FlushQuietlyAsync()
    {
        try { await FlushAsync(); }
        catch (Exception) { }
    }

    private static async Task YieldAsync()
    {
        if (OperatingSystem.IsBrowser()) await Task.Delay(1);
        else await Task.Yield();
    }

    private async Task SendOptionsAsync()
    {
        if (ReadOnly != _readOnly) await CallAsync("setReadOnly", _readOnly = ReadOnly);
        if (ZoomFactor != _zoom) await CallAsync("setZoom", _zoom = ZoomFactor);
        if (!ReferenceEquals(Shortcuts, _shortcuts)) await CallAsync("setShortcuts", (_shortcuts = Shortcuts) ?? []);
    }

    private void OnChanged(object sender, TextDocumentChangedEventArgs change)
    {
        if (!_live || !ReferenceEquals(sender, _document)) return;
        if (_acting && change.Selection != null) _resultSelection = change.Selection;
        var kind = change.Kind;
        _restart |= _chunking && ((kind & (DocumentChangeKind.Styles | DocumentChangeKind.Sections)) != 0 || kind.HasFlag(DocumentChangeKind.Structure | DocumentChangeKind.Content));
        if (_chunking && kind.HasFlag(DocumentChangeKind.Styles))
        {
            foreach (var id in change.RemovedBlockIds.Concat(change.RemovedNestedBlockIds)) _render.Forget(id);
            foreach (var id in change.ChangedBlockIds.Concat(change.ChangedNestedBlockIds).Concat(change.RemovedBlockIds).Concat(change.RemovedNestedBlockIds)) _changedAt.Remove(id);
            return;
        }

        var patches = DocumentSurfacePatches.Changes(_document, _render, change, () => ++_seq);
        foreach (var patch in patches) Track(patch, change);
        if (_collecting != null)
            _collecting.AddRange(patches);
        else
            _ = PushAsync(_render.TakeMedia(), patches);
    }

    private async Task PushAsync(IReadOnlyCollection<string> media, IReadOnlyList<SurfacePatch> patches)
    {
        await RegisterQuietlyAsync(media);
        try { await SendPatchesAsync(patches); }
        catch (Exception e) { await ReportAsync(e); }
    }

    private async Task SendPatchesAsync(IEnumerable<SurfacePatch> patches)
    {
        foreach (var patch in patches) await CallAsync("patch", (object)new[] { patch });
    }

    private async Task RegisterQuietlyAsync(IReadOnlyCollection<string> media)
    {
        try { await RegisterAsync(media); }
        catch (Exception e) { _ = ReportAsync(e); }
    }

    private Task RegisterAsync(IEnumerable<string> media) =>
        Task.WhenAll(media.Where(_registered.Add).ToList().Select(part => CallAsync("registerMedia", part, _document.GetMedia(part), _document.MediaContentType(part))));

    private SurfacePatch Track(SurfacePatch patch, TextDocumentChangedEventArgs change = null)
    {
        foreach (var id in change == null ? DocumentSurfacePatches.Ids(patch) : DocumentSurfacePatches.Ids(patch, change)) _changedAt[id] = patch.Seq;
        return patch;
    }

    private static async Task<(SurfaceSync Sync, bool Rejected, bool Unreadable)> ReadAsync(SurfaceSync sync, IJSStreamReference stream)
    {
        try { sync = await JsPayload.ReadAsync(sync, stream, MaxSyncBytes); }
        catch (Exception) { return (null, true, true); }
        return (sync, !WithinCaps(sync), false);
    }

    private bool Current(SurfaceSync sync) => sync != null && sync.Seen >= _attachedAt;

    private static bool WithinCaps(SurfaceSync sync) =>
        sync?.Typed is not { } typed || typed.Count <= MaxTyped && typed.Sum(t => (long)(t?.T?.Length ?? 0)) <= MaxTypedChars;

    private void ThrowIfInAction()
    {
        if (_inAction) throw new InvalidOperationException("The document surface cannot start an operation from inside another operation.");
    }

    private async Task OperateAsync(Action action, bool scroll)
    {
        if (_collecting != null) await Task.Yield();
        await _gate.WaitAsync();
        (SurfaceResult Result, Exception Error, bool Moved) outcome = (Empty, null, false);
        Exception failed = null;
        try
        {
            var document = _document;
            var suspended = false;
            SurfaceSync sync = null;
            if (_module.Js is { } js)
            {
                try
                {
                    sync = await js.InvokeAsync<SurfaceSync>("suspend");
                    suspended = true;
                }
                catch (Exception e)
                {
                    failed = e;
                }
            }

            try
            {
                if (ReferenceEquals(document, _document))
                {
                    sync = Current(sync) ? sync : null;
                    outcome = Operate(sync, !WithinCaps(sync), action, false, true);
                    await RegisterQuietlyAsync(_render?.TakeMedia() ?? []);
                }
            }
            finally
            {
                if (!suspended || !await CallAsync("resume", outcome.Result, scroll))
                    await SendPatchesAsync(outcome.Result.Patches);
            }
        }
        finally
        {
            _gate.Release();
        }

        if (outcome.Moved) _ = SelectionChangedAsync(_selection);
        if (failed != null) _ = ReportAsync(failed);
        if (outcome.Error == null) return;
        _ = ReportAsync(outcome.Error);
        ExceptionDispatchInfo.Capture(outcome.Error).Throw();
    }

    private async Task<SurfaceResult> HandleAsync(SurfaceSync sync, bool rejected, Action action, bool guarded)
    {
        var (result, error, moved) = Operate(sync, rejected, action, guarded, false);
        await RegisterQuietlyAsync(_render?.TakeMedia() ?? []);
        if (error != null) _ = ReportAsync(error);
        if (moved) _ = SelectionChangedAsync(_selection);
        return result;
    }

    private (SurfaceResult Result, Exception Error, bool Moved) Operate(SurfaceSync sync, bool rejected, Action action, bool guarded, bool gated)
    {
        if (_document == null) return (Empty, null, false);
        var before = _selection;
        Exception error = null;
        (_collecting, _resultSelection) = ([], null);
        try
        {
            var proceed = true;
            if (sync != null && rejected)
                Resend(sync);
            else if (sync != null)
                proceed = Apply(sync, guarded, out error);
            if (proceed && action != null)
            {
                (_acting, _inAction) = (true, gated);
                action();
            }
        }
        catch (Exception e)
        {
            error = e;
        }
        finally
        {
            (_acting, _inAction) = (false, false);
        }

        var patches = _collecting;
        var selection = _resultSelection is { } moved ? ToSurface(moved) : null;
        (_collecting, _resultSelection) = (null, null);
        if (selection != null) Select(ToRange(selection).Value);
        var caretFormat = _caretFormat is { } caret ? new SurfaceCaretFormat(caret.Token, caret.Css) : null;
        return (new SurfaceResult(patches, selection, caretFormat), error, !Equals(before, _selection));
    }

    private void Resend(SurfaceSync sync) =>
        Refresh((sync.Typed ?? []).Select(t => t?.P).Where(id => id != null).Distinct().Where(id => BodyParagraph(id) != null).Take(MaxTyped).ToList());

    private bool Apply(SurfaceSync sync, bool guarded, out Exception error)
    {
        error = null;
        var typed = sync.Typed ?? [];
        var last = new Dictionary<string, int>();
        for (var i = 0; i < typed.Count; i++)
            if (typed[i] is { P: not null, T: not null } entry) last[entry.P] = i;
        List<string> stale = guarded ? new[] { sync.Selection?.Anchor?.P, sync.Selection?.Focus?.P }.Where(id => Changed(id, sync.Seen)).Distinct().ToList() : [];
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

        if (accepted.Count > 0)
        {
            try { _document.ApplyTyping(accepted, Clock.GetUtcNow()); }
            catch (Exception e) { error = e; }
            refresh.AddRange(accepted.Select(p => p.ParagraphId));
        }

        Refresh([.. refresh, .. stale]);
        if (error != null) return false;
        var selection = ToRange(sync.Selection);
        var refused = guarded && (stale.Count > 0 || selection == null);
        if (!refused && selection != null) Select(selection.Value);
        if (sync.EndTyping) _document.EndTyping();
        return !refused;
    }

    private bool Changed(string id, int seen) => id != null && _changedAt.TryGetValue(id, out var at) && at > seen;

    private void Refresh(List<string> ids)
    {
        var rendered = _collecting.SelectMany(DocumentSurfacePatches.Ids).ToHashSet();
        ids.RemoveAll(rendered.Contains);
        if (ids.Count > 0 && _live) _collecting.Add(Track(DocumentSurfacePatches.Refresh(_document, _render, ids, ++_seq)));
    }

    private async Task SelectionChangedAsync(DocumentRange selection)
    {
        try { await SelectionChanged.InvokeAsync(selection); }
        catch (Exception e) { await ReportAsync(e); }
    }

    private RunFormat Token(string token) => token != null && _caretFormat is { } caret && caret.Token == token ? caret.Format : null;

    private void Step(bool undo)
    {
        _document.EndTyping();
        if (undo && _document.Undo.CanUndo) _document.Undo.Undo();
        else if (!undo && _document.Undo.CanRedo) _document.Undo.Redo();
    }

    private void Resync()
    {
        (_generation, _chunking, _restart) = (_generation + 1, false, false);
        _changedAt.Clear();
        var patches = DocumentSurfacePatches.Initial(_document, _render, () => ++_seq);
        _attachedAt = patches[0].Seq;
        foreach (var patch in patches) _collecting.Add(Track(patch));
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

    private async Task<bool> CallAsync(string method, params object[] arguments)
    {
        if (_module.Js is not { } js) return false;
        try
        {
            await js.InvokeVoidAsync(method, arguments);
            return true;
        }
        catch (Exception e) when (e is JSException or JSDisconnectedException or TaskCanceledException or ObjectDisposedException or InvalidOperationException)
        {
            return false;
        }
    }
}
