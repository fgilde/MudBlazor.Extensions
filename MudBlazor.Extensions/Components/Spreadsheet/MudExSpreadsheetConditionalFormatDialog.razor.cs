using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The conditional formatting dialog of <see cref="MudExSpreadsheetEditor"/>: the rules manager and the New/Edit Formatting Rule editor.
/// Closes with one add, edit or delete command for the sheet's conditional formats as result data.
/// </summary>
public partial class MudExSpreadsheetConditionalFormatDialog
{
    private enum RuleKind
    {
        CellValue,
        Text,
        Date,
        TopBottom,
        Average,
        Duplicate,
        Formula,
        ColorScale,
        DataBar,
        IconSet
    }

    private sealed record Entry(int FormatIndex, int RuleIndex, ConditionalRule Rule, IReadOnlyList<CellRange> Ranges);

    private static readonly (RuleKind Kind, string Name)[] Kinds =
    [
        (RuleKind.CellValue, "Format only cells that contain a value"), (RuleKind.Text, "Format only cells with specific text"),
        (RuleKind.Date, "Format only dates occurring"), (RuleKind.TopBottom, "Format only top or bottom ranked values"),
        (RuleKind.Average, "Format only values that are above or below average"), (RuleKind.Duplicate, "Format only unique or duplicate values"),
        (RuleKind.Formula, "Use a formula to determine which cells to format"), (RuleKind.ColorScale, "Format all cells with a color scale"),
        (RuleKind.DataBar, "Format all cells with a data bar"), (RuleKind.IconSet, "Format all cells with an icon set")
    ];

    private static readonly (string Operator, string Name)[] Operators =
    [
        ("between", "between"), ("notBetween", "not between"), ("equal", "equal to"), ("notEqual", "not equal to"), ("greaterThan", "greater than"),
        ("lessThan", "less than"), ("greaterThanOrEqual", "greater than or equal to"), ("lessThanOrEqual", "less than or equal to")
    ];

    private static readonly (ContainsTextKind Kind, string Name)[] TextKinds =
    [
        (ContainsTextKind.Contains, "containing"), (ContainsTextKind.NotContains, "not containing"), (ContainsTextKind.BeginsWith, "beginning with"),
        (ContainsTextKind.EndsWith, "ending with"), (ContainsTextKind.Blanks, "blanks"), (ContainsTextKind.NotBlanks, "no blanks"),
        (ContainsTextKind.Errors, "errors"), (ContainsTextKind.NotErrors, "no errors")
    ];

    private static readonly (string Period, string Name)[] Periods =
    [
        ("yesterday", "Yesterday"), ("today", "Today"), ("tomorrow", "Tomorrow"), ("last7Days", "In the last 7 days"), ("lastWeek", "Last week"),
        ("thisWeek", "This week"), ("nextWeek", "Next week"), ("lastMonth", "Last month"), ("thisMonth", "This month"), ("nextMonth", "Next month")
    ];

    internal static readonly (string Name, string Font, string Fill)[] Presets =
    [
        ("Light Red Fill with Dark Red Text", "#9C0006", "#FFC7CE"), ("Yellow Fill with Dark Yellow Text", "#9C5700", "#FFEB9C"),
        ("Green Fill with Dark Green Text", "#006100", "#C6EFCE"), ("Light Red Fill", null, "#FFC7CE"), ("Red Text", "#9C0006", null)
    ];

