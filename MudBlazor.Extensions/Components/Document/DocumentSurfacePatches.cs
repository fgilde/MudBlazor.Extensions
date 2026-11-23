using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

using Block = MudBlazor.Extensions.Office.Document.Block;

internal static class DocumentSurfacePatches
{
    public static IReadOnlyList<SurfacePatch> Initial(TextDocument document, DocumentSurfaceRender render, Func<int> nextSeq, int chunk = 500)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(chunk, 1);
        var upserts = All(document, render);
        var patches = new List<SurfacePatch>();
        for (var i = 0; i == 0 || i < upserts.Count; i += chunk)
            patches.Add(new SurfacePatch(nextSeq(), i == 0, i == 0 ? render.SheetCss() : null, [], upserts.GetRange(i, Math.Min(chunk, upserts.Count - i))));
        return patches;
    }

    public static SurfacePatch Change(TextDocument document, DocumentSurfaceRender render, TextDocumentChangedEventArgs change, int seq)
    {
        if (change.Kind.HasFlag(DocumentChangeKind.Styles)) return new SurfacePatch(seq, true, render.SheetCss(), [], All(document, render));
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
        return new SurfacePatch(seq, false, change.Kind.HasFlag(DocumentChangeKind.Sections) ? render.SheetCss() : null, change.RemovedBlockIds.ToList(), upserts);
    }

    public static SurfacePatch Refresh(TextDocument document, DocumentSurfaceRender render, IEnumerable<string> paragraphIds, int seq) =>
        new(seq, false, null, [], paragraphIds.Distinct().Select(document.FindBlock).OfType<Paragraph>().Select(p => new SurfaceUpsert(render.Render(p), null, true)).ToList());

    public static IEnumerable<string> Ids(SurfacePatch patch) => patch.Remove.Concat(patch.Upsert.SelectMany(u => Subtree(u.Block))).Where(id => id != null);

    private static List<SurfaceUpsert> All(TextDocument document, DocumentSurfaceRender render) =>
        document.Blocks.Select((block, i) => new SurfaceUpsert(render.Render(block), i == 0 ? null : document.Blocks[i - 1].Id, false)).ToList();

    private static IEnumerable<Paragraph> Paragraphs(Table table) =>
        table.Rows.SelectMany(r => r.Cells).SelectMany(c => c.Blocks).SelectMany(b => b switch { Paragraph p => [p], Table t => Paragraphs(t), _ => [] });

    private static IEnumerable<string> Subtree(SurfaceBlock block) =>
        (block.Rows ?? []).SelectMany(r => r.Cells).SelectMany(c => c.Blocks).Concat(block.Blocks ?? []).SelectMany(Subtree).Prepend(block.Id);
}
