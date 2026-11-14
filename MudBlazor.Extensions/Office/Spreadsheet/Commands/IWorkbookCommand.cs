namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>
/// An undoable workbook edit. <see cref="Apply"/> may run repeatedly (execute, then redo) and must capture whatever
/// <see cref="Revert"/> needs on each call. Commands must validate before mutating so a failing Apply leaves no trace.
/// </summary>
public interface IWorkbookCommand
{
    /// <summary>Short description of the action for undo and redo labels.</summary>
    string Description { get; }

    /// <summary>Performs the edit.</summary>
    /// <param name="wb">The workbook.</param>
    void Apply(Workbook wb);

    /// <summary>Reverses the edit performed by the last <see cref="Apply"/>.</summary>
    /// <param name="wb">The workbook.</param>
    void Revert(Workbook wb);
}
