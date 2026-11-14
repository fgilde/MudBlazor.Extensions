using System.Globalization;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>What a data validation accepts.</summary>
public enum ValidationType
{
    /// <summary>Any value.</summary>
    Any,

    /// <summary>Whole numbers compared with the operator.</summary>
    Whole,

    /// <summary>Numbers compared with the operator.</summary>
    Decimal,

    /// <summary>One of the items of <see cref="DataValidation.Formula1"/>.</summary>
    List,

    /// <summary>Dates (serial numbers) compared with the operator.</summary>
    Date,

    /// <summary>Times (fractions of a day) compared with the operator.</summary>
    Time,

    /// <summary>Text whose length compares with the operator.</summary>
    TextLength,

    /// <summary>Values for which <see cref="DataValidation.Formula1"/> returns true.</summary>
    Custom
}

/// <summary>
/// A data validation rule on one or more ranges. Formulas are written for the top-left cell of the first range and move relatively for the
/// other cells.
/// </summary>
/// <param name="Ranges">The validated ranges.</param>
/// <param name="Type">What is accepted.</param>
/// <param name="Operator">between, notBetween, equal, notEqual, greaterThan, lessThan, greaterThanOrEqual or lessThanOrEqual.</param>
/// <param name="Formula1">The first value; for lists a quoted comma separated list such as <c>"a,b,c"</c> or a range such as <c>$E$1:$E$3</c>.</param>
/// <param name="Formula2">The second value for between and notBetween, else null.</param>
/// <param name="AllowBlank">True if blank cells are valid.</param>
/// <param name="ShowDropDown">True to offer the list items in a drop-down (written as Excel's inverted <c>showDropDown</c> attribute).</param>
/// <param name="PromptTitle">Title of the input message, or null.</param>
/// <param name="Prompt">Input message shown when the cell is selected, or null.</param>
/// <param name="ErrorTitle">Title of the error message, or null.</param>
/// <param name="Error">Error message shown for invalid input, or null.</param>
/// <param name="ErrorStyle">stop (reject), warning or information.</param>
public sealed record DataValidation(IReadOnlyList<CellRange> Ranges, ValidationType Type, string Operator = "between", string Formula1 = null,
    string Formula2 = null, bool AllowBlank = true, bool ShowDropDown = true, string PromptTitle = null, string Prompt = null, string ErrorTitle = null,
    string Error = null, string ErrorStyle = "stop")
{
    internal XElement Source { get; init; }

    internal CellRef Anchor => RuleRanges.Anchor(Ranges);

    internal DataValidation Rewrite(Func<string, string> map)
    {
        var (f1, f2) = (Formula1 == null ? null : map(Formula1), Formula2 == null ? null : map(Formula2));
        return f1 == Formula1 && f2 == Formula2 ? this : this with { Formula1 = f1, Formula2 = f2 };
    }

    internal DataValidation Shift(Axis axis, int index, int count) =>
        RuleRanges.Shift(Ranges, axis, index, count) is { } moved ? WithRanges(moved.Ranges, moved.Rows, moved.Columns) : null;

    internal DataValidation WithRanges(List<CellRange> ranges, int rows, int columns)
    {
        var next = this with { Ranges = ranges };
        return rows == 0 && columns == 0 ? next : next.Rewrite(f => ReferenceShifter.Translate(f, rows, columns));
    }
}

/// <summary>The outcome of <see cref="DataValidator.Validate"/>.</summary>
/// <param name="IsValid">True if the value is accepted.</param>
/// <param name="Validation">The rule that applies to the cell (its messages and error style), or null if none does.</param>
public sealed record ValidationResult(bool IsValid, DataValidation Validation);

/// <summary>Checks values against the data validation rules of a sheet without changing the workbook.</summary>
public static class DataValidator
{
    /// <summary>Checks a value before it is written into a cell.</summary>
    /// <param name="wb">The workbook.</param>
    /// <param name="sheet">The sheet.</param>
    /// <param name="cell">The target cell.</param>
    /// <param name="value">The proposed value.</param>
    /// <returns>The result; valid if no rule covers the cell.</returns>
    public static ValidationResult Validate(Workbook wb, Worksheet sheet, CellRef cell, CellValue value)
    {
        var dv = sheet.DataValidations.FirstOrDefault(d => d.Ranges.Any(r => r.Contains(cell)));
        return new ValidationResult(dv == null || IsValid(wb, sheet, dv, cell.Relative, value), dv);
    }

