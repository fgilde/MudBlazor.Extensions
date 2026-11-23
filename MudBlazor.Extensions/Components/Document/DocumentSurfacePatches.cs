using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

internal static class DocumentSurfacePatches
{
    public static IReadOnlyList<SurfacePatch> Initial(TextDocument document, DocumentSurfaceRender render, Func<int> nextSeq, int chunk = 500) =>
        Reset(document, render, nextSeq, chunk);

    public static IReadOnlyList<SurfacePatch> Changes(TextDocument document, DocumentSurfaceRender render, TextDocumentChangedEventArgs change, Func<int> nextSeq, int chunk = 500)
    {
        var removed = change.RemovedBlockIds.Where(render.Forget).ToList();
        foreach (var id in change.RemovedNestedBlockIds) render.Forget(id);
        if (change.Kind.HasFlag(DocumentChangeKind.Styles)) return Reset(document, render, nextSeq, chunk);
        var structure = change.Kind.HasFlag(DocumentChangeKind.Structure);
        var changedNested = change.ChangedNestedBlockIds.ToHashSet();
        var upserts = new List<SurfaceUpsert>();
        var top = change.ChangedBlockIds.Select(id => (Id: id, Index: document.BlockIndex(id)))
            .Where(b => b.Index >= 0 && b.Index < document.Blocks.Count && document.Blocks[b.Index].Id == b.Id)
            .OrderBy(b => b.Index);
        foreach (var (_, index) in top)
        {
            var block = document.Blocks[index];
            if (block is Table table && !structure && Paragraphs(table).Where(p => changedNested.Contains(p.Id)).ToList() is { Count: > 0 } paragraphs)
                upserts.AddRange(paragraphs.Select(p => new SurfaceUpsert(render.Render(p), null, true)));
            else
                upserts.Add(new SurfaceUpsert(render.Render(block), index == 0 ? null : document.Blocks[index - 1].Id, !structure));
        }

        var rendered = upserts.SelectMany(u => Subtree(u.Block)).ToHashSet();
        var labels = change.ListLabelChangedIds.Where(id => !rendered.Contains(id)).ToHashSet();
        if (labels.Count > 0)
            upserts.AddRange(document.AllParagraphs().Where(p => labels.Contains(p.Id)).Select(p => new SurfaceUpsert(render.Render(p), null, true)));
        return [new SurfacePatch(nextSeq(), false, change.Kind.HasFlag(DocumentChangeKind.Sections) ? render.SheetCss() : null, removed, upserts)];
    }

    public static SurfacePatch Change(TextDocument document, DocumentSurfaceRender render, TextDocumentChangedEventArgs change, int seq) =>
        Changes(document, render, change, () => seq, int.MaxValue)[0];

    public static SurfacePatch Refresh(TextDocument document, DocumentSurfaceRender render, IEnumerable<string> paragraphIds, int seq) =>
        new(seq, false, null, [], paragraphIds.Distinct().Select(document.FindBlock).OfType<Paragraph>().Select(p => new SurfaceUpsert(render.Render(p), null, true)).ToList());

    public static IEnumerable<string> Ids(SurfacePatch patch) => patch.Remove.Concat(patch.Upsert.SelectMany(u => Subtree(u.Block))).Where(id => id != null);

    public static IEnumerable<string> Ids(SurfacePatch patch, TextDocumentChangedEventArgs change) =>
        Ids(patch).Concat(change.RemovedBlockIds).Concat(change.RemovedNestedBlockIds).Where(id => id != null).Distinct();

    private static List<SurfacePatch> Reset(TextDocument document, DocumentSurfaceRender render, Func<int> nextSeq, int chunk)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(chunk, 1);
        var upserts = document.Blocks.Select((block, i) => new SurfaceUpsert(render.Render(block), i == 0 ? null : document.Blocks[i - 1].Id, false)).ToList();
        var patches = new List<SurfacePatch>();
        for (var i = 0; i == 0 || i < upserts.Count; i += chunk)
            patches.Add(new SurfacePatch(nextSeq(), i == 0, i == 0 ? render.SheetCss() : null, [], upserts.GetRange(i, Math.Min(chunk, upserts.Count - i))));
        return patches;
    }

    private static IEnumerable<Paragraph> Paragraphs(Table table) =>
        table.Rows.SelectMany(r => r.Cells).SelectMany(c => c.Blocks).SelectMany(b => b switch { Paragraph p => [p], Table t => Paragraphs(t), _ => [] });

    private static IEnumerable<string> Subtree(SurfaceBlock block) =>
        (block.Rows ?? []).SelectMany(r => r.Cells).SelectMany(c => c.Blocks).Concat(block.Blocks ?? []).SelectMany(Subtree).Prepend(block.Id);
}
