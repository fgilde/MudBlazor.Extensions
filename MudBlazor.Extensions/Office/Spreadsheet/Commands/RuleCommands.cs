namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>Adds conditional formatting to a sheet. Like Excel, its rules take the highest priorities and all other rules of the sheet move down.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="formatting">The ranges and rules.</param>
public sealed class AddConditionalFormattingCommand(Worksheet sheet, ConditionalFormatting formatting) : IWorkbookCommand
{
    private RuleState _before;

    /// <inheritdoc />
    public string Description => "Conditional formatting";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        RuleRanges.Check(formatting);
        _before = RuleState.Capture(sheet);
        (_before with { Formats = RuleState.Prepend(_before.Formats, formatting) }).Restore(wb, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _before.Restore(wb, sheet);
}

/// <summary>
/// Replaces one conditional formatting of a sheet. Build the replacement with <c>with</c> from the current item so the parts of the file the
/// model does not represent (extension links, unknown attributes) stay.
/// </summary>
/// <param name="sheet">The sheet.</param>
/// <param name="index">Index in <see cref="Worksheet.ConditionalFormats"/>.</param>
/// <param name="formatting">The new ranges and rules.</param>
public sealed class EditConditionalFormattingCommand(Worksheet sheet, int index, ConditionalFormatting formatting) : IWorkbookCommand
{
    private RuleState _before;

    /// <inheritdoc />
    public string Description => "Conditional formatting";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        RuleRanges.Check(formatting);
        _before = RuleState.Capture(sheet);
        (_before with { Formats = RuleState.Replace(_before.Formats, index, formatting) }).Restore(wb, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _before.Restore(wb, sheet);
}

/// <summary>Removes one conditional formatting of a sheet.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="index">Index in <see cref="Worksheet.ConditionalFormats"/>.</param>
public sealed class DeleteConditionalFormattingCommand(Worksheet sheet, int index) : IWorkbookCommand
{
    private RuleState _before;

    /// <inheritdoc />
    public string Description => "Delete conditional formatting";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        _before = RuleState.Capture(sheet);
        (_before with { Formats = RuleState.Replace(_before.Formats, index, null) }).Restore(wb, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _before.Restore(wb, sheet);
}

/// <summary>Adds a data validation rule to a sheet.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="validation">The rule.</param>
public sealed class AddDataValidationCommand(Worksheet sheet, DataValidation validation) : IWorkbookCommand
{
    private RuleState _before;

    /// <inheritdoc />
    public string Description => "Data validation";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        RuleRanges.Check(validation.Ranges);
        validation.Rewrite(f => StructuredReferences.RequireTable(f, false));
        _before = RuleState.Capture(sheet);
        (_before with { Validations = [.. _before.Validations, validation] }).Restore(wb, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _before.Restore(wb, sheet);
}

/// <summary>
/// Replaces one data validation rule of a sheet. Build the replacement with <c>with</c> from the current item so the parts of the file the
/// model does not represent stay.
/// </summary>
/// <param name="sheet">The sheet.</param>
/// <param name="index">Index in <see cref="Worksheet.DataValidations"/>.</param>
/// <param name="validation">The new rule.</param>
public sealed class EditDataValidationCommand(Worksheet sheet, int index, DataValidation validation) : IWorkbookCommand
{
    private RuleState _before;

    /// <inheritdoc />
    public string Description => "Data validation";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        RuleRanges.Check(validation.Ranges);
        validation.Rewrite(f => StructuredReferences.RequireTable(f, false));
        _before = RuleState.Capture(sheet);
        (_before with { Validations = RuleState.Replace(_before.Validations, index, validation) }).Restore(wb, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _before.Restore(wb, sheet);
}

/// <summary>Removes one data validation rule of a sheet.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="index">Index in <see cref="Worksheet.DataValidations"/>.</param>
public sealed class DeleteDataValidationCommand(Worksheet sheet, int index) : IWorkbookCommand
{
    private RuleState _before;

    /// <inheritdoc />
    public string Description => "Delete data validation";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        _before = RuleState.Capture(sheet);
        (_before with { Validations = RuleState.Replace(_before.Validations, index, null) }).Restore(wb, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _before.Restore(wb, sheet);
}

/// <summary>
/// Removes conditional formatting and data validation from a range, like Excel's "Clear rules from selected cells". Ranges that only partly
/// overlap keep their other cells, and their formulas stay relative to the same cells.
/// </summary>
/// <param name="sheet">The sheet.</param>
/// <param name="range">The cleared range.</param>
/// <param name="conditionalFormats">True to clear conditional formatting.</param>
/// <param name="dataValidations">True to clear data validation.</param>
public sealed class ClearRulesCommand(Worksheet sheet, CellRange range, bool conditionalFormats = true, bool dataValidations = true) : IWorkbookCommand
{
    private RuleState _before;

    /// <inheritdoc />
    public string Description => "Clear rules";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        Worksheet.Check(range);
        _before = RuleState.Capture(sheet);
        var formats = !conditionalFormats ? _before.Formats
            : _before.Formats.Select(c => RuleRanges.Clear(c.Ranges, range) is { } left ? c.WithRanges(left.Ranges, left.Rows, left.Columns) : null)
                .OfType<ConditionalFormatting>().ToList();
        var validations = !dataValidations ? _before.Validations
            : _before.Validations.Select(d => RuleRanges.Clear(d.Ranges, range) is { } left ? d.WithRanges(left.Ranges, left.Rows, left.Columns) : null)
                .OfType<DataValidation>().ToList();
        new RuleState(formats, validations).Restore(wb, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _before.Restore(wb, sheet);
}

internal sealed record RuleState(IReadOnlyList<ConditionalFormatting> Formats, IReadOnlyList<DataValidation> Validations)
{
    public static RuleState Capture(Worksheet sheet) => new(sheet.ConditionalFormats, sheet.DataValidations);

    public static List<ConditionalFormatting> Prepend(IReadOnlyList<ConditionalFormatting> formats, ConditionalFormatting added)
    {
        var priorities = new Dictionary<ConditionalRule, int>(ReferenceEqualityComparer.Instance);
        foreach (var rule in added.Rules.OrderBy(r => r.Priority).Concat(formats.SelectMany(f => f.Rules).OrderBy(r => r.Priority)))
            priorities[rule] = priorities.Count + 1;

        ConditionalFormatting Renumber(ConditionalFormatting f)
        {
            var rules = f.Rules.Select(r => r.Priority == priorities[r] ? r : r with { Priority = priorities[r] }).ToList();
            return rules.SequenceEqual(f.Rules, ReferenceEqualityComparer.Instance) ? f : f with { Rules = rules };
        }

        return [.. formats.Select(Renumber), Renumber(added)];
    }

    public static List<T> Replace<T>(IReadOnlyList<T> list, int index, T item) where T : class
    {
        if (index < 0 || index >= list.Count) throw new ArgumentOutOfRangeException(nameof(index));
        var next = list.ToList();
        if (item == null) next.RemoveAt(index);
        else next[index] = item;
        return next;
    }

    public void Restore(Workbook wb, Worksheet sheet)
    {
        var touched = sheet.ConditionalFormats.SelectMany(c => c.Ranges).Concat(sheet.DataValidations.SelectMany(d => d.Ranges))
            .Concat(Formats.SelectMany(c => c.Ranges)).Concat(Validations.SelectMany(d => d.Ranges)).Distinct().ToList();
        sheet.ConditionalFormats = Formats;
        sheet.DataValidations = Validations;
        foreach (var r in touched) wb.NotifyFormatted(sheet, r);
        sheet.Modified = true;
    }
}