    /// <summary>The items of a list validation as display texts, for a drop-down.</summary>
    /// <param name="wb">The workbook.</param>
    /// <param name="sheet">The sheet holding the rule.</param>
    /// <param name="dv">The rule.</param>
    /// <returns>The items; empty for other types or blank sources.</returns>
    public static IReadOnlyList<string> ListItems(Workbook wb, Worksheet sheet, DataValidation dv)
    {
        if (dv.Type != ValidationType.List || RuleFormulas.Bare(dv.Formula1) is not { Length: > 0 } source) return [];
        if (source.StartsWith('"'))
            return source.Trim('"').Replace("\"\"", "\"").Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        if (RuleFormulas.Parse(source) is not { } node) return [];
        var result = RuleFormulas.Calculate(new FormulaEvaluator(FunctionRegistry.Default), node, wb, sheet, dv.Anchor);
        var items = new List<string>();
        switch (result.Kind)
        {
            case CalcValueKind.Reference:
                foreach (var (area, range) in result.Areas)
                    foreach (var c in FormulaEvaluator.TrimToUsed(area, range).Cells())
                        if (area[c]?.Value is { Kind: not CellValueKind.Empty } v)
                            items.Add(Display(wb, area, c, v));
                break;
            case CalcValueKind.Array:
                for (var r = 0; r < result.Array.Rows; r++)
                    for (var c = 0; c < result.Array.Columns; c++)
                        if (result.Array[r, c] is { Kind: not CellValueKind.Empty } v)
                            items.Add(v.ToString());
                break;
            default:
                if (result.Scalar.Kind is not (CellValueKind.Empty or CellValueKind.Error)) items.Add(result.Scalar.ToString());
                break;
        }

        return items;
    }

    private static string Display(Workbook wb, Worksheet sheet, CellRef c, CellValue v) =>
        NumberFormat.Format(v, wb.Styles[sheet.EffectiveStyleId(c)].NumberFormat, CultureInfo.CurrentCulture, out _);

    private static bool IsValid(Workbook wb, Worksheet sheet, DataValidation dv, CellRef cell, CellValue value)
    {
        if (dv.Type == ValidationType.Any) return true;
        if (value.Kind == CellValueKind.Empty) return dv.AllowBlank;
        switch (dv.Type)
        {
            case ValidationType.List:
                var items = ListItems(wb, sheet, dv);
                var texts = new[] { value.ToString(), Display(wb, sheet, cell, value) };
                return items.Any(i => texts.Any(t => string.Equals(i, t, StringComparison.CurrentCultureIgnoreCase)));
            case ValidationType.Custom:
                if (RuleFormulas.Parse(dv.Formula1) is not { } custom) return true;
                var own = ReferenceShifter.Translate(custom, cell.Row - dv.Anchor.Row, cell.Column - dv.Anchor.Column)
                    .Rewrite(n => n is RefNode { Sheet: null, Spill: false } r && r.Cell.Relative == cell ? RuleFormulas.Literal(value) : n);
                return RuleFormulas.IsTrue(RuleFormulas.Evaluate(wb, sheet, own, cell, cell));
            case ValidationType.TextLength:
                return Compare(wb, sheet, dv, cell, CellValue.Number(value.ToString().Length));
            default:
                if (value.Kind != CellValueKind.Number || dv.Type == ValidationType.Whole && value.NumberValue != Math.Floor(value.NumberValue)) return false;
                return Compare(wb, sheet, dv, cell, value);
        }
    }

    private static bool Compare(Workbook wb, Worksheet sheet, DataValidation dv, CellRef cell, CellValue value) =>
        RuleFormulas.Comparison(dv.Operator, RuleFormulas.Literal(value), dv.Formula1, dv.Formula2) is not { } node
        || RuleFormulas.IsTrue(RuleFormulas.Evaluate(wb, sheet, node, dv.Anchor, cell));
}