    private static readonly Regex HexColor = new("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant);

    private readonly string[] _colors = ["#F8696B", "#FFEB84", "#63BE7B", "#638EC6", "#9C0006", "#FFC7CE"];
    private bool _manage;
    private bool _wholeSheet;
    private Entry _editing;
    private RuleKind _kind;
    private string _appliesTo = "";
    private string _operator = "greaterThan";
    private string _value1 = "";
    private string _value2 = "";
    private ContainsTextKind _textKind;
    private string _period = "today";
    private int _rank = 10;
    private bool _percent;
    private bool _bottom;
    private bool _below;
    private bool _unique;
    private bool _threeColors = true;
    private string _iconSet = "3TrafficLights1";
    private bool _reverse;
    private bool _iconOnly;
    private int _preset;
    private bool _bold;
    private bool _italic;
    private bool _stop;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The sheet.</summary>
    [Parameter] public Worksheet Sheet { get; set; }

    /// <summary>The ranges a new rule applies to, usually the selection.</summary>
    [Parameter] public IReadOnlyList<CellRange> Ranges { get; set; }

    /// <summary>
    /// What the dialog opens with: manage, new, or a rule preset such as greaterThan, lessThan, between, equal, containsText, timePeriod, duplicate,
    /// top10, top10Percent, bottom10, bottom10Percent, aboveAverage or belowAverage.
    /// </summary>
    [Parameter] public string Kind { get; set; } = "new";

    private bool HasFormat => _kind is not (RuleKind.ColorScale or RuleKind.DataBar or RuleKind.IconSet);

    private IEnumerable<Entry> Entries => (Sheet?.ConditionalFormats ?? [])
        .SelectMany((cf, f) => cf.Rules.Select((rule, r) => new Entry(f, r, rule, cf.Ranges)))
        .Where(e => _wholeSheet || e.Ranges.Any(r => (Ranges ?? []).Any(r.Intersects)))
        .OrderBy(e => e.Rule.Priority);

    private bool Valid => ParseRanges() != null && _kind switch
    {
        RuleKind.CellValue => _value1.Trim().Length > 0 && (_operator is not ("between" or "notBetween") || _value2.Trim().Length > 0),
        RuleKind.Formula => _value1.Trim().TrimStart('=').Length > 0,
        RuleKind.Text => _textKind is not (ContainsTextKind.Contains or ContainsTextKind.NotContains or ContainsTextKind.BeginsWith or ContainsTextKind.EndsWith)
                         || _value1.Length > 0,
        _ => true
    };

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _appliesTo = string.Join(" ", (Ranges ?? []).Select(r => r.ToA1()));
        _manage = Kind == "manage";
        if (!_manage) StartNew(Kind);
    }

    private void StartNew(string kind)
    {
        _manage = false;
        _editing = null;
        _kind = kind switch
        {
            "containsText" => RuleKind.Text,
            "timePeriod" => RuleKind.Date,
            "duplicate" => RuleKind.Duplicate,
            "top10" or "top10Percent" or "bottom10" or "bottom10Percent" => RuleKind.TopBottom,
            "aboveAverage" or "belowAverage" => RuleKind.Average,
            _ => RuleKind.CellValue
        };
        if (Operators.Any(o => o.Operator == kind)) _operator = kind;
        _percent = kind is "top10Percent" or "bottom10Percent";
        _bottom = kind is "bottom10" or "bottom10Percent";
        _below = kind == "belowAverage";
    }

    private void StartEdit(Entry entry)
    {
        _manage = false;
        _editing = entry;
        _appliesTo = string.Join(" ", entry.Ranges.Select(r => r.ToA1()));
        _stop = entry.Rule.StopIfTrue;
        switch (entry.Rule)
        {
            case ConditionalRule.CellIs c:
                (_kind, _operator, _value1, _value2) = (RuleKind.CellValue, c.Operator, Show(c.Formula1), Show(c.Formula2));
                break;
            case ConditionalRule.Expression x:
                (_kind, _value1) = (RuleKind.Formula, "=" + x.Formula.TrimStart('='));
                break;
            case ConditionalRule.ContainsText t:
                (_kind, _textKind, _value1) = (RuleKind.Text, t.Kind, t.Text ?? "");
                break;
            case ConditionalRule.TimePeriod p:
                (_kind, _period) = (RuleKind.Date, p.Period);
                break;
            case ConditionalRule.Top10 t:
                (_kind, _rank, _percent, _bottom) = (RuleKind.TopBottom, t.Rank, t.Percent, t.Bottom);
                break;
            case ConditionalRule.AboveAverage a:
                (_kind, _below) = (RuleKind.Average, a.Below);
                break;
            case ConditionalRule.Duplicate d:
                (_kind, _unique) = (RuleKind.Duplicate, d.Unique);
                break;
            case ConditionalRule.ColorScale s:
                _kind = RuleKind.ColorScale;
                _threeColors = s.Stops.Count == 3;
                (_colors[0], _colors[2]) = (s.Stops[0].Color, s.Stops[^1].Color);
                if (_threeColors) _colors[1] = s.Stops[1].Color;
                break;
            case ConditionalRule.DataBar b:
                (_kind, _colors[3]) = (RuleKind.DataBar, b.Color);
                break;
            case ConditionalRule.IconSet i:
                (_kind, _iconSet, _reverse, _iconOnly) = (RuleKind.IconSet, i.Set, i.Reverse, !i.ShowValue);
                break;
        }

        if (entry.Rule.Format is { } style)
        {
            _preset = Presets.Length;
            (_colors[4], _colors[5]) = (style.Font?.Color, style.Fill?.Color);
            (_bold, _italic) = (style.Font?.Bold == true, style.Font?.Italic == true);
        }
    }

