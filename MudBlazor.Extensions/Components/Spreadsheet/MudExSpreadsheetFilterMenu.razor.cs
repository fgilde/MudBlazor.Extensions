using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The drop-down of an auto filter column in <see cref="MudExSpreadsheetGrid"/>: sorting, a searchable list of the column's values and
/// number or text filters. It raises <see cref="OnApply"/> with the new criteria and leaves the command to the host.
/// </summary>
public partial class MudExSpreadsheetFilterMenu
{
    /// <summary>The most distinct values the list shows, as in Excel.</summary>
    public const int MaxValues = 10_000;

    private const int VirtualizeFrom = 200;

    private static readonly Choice[] Choices =
    [
        new("Equals", CustomFilterOperator.Equal, "{0}"),
        new("Does Not Equal", CustomFilterOperator.NotEqual, "{0}"),
        new("Is Greater Than", CustomFilterOperator.GreaterThan, "{0}"),
        new("Is Greater Than Or Equal To", CustomFilterOperator.GreaterThanOrEqual, "{0}"),
        new("Is Less Than", CustomFilterOperator.LessThan, "{0}"),
        new("Is Less Than Or Equal To", CustomFilterOperator.LessThanOrEqual, "{0}"),
        new("Begins With", CustomFilterOperator.Equal, "{0}*"),
        new("Ends With", CustomFilterOperator.Equal, "*{0}"),
        new("Contains", CustomFilterOperator.Equal, "*{0}*"),
        new("Does Not Contain", CustomFilterOperator.NotEqual, "*{0}*")
    ];

    private static readonly Choice Between = new("Between", CustomFilterOperator.GreaterThanOrEqual, "{0}");
    private static readonly Choice[] NumberChoices = [.. Choices[..6], Between];
    private static readonly Choice[] TextChoices = [Choices[0], Choices[1], .. Choices[6..]];

    private readonly string _id = "mud-ex-ssfm-" + Guid.NewGuid().ToString("N");
    private readonly HashSet<string> _checked = new(StringComparer.OrdinalIgnoreCase);
    private readonly int[] _operators = [0, -1];
    private readonly string[] _values = ["", ""];
    private List<string> _items = [];
    private bool _hasBlanks;
    private bool _blanks = true;
    private bool _capped;
    private bool _numeric;
    private string _search = "";
    private bool _showOperators;
    private bool _custom;
    private bool _and = true;
    private List<string> _shown;

    private sealed record Choice(string Name, CustomFilterOperator Operator, string Pattern);

    /// <summary>The sheet.</summary>
    [Parameter] public Worksheet Sheet { get; set; }

    /// <summary>The filter range including its header row.</summary>
    [Parameter] public CellRange Range { get; set; }

    /// <summary>Zero-based sheet column of the filter.</summary>
    [Parameter] public int Column { get; set; }

    /// <summary>The auto filter the column belongs to. Rows hidden by its other columns are left out of the value list, as in Excel.</summary>
    [Parameter] public AutoFilter Filter { get; set; }

    /// <summary>The current criteria of the column, or null.</summary>
    [Parameter] public FilterCriteria Criteria { get; set; }

    /// <summary>Whether the filter part is offered; false shows only sorting.</summary>
    [Parameter] public bool CanFilter { get; set; } = true;

    /// <summary>Returns the shown text of a cell; filters compare against it.</summary>
    [Parameter] public Func<CellRef, string> DisplayText { get; set; }

    /// <summary>Raised with true for descending order when the user sorts by the column.</summary>
    [Parameter] public EventCallback<bool> OnSort { get; set; }

    /// <summary>Raised with the new criteria of the column, null to show all values.</summary>
    [Parameter] public EventCallback<FilterCriteria> OnApply { get; set; }

    /// <summary>Raised when the user closes the menu without a change.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    private bool Numeric => _numeric;

    private string Header => DisplayText?.Invoke(new CellRef(Range.Top, Column)) ?? "";

    private List<string> Shown => _shown ??= _search.Length == 0 ? _items : _items.Where(v => v.Contains(_search, StringComparison.CurrentCultureIgnoreCase)).ToList();

