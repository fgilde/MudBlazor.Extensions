using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

public partial class MudExDocumentEditor
{
    private const int MaxHeadings = 5000;

    private bool _showRuler = true;
    private bool _showNav;
    private bool _showComments;
    private bool _navStale = true;
    private TextDocument _navDocument;
    private List<MudExDocumentHeading> _headings = [];
    private HashSet<string> _headingIds = [];

    private IReadOnlyList<MudExDocumentHeading> Headings
    {
        get
        {
            if (_navStale || !ReferenceEquals(_navDocument, _document))
            {
                _headings = _document == null ? [] : _document.Blocks.OfType<Paragraph>().Select(HeadingOf).Where(h => h != null).Take(MaxHeadings).ToList();
                (_headingIds, _navDocument, _navStale) = ([.. _headings.Select(h => h.Id)], _document, false);
            }

            return _headings;
        }
    }

    private MudExDocumentHeading HeadingOf(Paragraph paragraph) =>
        _document.EffectiveParagraphFormat(paragraph).OutlineLevel is { } level and < 9 && paragraph.Text.Replace((char)0xFFFC, ' ').Trim() is { Length: > 0 } text
            ? new MudExDocumentHeading(paragraph.Id, text.Length > 80 ? text[..80] : text, level)
            : null;

    private void TrackHeadings(TextDocumentChangedEventArgs e)
    {
        if (_navStale || !_showNav)
        {
            _navStale = true;
            return;
        }

        _navStale = e.Kind.HasFlag(DocumentChangeKind.Structure) || e.Kind.HasFlag(DocumentChangeKind.Styles) || e.RemovedBlockIds.Count > 0
                    || e.ChangedBlockIds.Any(id => _headingIds.Contains(id) || _document.FindBlock(id) is Paragraph p && HeadingOf(p) != null);
    }

    private string CurrentHeadingId
    {
        get
        {
            if (!_showNav || _document == null || Selection.Start?.BlockId is not { } id)
                return null;
            _ = Headings;
            string current = null;
            foreach (var block in _document.Blocks)
            {
                if (_headingIds.Contains(block.Id))
                    current = block.Id;
                if (block.Id == id)
                    break;
            }

            return current;
        }
    }

    private MudExDocumentRulerParagraph RulerParagraph =>
        _showRuler && _document != null && Selection.Start is { BlockId: { } id, Path: null or { Count: 0 } } && _document.FindBlock(id) is Paragraph p
            ? new MudExDocumentRulerParagraph(_document.EffectiveParagraphFormat(p).IndentLeft ?? 0, _document.EffectiveParagraphFormat(p).IndentRight ?? 0,
                _document.EffectiveParagraphFormat(p).IndentFirstLine ?? 0, _document.EffectiveParagraphFormat(p).Tabs ?? [])
            : null;

    private string PaneKey => $"{CurrentHeadingId}|{(RulerParagraph is { } r ? $"{r.Left}/{r.Right}/{r.FirstLine}/{string.Join(',', r.Tabs.Select(t => t.Position))}" : "")}|{_showNav}{_showRuler}{_showComments}";

    private Task OnRulerEditedAsync(MudExDocumentRulerEdit edit)
    {
        if (IsReadOnly || RulerParagraph is not { } paragraph || CurrentPage is not { } page)
            return Task.CompletedTask;
        var value = Math.Clamp(edit.Twips, 0, page.TextWidth);
        return edit.Kind switch
        {
            "left" => SetParagraphAsync(p => p with { IndentLeft = value }, "Indent"),
            "first" => SetParagraphAsync(p => p with { IndentFirstLine = value - paragraph.Left }, "Indent"),
            "right" => SetParagraphAsync(p => p with { IndentRight = page.TextWidth - value }, "Indent"),
            "tab" or "add" => SetParagraphAsync(p => p with { Tabs = NewTabs(paragraph.Tabs, edit with { Twips = value }) }, "Tab stops"),
            _ => Task.CompletedTask
        };
    }

    private static IReadOnlyList<TabStop> NewTabs(IReadOnlyList<TabStop> current, MudExDocumentRulerEdit edit)
    {
        var tabs = current.ToList();
        if (edit.Kind == "add")
            tabs.Add(new TabStop(edit.Twips, TabAlignment.Left, null));
        else if (edit.Index >= 0 && edit.Index < tabs.Count)
        {
            var moved = tabs[edit.Index];
            tabs.RemoveAt(edit.Index);
            if (!edit.Remove)
                tabs.Add(moved with { Position = edit.Twips });
        }

        return tabs.OrderBy(t => t.Position).ToList();
    }

    internal async Task GoToParagraphAsync(string id)
    {
        if (_surface == null || _document?.FindBlock(id ?? "") is not Paragraph)
            return;
        var position = _document.PositionOf(id, 0);
        await _surface.SelectAsync(new DocumentRange(position, position));
        await FocusSurfaceAsync();
    }
}
