using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The Data Validation dialog of <see cref="MudExSpreadsheetEditor"/> with the tabs Settings, Input Message and Error Alert. Closes with an add,
/// edit or delete command for the sheet's data validations as result data.
/// </summary>
public partial class MudExSpreadsheetValidationDialog
{
    private static readonly (ValidationType Type, string Name)[] Types =
    [
        (ValidationType.Any, "Any value"), (ValidationType.Whole, "Whole number"), (ValidationType.Decimal, "Decimal"), (ValidationType.List, "List"),
        (ValidationType.Date, "Date"), (ValidationType.Time, "Time"), (ValidationType.TextLength, "Text length"), (ValidationType.Custom, "Custom")
    ];

    private static readonly (string Operator, string Name)[] Operators =
    [
        ("between", "between"), ("notBetween", "not between"), ("equal", "equal to"), ("notEqual", "not equal to"), ("greaterThan", "greater than"),
        ("lessThan", "less than"), ("greaterThanOrEqual", "greater than or equal to"), ("lessThanOrEqual", "less than or equal to")
    ];

    private DataValidation _existing;
    private ValidationType _type;
    private string _operator = "between";
    private string _formula1 = "";
    private string _formula2 = "";
    private bool _allowBlank = true;
    private bool _dropDown = true;
    private string _promptTitle;
    private string _prompt;
    private string _errorStyle = "stop";
    private string _errorTitle;
    private string _error;
    private int _tab;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The sheet.</summary>
    [Parameter] public Worksheet Sheet { get; set; }

    /// <summary>The ranges a new validation applies to, usually the selection.</summary>
    [Parameter] public IReadOnlyList<CellRange> Ranges { get; set; }

    /// <summary>Index of the validation to edit in <see cref="Worksheet.DataValidations"/>, or -1 to add one.</summary>
    [Parameter] public int Index { get; set; } = -1;

    private bool Compares => _type is ValidationType.Whole or ValidationType.Decimal or ValidationType.Date or ValidationType.Time or ValidationType.TextLength;

    private bool Between => Compares && _operator is "between" or "notBetween";

    private bool Valid => _type == ValidationType.Any || (!string.IsNullOrWhiteSpace(_formula1) && (!Between || !string.IsNullOrWhiteSpace(_formula2)));

    private string FirstLabel => _type switch
    {
        ValidationType.List => "Source",
        ValidationType.Custom => "Formula",
        _ when Between => "Minimum",
        _ => "Value"
    };

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _existing = Index >= 0 && Index < (Sheet?.DataValidations.Count ?? 0) ? Sheet.DataValidations[Index] : null;
        if (_existing is not { } v) return;
        (_type, _operator, _allowBlank, _dropDown) = (v.Type, v.Operator ?? "between", v.AllowBlank, v.ShowDropDown);
        (_formula1, _formula2) = (Display(v.Formula1, v.Type), Display(v.Formula2, v.Type));
        (_promptTitle, _prompt, _errorStyle, _errorTitle, _error) = (v.PromptTitle, v.Prompt, v.ErrorStyle ?? "stop", v.ErrorTitle, v.Error);
    }

    private static string Display(string formula, ValidationType type)
    {
        if (formula == null) return "";
        if (type == ValidationType.List && formula.Length >= 2 && formula[0] == '"' && formula[^1] == '"') return formula[1..^1].Replace("\"\"", "\"");
        return formula.StartsWith('=') || type is not (ValidationType.List or ValidationType.Custom) ? formula : "=" + formula;
    }

    private string Formula(string text)
    {
        text = text?.Trim() ?? "";
        if (text.Length == 0) return null;
        if (text.StartsWith('=')) return text[1..];
        return _type == ValidationType.List ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;
    }

    private static string Text(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private void Ok()
    {
        var rule = (_existing ?? new DataValidation(Ranges ?? [], _type)) with
        {
            Type = _type,
            Operator = Compares ? _operator : "between",
            Formula1 = _type == ValidationType.Any ? null : Formula(_formula1),
            Formula2 = Between ? Formula(_formula2) : null,
            AllowBlank = _allowBlank,
            ShowDropDown = _dropDown,
            PromptTitle = Text(_promptTitle),
            Prompt = Text(_prompt),
            ErrorStyle = _errorStyle,
            ErrorTitle = Text(_errorTitle),
            Error = Text(_error)
        };
        MudDialog.Close(DialogResult.Ok<IWorkbookCommand>(_existing == null ? new AddDataValidationCommand(Sheet, rule) : new EditDataValidationCommand(Sheet, Index, rule)));
    }

    private void ClearAll()
    {
        if (_existing != null)
            MudDialog.Close(DialogResult.Ok<IWorkbookCommand>(new DeleteDataValidationCommand(Sheet, Index)));
    }

    private void Cancel() => MudDialog.Cancel();
}