    private static string Show(string formula) => formula == null ? "" : formula.Length >= 2 && formula[0] == '"' && formula[^1] == '"'
        ? formula[1..^1].Replace("\"\"", "\"")
        : double.TryParse(formula, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ? formula : "=" + formula.TrimStart('=');

    private static string Value(string text)
    {
        text = text.Trim();
        if (text.StartsWith('=')) return text[1..];
        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var n)
            ? n.ToString("R", CultureInfo.InvariantCulture)
            : "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    private IReadOnlyList<CellRange> ParseRanges()
    {
        var parts = _appliesTo.Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries);
        var ranges = new List<CellRange>();
        foreach (var part in parts)
            if (CellRange.TryParse(part.Replace("$", ""), out var range)) ranges.Add(range.Normalized);
            else return null;
        return ranges.Count == 0 ? null : ranges;
    }

    private DifferentialStyle Format()
    {
        if (_preset < Presets.Length)
        {
            var (_, font, fill) = Presets[_preset];
            return new DifferentialStyle(font == null ? null : new DifferentialFont { Color = font }, fill == null ? null : new CellFill { Color = fill });
        }

        var hasFont = _colors[4] != null || _bold || _italic;
        return new DifferentialStyle(hasFont ? new DifferentialFont { Color = _colors[4], Bold = _bold ? true : null, Italic = _italic ? true : null } : null,
            _colors[5] == null ? null : new CellFill { Color = _colors[5] });
    }

    private ConditionalRule Build()
    {
        var style = Format();
        ConditionalRule rule = _kind switch
        {
            RuleKind.CellValue => new ConditionalRule.CellIs(_operator, Value(_value1), _operator is "between" or "notBetween" ? Value(_value2) : null, style),
            RuleKind.Text => new ConditionalRule.ContainsText(_textKind, _value1, style),
            RuleKind.Date => new ConditionalRule.TimePeriod(_period, style),
            RuleKind.TopBottom => new ConditionalRule.Top10(Math.Max(1, _rank), _percent, _bottom, style),
            RuleKind.Average => new ConditionalRule.AboveAverage(_below, false, 0, style),
            RuleKind.Duplicate => new ConditionalRule.Duplicate(_unique, style),
            RuleKind.Formula => new ConditionalRule.Expression(_value1.Trim().TrimStart('='), style),
            RuleKind.ColorScale => new ConditionalRule.ColorScale(_threeColors
                ? [new(new ConditionalValue(ConditionalValueType.Min), _colors[0]), new(new ConditionalValue(ConditionalValueType.Percentile, "50"), _colors[1]),
                    new(new ConditionalValue(ConditionalValueType.Max), _colors[2])]
                : [new(new ConditionalValue(ConditionalValueType.Min), _colors[0]), new(new ConditionalValue(ConditionalValueType.Max), _colors[2])]),
            RuleKind.DataBar => new ConditionalRule.DataBar(new ConditionalValue(ConditionalValueType.Min), new ConditionalValue(ConditionalValueType.Max), _colors[3]),
            _ => IconRule()
        };
        return rule with { Priority = _editing?.Rule.Priority ?? 1, StopIfTrue = _stop };
    }

    private ConditionalRule IconRule()
    {
        var count = _iconSet[0] - '0';
        var thresholds = Enumerable.Range(0, count)
            .Select(i => new ConditionalValue(ConditionalValueType.Percent, Math.Round(100.0 * i / count).ToString(CultureInfo.InvariantCulture)))
            .ToList();
        if (_editing?.Rule is ConditionalRule.IconSet old && old.Set == _iconSet) thresholds = old.Thresholds.ToList();
        return new ConditionalRule.IconSet(_iconSet, thresholds, _reverse, !_iconOnly);
    }