    private bool AllChecked => Shown.All(_checked.Contains) && (!_hasBlanks || _blanks || _search.Length > 0);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Sheet == null || DisplayText == null) return;
        var bottom = Math.Min(Range.Bottom, Sheet.UsedRange.Bottom);
        var seen = new Dictionary<string, CellValue>(StringComparer.OrdinalIgnoreCase);
        var numbers = 0;
        var hidden = Filter?.WithColumn(Column, null).Evaluate(Sheet) ?? [];
        for (var r = Range.Top + 1; r <= bottom; r++)
        {
            if (hidden.Contains(r)) continue;
            var at = new CellRef(r, Column);
            var text = DisplayText(at);
            if (text.Length == 0)
            {
                _hasBlanks = true;
                continue;
            }

            if (seen.ContainsKey(text)) continue;
            if (seen.Count >= MaxValues)
            {
                _capped = true;
                continue;
            }

            var value = Sheet[at]?.Value ?? default;
            if (value.Kind == CellValueKind.Number) numbers++;
            seen[text] = value;
        }

        _numeric = numbers * 2 > seen.Count;
        _items = seen.OrderBy(p => p.Value.Kind == CellValueKind.Number ? 0 : 1)
            .ThenBy(p => p.Value.NumberValue)
            .ThenBy(p => p.Key, StringComparer.CurrentCultureIgnoreCase)
            .Select(p => p.Key)
            .ToList();
        Load(Criteria);
    }

    private void Load(FilterCriteria criteria)
    {
        switch (criteria)
        {
            case { Kind: FilterKind.Values }:
                _checked.UnionWith(_items.Where(criteria.Shown.Contains));
                _blanks = criteria.Blanks;
                break;
            case { Kind: FilterKind.Custom }:
                _custom = true;
                _and = criteria.And;
                (_operators[0], _values[0]) = Find(criteria.Operator1, criteria.Value1);
                if (criteria.Operator2 is { } second) (_operators[1], _values[1]) = Find(second, criteria.Value2);
                break;
            default:
                _checked.UnionWith(_items);
                break;
        }
    }

    private static (int Index, string Value) Find(CustomFilterOperator op, string value)
    {
        value ??= "";
        var lead = value.Length > 1 && value[0] == '*';
        var trail = value.Length > 1 && value[^1] == '*';
        var pattern = lead && trail ? "*{0}*" : trail ? "{0}*" : lead ? "*{0}" : "{0}";
        var index = Array.FindIndex(Choices, c => c.Operator == op && c.Pattern == pattern);
        return index < 0
            ? (Array.FindIndex(Choices, c => c.Operator == op && c.Pattern == "{0}"), value)
            : (index, value[(lead ? 1 : 0)..(value.Length - (trail ? 1 : 0))]);
    }

    private void Check(string value, bool on)
    {
        if (on) _checked.Add(value);
        else _checked.Remove(value);
    }

    private void CheckAll(bool on)
    {
        foreach (var value in Shown) Check(value, on);
        if (_search.Length == 0) _blanks = on;
    }

    private void StartCustom(Choice choice)
    {
        _custom = true;
        _showOperators = false;
        if (choice == Between)
        {
            _operators[0] = Array.FindIndex(Choices, c => c.Operator == CustomFilterOperator.GreaterThanOrEqual);
            _operators[1] = Array.FindIndex(Choices, c => c.Operator == CustomFilterOperator.LessThanOrEqual);
            _and = true;
        }
        else
        {
            _operators[0] = Array.IndexOf(Choices, choice);
            _operators[1] = -1;
        }
    }

    private Task ApplyAsync() => OnApply.InvokeAsync(_custom ? CustomCriteria() : ValuesCriteria());

    private FilterCriteria ValuesCriteria()
    {
        var shown = Shown.Where(_checked.Contains).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var blanks = _search.Length == 0 && _blanks;
        return shown.Count == _items.Count && (blanks || !_hasBlanks) ? null : FilterCriteria.Values(shown, blanks);
    }

    private FilterCriteria CustomCriteria()
    {
        if (_operators[0] < 0) return null;
        string Value(int row) => string.Format(CultureInfo.InvariantCulture, Choices[_operators[row]].Pattern, _values[row].Trim());
        var first = Choices[_operators[0]];
        return _operators[1] < 0 || _values[1].Trim().Length == 0
            ? FilterCriteria.Custom(first.Operator, Value(0))
            : FilterCriteria.Custom(first.Operator, Value(0), _and, Choices[_operators[1]].Operator, Value(1));
    }
}
