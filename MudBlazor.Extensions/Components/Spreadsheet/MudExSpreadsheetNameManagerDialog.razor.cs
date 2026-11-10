using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The Name Manager of <see cref="MudExSpreadsheetEditor"/>: lists the defined names and adds, changes, renames and deletes them. Every
/// change is applied at once as one undo step.
/// </summary>
public partial class MudExSpreadsheetNameManagerDialog
{
    private string _selected;
    private string _name = "";
    private string _refersTo = "";
    private string _error;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The workbook whose names are managed.</summary>
    [Parameter] public Workbook Workbook { get; set; }

    /// <summary>The reference proposed for a new name, usually the selection.</summary>
    [Parameter] public string DefaultRefersTo { get; set; } = "";

    private bool CanSave => MudExSpreadsheetGrid.IsValidName(_name?.Trim() ?? "") && !string.IsNullOrWhiteSpace(_refersTo?.TrimStart('='));

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        New();
    }

    private void Select(string name)
    {
        _selected = name;
        _name = name;
        _refersTo = "=" + Workbook.DefinedNames[name];
        _error = null;
    }

    private void New()
    {
        _selected = null;
        _name = "";
        _refersTo = "=" + DefaultRefersTo;
        _error = null;
    }

    private void Save()
    {
        var name = _name.Trim();
        var refersTo = _refersTo.Trim().TrimStart('=');
        if (Run("Define name", () =>
            {
                var other = _selected == null || !string.Equals(_selected, name, StringComparison.OrdinalIgnoreCase);
                if (other && Workbook.DefinedNames.ContainsKey(name)) throw new ArgumentException(TryLocalize("The name '{0}' already exists.", name));
                if (_selected != null && _selected != name) Workbook.Execute(new RenameNameCommand(_selected, name));
                if (_selected == null || Workbook.DefinedNames[name] != refersTo) Workbook.Execute(new DefineNameCommand(name, refersTo));
            }))
            Select(Workbook.DefinedNames.Keys.First(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase)));
    }

    private void Delete()
    {
        if (_selected != null && Run("Delete name", () => Workbook.Execute(new DefineNameCommand(_selected, null))))
            New();
    }

    private bool Run(string description, Action action)
    {
        try
        {
            using (Workbook.Undo.BeginTransaction(description))
                action();
            _error = null;
            return true;
        }
        catch (Exception e)
        {
            _error = e.Message;
            return false;
        }
    }

    private void Close() => MudDialog.Close();
}
