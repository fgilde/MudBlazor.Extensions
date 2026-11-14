using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The Insert Function dialog of <see cref="MudExSpreadsheetEditor"/>: search and category filter over the worksheet functions with their
/// signature and description. Closes with the chosen <see cref="FunctionInfo"/> as result data.
/// </summary>
public partial class MudExSpreadsheetInsertFunctionDialog
{
    private const string All = "All";
    private string _search = "";
    private string _category = All;
    private FunctionInfo _selected;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The functions to choose from.</summary>
    [Parameter] public IReadOnlyCollection<FunctionInfo> Functions { get; set; } = [];

    private IEnumerable<string> Categories => Functions.Select(f => f.Category).Distinct().Order(StringComparer.Ordinal);

    private IEnumerable<FunctionInfo> Shown => Functions
        .Where(f => _category == All || f.Category == _category)
        .Where(f => string.IsNullOrWhiteSpace(_search) || f.Name.Contains(_search.Trim(), StringComparison.OrdinalIgnoreCase)
                                                       || f.Description?.Contains(_search.Trim(), StringComparison.CurrentCultureIgnoreCase) == true)
        .OrderBy(f => !f.Name.StartsWith(_search?.Trim() ?? "", StringComparison.OrdinalIgnoreCase))
        .ThenBy(f => f.Name, StringComparer.Ordinal);

    private void Insert(FunctionInfo function)
    {
        if (function != null) MudDialog.Close(DialogResult.Ok(function));
    }

    private void Cancel() => MudDialog.Cancel();
}
