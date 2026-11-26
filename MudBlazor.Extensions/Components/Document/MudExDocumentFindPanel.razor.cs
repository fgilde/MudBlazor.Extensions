using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

/// <summary>Find and replace panel of the <see cref="MudExDocumentEditor"/>.</summary>
public partial class MudExDocumentFindPanel
{
    private ElementReference _input;
    private IReadOnlyList<DocumentRange> _ranges = [];
    private TextDocument _searched;
    private (int Version, string Query, bool Case, bool Word, bool Regex) _key;
    private bool _truncated;
    private bool _invalid;
    private string _message;
    private string _find = "";
    private string _with = "";
    private bool _matchCase;
    private bool _wholeWord;
    private bool _regex;
    private bool _replaceParameter;
    private bool _showReplace;
    private int _index = -1;

    /// <summary>The editor whose document is searched.</summary>
    [Parameter] public MudExDocumentEditor Editor { get; set; }

    /// <summary>True to show the replace row (not for a read-only editor).</summary>
    [Parameter] public bool Replace { get; set; }

    /// <summary>Raised when the user closes the panel.</summary>
    [Parameter] public EventCallback Closed { get; set; }

    private bool ShowReplace => _showReplace && Editor?.Locked == false;

    private FindOptions Options => new(_matchCase, _wholeWord, _regex, false);

    private TextDocument Document => Editor?.CurrentDocument;

    private string Status => _invalid ? TryLocalize("Invalid regular expression.")
        : _message ?? (_find.Length == 0 ? "" : _truncated ? TryLocalize("The search took too long; results are incomplete.")
        : _ranges.Count == 0 ? TryLocalize("No results") : TryLocalize("{0} of {1}", _index + 1, _ranges.Count));

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Replace != _replaceParameter || !IsInitialized)
            _showReplace = _replaceParameter = Replace;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
            await FocusInputAsync();
    }

    private async Task FocusInputAsync()
    {
        try { await _input.FocusAsync(); }
        catch (Exception e) when (e is JSException or InvalidOperationException or TaskCanceledException) { }
    }

    private bool Refresh()
    {
        var document = Document;
        if (document == null)
        {
            (_ranges, _index, _truncated, _invalid) = ([], -1, false, false);
            return false;
        }

        var key = (document.Version, _find, _matchCase, _wholeWord, _regex);
        if (ReferenceEquals(document, _searched) && key == _key)
            return _ranges.Count > 0;
        (_searched, _key, _message, _index) = (document, key, null, -1);
        try
        {
            var result = DocumentSearch.Find(document, _find, Options);
            (_ranges, _truncated, _invalid) = (result.Ranges, result.Truncated, false);
        }
        catch (ArgumentException)
        {
            (_ranges, _truncated, _invalid) = ([], false, true);
        }

        return _ranges.Count > 0;
    }

    private int Following(DocumentPosition position)
    {
        var index = _ranges.ToList().FindIndex(r => Document.Compare(r.Start, position) >= 0);
        return index < 0 ? 0 : index;
    }

    private int Preceding(DocumentPosition position)
    {
        var index = _ranges.ToList().FindLastIndex(r => Document.Compare(r.Start, position) < 0);
        return index < 0 ? _ranges.Count - 1 : index;
    }

    private bool OnMatch(DocumentRange selection, out int index)
    {
        var document = Document;
        index = -1;
        var range = selection.Normalize(document);
        if (range.Start == null || range.End == null)
            return false;
        index = _ranges.ToList().FindIndex(r => document.Compare(r.Start, range.Start) == 0 && document.Compare(r.End, range.End) == 0);
        return index >= 0;
    }

    private async Task ShowAsync(int index)
    {
        _index = index;
        await Editor.SurfaceRef.SelectAsync(_ranges[index]);
        await Editor.SurfaceRef.HighlightAsync(_ranges);
        await FocusInputAsync();
    }

    private async Task ClearAsync()
    {
        if (Editor?.SurfaceRef is { } surface)
            await surface.HighlightAsync([]);
    }

    private async Task SearchAsync()
    {
        if (!Refresh())
        {
            await ClearAsync();
            return;
        }

        var from = Editor.Selection.Normalize(Document).Start;
        await ShowAsync(from == null ? 0 : Following(from));
    }

    private Task OnFindInputAsync(ChangeEventArgs e)
    {
        _find = e.Value?.ToString() ?? "";
        return SearchAsync();
    }

    private Task ToggleAsync(Action toggle)
    {
        toggle();
        return SearchAsync();
    }

    private async Task StepAsync(bool forward)
    {
        if (!Refresh())
        {
            await ClearAsync();
            return;
        }

        var selection = Editor.Selection.Normalize(Document);
        var count = _ranges.Count;
        var next = OnMatch(selection, out var current)
            ? (current + (forward ? 1 : count - 1)) % count
            : selection.Start == null ? 0 : forward ? Following(selection.End) : Preceding(selection.Start);
        await ShowAsync(next);
    }

    private async Task ReplaceAsync()
    {
        if (Editor.Locked || !Refresh())
            return;
        var document = Document;
        string text = null;
        var current = -1;
        if (OnMatch(Editor.Selection, out current))
            text = DocumentSearch.ReplacementFor(document, _ranges[current], _find, _with, Options);
        if (text == null)
        {
            await StepAsync(true);
            return;
        }

        var range = _ranges[current];
        await Editor.SurfaceRef.RunAsync(_ =>
        {
            var paragraph = document.Resolve(range.Start with { Offset = 0 }).Paragraph;
            document.Execute(new ReplaceTextCommand(range, text, InlineEdit.At(paragraph, range.Start.Offset)?.Format));
        });
        var after = document.PositionOf(range.Start.BlockId, range.Start.Offset + text.Length);
        if (!Refresh())
        {
            await ClearAsync();
            return;
        }

        await ShowAsync(Following(after));
    }

    private async Task ReplaceAllAsync()
    {
        var document = Document;
        if (Editor.Locked || document == null || _find.Length == 0)
            return;
        var (count, timeout, invalid) = (0, false, false);
        await Editor.SurfaceRef.RunAsync(_ =>
        {
            try { count = DocumentSearch.ReplaceAll(document, _find, _with, Options); }
            catch (TimeoutException) { timeout = true; }
            catch (ArgumentException) { invalid = true; }
        });
        Refresh();
        _message = timeout ? TryLocalize("The search took too long; results are incomplete.") : invalid ? null : TryLocalize(count == 1 ? "{0} replacement made" : "{0} replacements made", count);
        await Editor.SurfaceRef.HighlightAsync(_ranges);
    }

    private Task OnKeyDownAsync(KeyboardEventArgs e) => e.Key switch
    {
        "Enter" => StepAsync(!e.ShiftKey),
        "Escape" => Closed.InvokeAsync(),
        _ => Task.CompletedTask
    };

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        try { await ClearAsync(); }
        catch (Exception e) when (e is ObjectDisposedException or InvalidOperationException) { }
        await base.DisposeAsync();
    }
}
