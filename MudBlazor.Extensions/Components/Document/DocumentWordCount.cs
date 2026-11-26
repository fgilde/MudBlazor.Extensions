using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

internal sealed class DocumentWordCount
{
    private readonly TextDocument _document;
    private readonly Dictionary<string, int> _counts = new();

    public DocumentWordCount(TextDocument document)
    {
        _document = document;
        Recount();
    }

    public int Total { get; private set; }

    public void Apply(TextDocumentChangedEventArgs change)
    {
        if ((change.Kind & (DocumentChangeKind.Content | DocumentChangeKind.Structure)) == 0) return;
        var changed = change.ChangedNestedBlockIds.Concat(change.AddedNestedBlockIds).Concat(change.ChangedBlockIds).ToHashSet();
        if (changed.Count == 0 && change.RemovedBlockIds.Count == 0 && change.RemovedNestedBlockIds.Count == 0)
        {
            Recount();
            return;
        }

        foreach (var id in change.RemovedNestedBlockIds.Concat(change.RemovedBlockIds)) Set(id, null);
        foreach (var id in changed)
            Set(id, _document.FindBlock(id) is Paragraph paragraph && IsBody(id) ? MudExDocumentEditor.CountWords(paragraph.Text) : null);
    }

    private void Recount()
    {
        _counts.Clear();
        Total = 0;
        foreach (var paragraph in _document.AllParagraphs()) Set(paragraph.Id, MudExDocumentEditor.CountWords(paragraph.Text));
    }

    private void Set(string id, int? count)
    {
        if (_counts.Remove(id, out var old)) Total -= old;
        if (count is not { } words) return;
        _counts[id] = words;
        Total += words;
    }

    private bool IsBody(string id)
    {
        var top = _document.PositionOf(id, 0).BlockId;
        var index = _document.BlockIndex(top);
        return index >= 0 && index < _document.Blocks.Count && _document.Blocks[index].Id == top;
    }
}
