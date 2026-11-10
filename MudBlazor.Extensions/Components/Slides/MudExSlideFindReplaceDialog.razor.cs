using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The Find and Replace dialog of <see cref="MudExSlideEditor"/>. Searches the text of all shapes and the notes, optionally case-sensitive or for whole
/// words. Find Next shows the slide and selects the shape; Replace All is one undo step and keeps the formatting of the runs.
/// </summary>
public partial class MudExSlideFindReplaceDialog
{
    private int _tab;
    private string _what = "";
    private string _with = "";
    private bool _matchCase;
    private bool _wholeWord;
    private string _status;
    private (int Slide, int Item, int Paragraph, int Index) _at;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The presentation to search.</summary>
    [Parameter] public Presentation Presentation { get; set; }

    /// <summary>The slide where the search starts.</summary>
    [Parameter] public Slide Start { get; set; }

    /// <summary>Whether the dialog opens on the Replace tab.</summary>
    [Parameter] public bool Replace { get; set; }

    /// <summary>Whether replacing is allowed; false shows only the Find part.</summary>
    [Parameter] public bool CanReplace { get; set; } = true;

    /// <summary>Shows a found slide and selects the shape, or no shape for a match in the notes.</summary>
    [Parameter] public Func<Slide, uint?, Task> Select { get; set; }

    private bool Replacing => _tab == 1;

    private bool CanSearch => !string.IsNullOrEmpty(_what);

    private StringComparison Comparison => _matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _tab = Replace && CanReplace ? 1 : 0;
        _at = (Start == null ? -1 : Presentation.SlideList.IndexOf(Start), -1, 0, 0);
    }

    private async Task FindNextAsync()
    {
        _status = null;
        var hits = Hits().ToList();
        if ((hits.FirstOrDefault(h => h.Key.CompareTo(_at) > 0) ?? hits.FirstOrDefault()) is not { } hit)
        {
            _status = TryLocalize("Cannot find \"{0}\".", _what);
            return;
        }

        _at = hit.Key;
        if (Select != null) await Select(hit.Slide, hit.Shape?.Id);
    }

    private async Task ReplaceAsync()
    {
        if (Hits().FirstOrDefault(h => h.Key == _at) is { } hit)
        {
            if (!TryWrite([hit], "Replace")) return;
            _at = _at with { Index = _at.Index + (_with ?? "").Length - 1 };
        }

        await FindNextAsync();
    }

    private void ReplaceAll()
    {
        var hits = Hits().ToList();
        if (TryWrite(hits, "Replace All"))
            _status = TryLocalize("All done. We made {0} replacements.", hits.Count);
    }

    private bool TryWrite(IReadOnlyList<Hit> hits, string description)
    {
        try
        {
            using (Presentation.Undo.BeginTransaction(description))
                foreach (var item in hits.GroupBy(h => (h.Slide, h.Shape)))
                {
                    var paragraphs = item.ToLookup(h => h.Key.Paragraph, h => h.Key.Index);
                    if (item.Key.Shape is { } shape)
                        Presentation.Execute(new SetTextCommand(item.Key.Slide, shape.Id, shape.Text with
                        {
                            Paragraphs = shape.Text.Paragraphs.Select((p, i) => paragraphs.Contains(i) ? Replaced(p, paragraphs[i]) : p).ToList()
                        }));
                    else
                        Presentation.Execute(new SetNotesCommand(item.Key.Slide, string.Join("\n", Lines(item.Key.Slide, null).Select((line, i) =>
                            paragraphs.Contains(i) ? string.Concat(Replaced(new TextParagraph { Runs = [new TextRun(line)] }, paragraphs[i]).Runs.Select(r => r.Text)) : line))));
                }

            return true;
        }
        catch (Exception e)
        {
            _status = e.Message;
            return false;
        }
    }

    private TextParagraph Replaced(TextParagraph paragraph, IEnumerable<int> starts)
    {
        var runs = paragraph.Runs.ToList();
        var changed = new HashSet<int>();
        foreach (var start in starts.OrderDescending())
        {
            var (position, placed) = (0, false);
            for (var r = 0; r < runs.Count; r++)
            {
                var text = runs[r].Text;
                var (from, to) = (Math.Max(start, position) - position, Math.Min(start + _what.Length, position + text.Length) - position);
                position += text.Length;
                if (from >= to) continue;
                runs[r] = runs[r] with { Text = text[..from] + (placed ? "" : _with ?? "") + text[to..] };
                changed.Add(r);
                placed = true;
            }
        }

        return paragraph with { Runs = runs.Where((run, i) => run.Text.Length > 0 || !changed.Contains(i)).ToList() };
    }

    private IEnumerable<Hit> Hits()
    {
        var slides = Presentation.SlideList;
        for (var s = 0; s < slides.Count; s++)
        {
            var items = SlideShape.Flatten(slides[s].Shapes).OfType<AutoShape>().Where(a => a.Text != null).Append(null).ToList();
            for (var i = 0; i < items.Count; i++)
            {
                var lines = Lines(slides[s], items[i]);
                for (var p = 0; p < lines.Count; p++)
                    foreach (var index in Matches(lines[p]))
                        yield return new Hit((s, i, p, index), slides[s], items[i]);
            }
        }
    }

    private static IReadOnlyList<string> Lines(Slide slide, AutoShape shape) =>
        shape == null ? slide.Notes.Split('\n') : shape.Text.Paragraphs.Select(p => string.Concat(p.Runs.Select(r => r.Text))).ToList();

    private IEnumerable<int> Matches(string text)
    {
        for (var i = text.IndexOf(_what, Comparison); i >= 0; i = text.IndexOf(_what, i + _what.Length, Comparison))
            if (!_wholeWord || (IsBoundary(text, i - 1) && IsBoundary(text, i + _what.Length)))
                yield return i;
    }

    private static bool IsBoundary(string text, int index) => index < 0 || index >= text.Length || !(char.IsLetterOrDigit(text[index]) || text[index] == '_');

    private void Close() => MudDialog.Close();

    private sealed record Hit((int Slide, int Item, int Paragraph, int Index) Key, Slide Slide, AutoShape Shape);
}
