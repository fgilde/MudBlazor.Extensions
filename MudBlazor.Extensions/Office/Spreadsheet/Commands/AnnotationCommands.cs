namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>
/// Adds a hyperlink or replaces the link with the same area. A link set on part of a ranged link is added after it and applies to its cells, as in Excel;
/// build an edit with <c>with</c> from <see cref="Worksheet.LinkAt"/> to change the ranged link itself.
/// </summary>
/// <param name="sheet">The sheet.</param>
/// <param name="link">The link; it needs a <see cref="CellLink.Target"/> or a <see cref="CellLink.Location"/>.</param>
public sealed class SetLinkCommand(Worksheet sheet, CellLink link) : IWorkbookCommand
{
    private AnnotationState _before;

    /// <inheritdoc />
    public string Description => "Hyperlink";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        ArgumentNullException.ThrowIfNull(link);
        Worksheet.Check(link.Cell);
        if (string.IsNullOrEmpty(link.Target) && string.IsNullOrEmpty(link.Location))
            throw new ArgumentException("A link needs a target or a location.", nameof(link));
        _before = AnnotationState.Capture(sheet);
        var cell = link.Cell.Relative;
        var area = (link with { Cell = cell }).Area;
        var old = sheet.Links.LastOrDefault(l => l.Area == area);
        var next = link with { Cell = cell, Source = link.Source ?? old?.Source, RelationshipId = link.RelationshipId ?? old?.RelationshipId };
        (_before with { Links = AnnotationState.Put(sheet.Links, old, next) }).Restore(wb, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _before.Restore(wb, sheet);
}

/// <summary>Removes the hyperlink that applies to a cell (<see cref="Worksheet.LinkAt"/>).</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="cell">A cell of the link.</param>
public sealed class RemoveLinkCommand(Worksheet sheet, CellRef cell) : IWorkbookCommand
{
    private AnnotationState _before;

    /// <inheritdoc />
    public string Description => "Remove hyperlink";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        var old = sheet.LinkAt(cell) ?? throw new ArgumentException("The cell has no link.", nameof(cell));
        _before = AnnotationState.Capture(sheet);
        (_before with { Links = AnnotationState.Put(sheet.Links, old, null) }).Restore(wb, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _before.Restore(wb, sheet);
}

/// <summary>Adds a note to a cell or replaces its note. Build an edit with <c>with</c> from the current note so its shape and formatting stay.</summary>
/// <param name="sheet">The sheet; its <see cref="Worksheet.NotesReadOnly"/> must be false.</param>
/// <param name="note">The note.</param>
public sealed class SetNoteCommand(Worksheet sheet, CellNote note) : IWorkbookCommand
{
    private AnnotationState _before;

    /// <inheritdoc />
    public string Description => "Note";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        ArgumentNullException.ThrowIfNull(note);
        AnnotationState.CheckWritable(sheet);
        Worksheet.Check(note.Cell);
        _before = AnnotationState.Capture(sheet);
        var cell = note.Cell.Relative;
        var old = sheet.Notes.FirstOrDefault(n => n.Cell == cell);
        var next = note with { Cell = cell, Source = note.Source ?? old?.Source, Shape = note.Shape ?? old?.Shape };
        (_before with { Notes = AnnotationState.Put(sheet.Notes, old, next) }).Restore(wb, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _before.Restore(wb, sheet);
}

/// <summary>Removes the note of a cell.</summary>
/// <param name="sheet">The sheet; its <see cref="Worksheet.NotesReadOnly"/> must be false.</param>
/// <param name="cell">The cell.</param>
public sealed class RemoveNoteCommand(Worksheet sheet, CellRef cell) : IWorkbookCommand
{
    private AnnotationState _before;

    /// <inheritdoc />
    public string Description => "Delete note";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        AnnotationState.CheckWritable(sheet);
        var old = sheet.Notes.FirstOrDefault(n => n.Cell == cell.Relative) ?? throw new ArgumentException("The cell has no note.", nameof(cell));
        _before = AnnotationState.Capture(sheet);
        (_before with { Notes = AnnotationState.Put(sheet.Notes, old, null) }).Restore(wb, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _before.Restore(wb, sheet);
}

internal sealed record AnnotationState(IReadOnlyList<CellLink> Links, IReadOnlyList<CellNote> Notes)
{
    public static AnnotationState Capture(Worksheet sheet) => new(sheet.Links, sheet.Notes);

    public static void CheckWritable(Worksheet sheet)
    {
        if (sheet.NotesReadOnly) throw new InvalidOperationException("The notes of a sheet with threaded comments cannot be edited.");
    }

    public static List<T> Put<T>(IReadOnlyList<T> list, T old, T item) where T : class
    {
        var next = list.ToList();
        var at = old == null ? -1 : next.IndexOf(old);
        if (at < 0) next.Add(item);
        else if (item == null) next.RemoveAt(at);
        else next[at] = item;
        return next;
    }

    public void Restore(Workbook wb, Worksheet sheet)
    {
        var touched = Changed(sheet.Links, Links).Select(l => l.Area).Concat(Changed(sheet.Notes, Notes).Select(n => new CellRange(n.Cell, n.Cell))).ToList();
        sheet.Links = Links;
        sheet.Notes = Notes;
        foreach (var r in touched) wb.NotifyFormatted(sheet, r);
        sheet.Modified = true;
    }

    private static IEnumerable<T> Changed<T>(IReadOnlyList<T> a, IReadOnlyList<T> b) where T : class =>
        a.Except<T>(b, ReferenceEqualityComparer.Instance).Concat(b.Except<T>(a, ReferenceEqualityComparer.Instance));
}