    private void Ok()
    {
        if (ParseRanges() is not { } ranges) return;
        var rule = Build();
        if (_editing is not { } e)
        {
            Close(new AddConditionalFormattingCommand(Sheet, new ConditionalFormatting(ranges, [rule])));
            return;
        }

        var cf = Sheet.ConditionalFormats[e.FormatIndex];
        var rules = cf.Rules.ToList();
        rules[e.RuleIndex] = rule;
        Close(new EditConditionalFormattingCommand(Sheet, e.FormatIndex, cf with { Ranges = ranges, Rules = rules }));
    }

    private void Delete(Entry e)
    {
        var cf = Sheet.ConditionalFormats[e.FormatIndex];
        Close(cf.Rules.Count == 1
            ? new DeleteConditionalFormattingCommand(Sheet, e.FormatIndex)
            : new EditConditionalFormattingCommand(Sheet, e.FormatIndex, cf with { Rules = cf.Rules.Where((_, i) => i != e.RuleIndex).ToList() }));
    }

    private void Close(IWorkbookCommand command) => MudDialog.Close(DialogResult.Ok(command));

    private string Describe(ConditionalRule rule) => rule switch
    {
        ConditionalRule.CellIs c => TryLocalize("Cell Value {0} {1}", TryLocalize(Operators.FirstOrDefault(o => o.Operator == c.Operator).Name ?? c.Operator),
            c.Formula2 == null ? Show(c.Formula1) : Show(c.Formula1) + " - " + Show(c.Formula2)),
        ConditionalRule.Expression x => TryLocalize("Formula: {0}", "=" + x.Formula.TrimStart('=')),
        ConditionalRule.ContainsText t => TryLocalize("Specific text: {0} {1}", TryLocalize(TextKinds.FirstOrDefault(k => k.Kind == t.Kind).Name), t.Text),
        ConditionalRule.TimePeriod p => TryLocalize("Date occurring: {0}", TryLocalize(Periods.FirstOrDefault(x => x.Period == p.Period).Name ?? p.Period)),
        ConditionalRule.Top10 t => TryLocalize("{0} {1}{2}", TryLocalize(t.Bottom ? "Bottom" : "Top"), t.Rank, t.Percent ? "%" : ""),
        ConditionalRule.AboveAverage a => TryLocalize(a.Below ? "Below Average" : "Above Average"),
        ConditionalRule.Duplicate d => TryLocalize(d.Unique ? "Unique Values" : "Duplicate Values"),
        ConditionalRule.ColorScale => TryLocalize("Graded Color Scale"),
        ConditionalRule.DataBar => TryLocalize("Data Bar"),
        ConditionalRule.IconSet => TryLocalize("Icon Set"),
        _ => TryLocalize("Rule")
    };

    private static string Preview(ConditionalRule rule) => rule switch
    {
        ConditionalRule.ColorScale s => $"background:linear-gradient(90deg,{string.Join(",", s.Stops.Select(x => Safe(x.Color) ?? "transparent"))})",
        ConditionalRule.DataBar b => $"background:linear-gradient(90deg,{Safe(b.Color) ?? "#638EC6"} 60%,transparent 60%)",
        _ => StylePreview(rule.Format)
    };

    private static string StylePreview(DifferentialStyle style) =>
        style == null ? "" : $"{(Safe(style.Fill?.Color) is { } fill ? "background:" + fill + ";" : "")}{(Safe(style.Font?.Color) is { } color ? "color:" + color + ";" : "")}"
                             + $"{(style.Font?.Bold == true ? "font-weight:700;" : "")}{(style.Font?.Italic == true ? "font-style:italic;" : "")}";

    private static string Safe(string color) => color != null && HexColor.IsMatch(color) ? color : null;

    private static string ColorOf(ChangeEventArgs e) => e.Value?.ToString() is { } value && HexColor.IsMatch(value) ? value.ToUpperInvariant() : null;

    private void Cancel() => MudDialog.Cancel();
}
