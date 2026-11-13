using System.ComponentModel;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

public partial class MudExNotebookCanvas
{
    private const double MinColumnWidth = 8;

    /// <summary>Called by the browser for a click into a table cell in the text tool: edits the text of that cell.</summary>
    /// <param name="id">The id of the table.</param>
    /// <param name="row">The row of the cell.</param>
    /// <param name="column">The column of the cell.</param>
    /// <returns>A task that completes when the text session started.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnCellClickAsync(string id, int row, int column) => InvokeAsync(async () =>
    {
        if (!Editable || Tool != NotebookTool.Text || Find(id) is not TableElement || _text is { Row: not null } session && session.ElementId == id
            && session.Row == row && session.Column == column)
            return;
        await CommitTextAsync();
        await EditCellAsync(id, row, column);
    });

    /// <summary>Called by the browser when the user dragged the border between two table columns.</summary>
    /// <param name="id">The id of the table.</param>
    /// <param name="widths">The new width of every column; widths below 8 pixels are raised to 8.</param>
    /// <returns>A task that completes when the widths are set.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnColumnWidthsAsync(string id, double[] widths) => InvokeAsync(async () =>
    {
        if (!Editable || Find(id) is not TableElement table || widths == null || widths.Length != table.ColumnWidths.Count || !widths.All(double.IsFinite))
            return;
        var target = widths.Select(w => Math.Max(MinColumnWidth, w)).ToList();
        if (target.SequenceEqual(table.ColumnWidths))
            return;
        await CommitTextAsync();
        Execute(new SetTableColumnWidthsCommand(Page, id, target));
    });

    internal async Task EditCellAsync(string id, int row, int column)
    {
        if (!Editable || Find(id) is not TableElement table || row < 0 || row >= table.Rows.Count || column < 0 || column >= table.Rows[row].Count)
            return;
        await SelectAsync([id]);
        await BeginTextAsync(CellRequest(table, row, column));
    }

    private async Task OnTextTabbedAsync((IReadOnlyList<NotebookTextParagraphDto> Paragraphs, bool Back) tab)
    {
        if (_text is not { } session)
            return;
        var paragraphs = NotebookTextDom.FromDom(OriginalOf(session), tab.Paragraphs);
        if (!Editable)
            await EndTextEditAsync(null);
        else if (session is { Row: { } row, Column: { } column } && Find(session.ElementId) is TableElement table && CellOf(table, session) != null)
            await TabToCellAsync(table, row, column, paragraphs, tab.Back);
        else if (session.Row == null && !tab.Back && paragraphs is [{ List: NoteListKind.None }] && !NotebookEdit.IsEmpty(paragraphs))
            await ToTableAsync(session, paragraphs);
        else
            await EndTextEditAsync(paragraphs);
    }

    private async Task TabToCellAsync(TableElement table, int row, int column, IReadOnlyList<NoteParagraph> paragraphs, bool back)
    {
        var (nextRow, nextColumn) = back
            ? column > 0 ? (row, column - 1) : (row - 1, table.ColumnWidths.Count - 1)
            : column + 1 < table.ColumnWidths.Count ? (row, column + 1) : (row + 1, 0);
        _text = null;
        _sessionState = null;
        using (Notebook.Undo.BeginTransaction("Edit table"))
        {
            Notebook.Execute(new SetTableCellCommand(Page, table.Id, row, column, CellText(paragraphs)));
            if (nextRow >= table.Rows.Count)
                Notebook.Execute(new InsertTableRowsCommand(Page, table.Id, table.Rows.Count));
        }

        StateHasChanged();
        if (nextRow < 0)
            await RaiseSelectionAsync();
        else
            await EditCellAsync(table.Id, nextRow, nextColumn);
    }

    private async Task ToTableAsync(NotebookTextEditRequest session, IReadOnlyList<NoteParagraph> paragraphs)
    {
        _text = null;
        _sessionState = null;
        var add = new AddTableCommand(Page, 1, 2, session.X, session.Y);
        using (Notebook.Undo.BeginTransaction("Insert table"))
        {
            if (!session.IsNew && Find(session.ElementId) is TextElement)
                Notebook.Execute(new DeleteElementsCommand(Page, [session.ElementId]));
            Notebook.Execute(add);
            Notebook.Execute(new SetTableCellCommand(Page, add.Table.Id, 0, 0, paragraphs));
        }

        StateHasChanged();
        await EditCellAsync(add.Table.Id, 0, 1);
    }

    private NotebookTextEditRequest CellRequest(TableElement table, int row, int column) =>
        new(table.Id, table.X + table.ColumnWidths.Take(column).Sum() + NotebookStyles.CellPadding,
            table.Y + HeightOf(table) * row / Math.Max(1, table.Rows.Count) + NotebookStyles.CellPadding, false) { Row = row, Column = column };

    private static NoteTableCell CellOf(TableElement table, NotebookTextEditRequest session) =>
        session is { Row: { } row, Column: { } column } && row >= 0 && row < table.Rows.Count && column >= 0 && column < table.Rows[row].Count ? table.Rows[row][column] : null;

    private static IReadOnlyList<NoteParagraph> CellText(IReadOnlyList<NoteParagraph> paragraphs) => paragraphs.Count == 0 ? [new NoteParagraph()] : paragraphs;

    private static string CellKey(NotebookTextEditRequest session) => session.Row is { } row && session.Column is { } column ? $"{row},{column}" : null;
}
