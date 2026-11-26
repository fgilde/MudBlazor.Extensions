using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The paragraph dialog of <see cref="MudExDocumentEditor"/>: alignment, indents, spacing, line spacing and the line and page break options.
/// Indents are shown in centimetres or inches, spacing in points. A property that differs across the selection starts empty. Closes with a
/// <c>Func&lt;ParagraphFormat, ParagraphFormat&gt;</c> that changes only the properties the user changed.
/// </summary>
public partial class MudExDocumentParagraphDialog
{
    private const double MaxPoints = DocumentCss.MaxTwips / 20.0;
    private const int DefaultSpecialTwips = 720;

    private static readonly (string Value, string Text)[] Specials = [("none", "(none)"), ("firstLine", "First line"), ("hanging", "Hanging")];

    private static readonly (string Value, string Text)[] Lines =
        [("single", "Single"), ("onePointFive", "1.5 lines"), ("double", "Double"), ("atLeast", "At least"), ("exactly", "Exactly"), ("multiple", "Multiple")];

    private ParagraphFormat _initial;
    private ParagraphAlignment? _alignment;
    private double? _left;
    private double? _right;
    private string _special;
    private double? _by;
    private double? _spaceBefore;
    private double? _spaceAfter;
    private string _line;
    private double? _at;
    private bool? _widow;
    private bool? _keepNext;
    private bool? _keepLines;
    private bool? _pageBreak;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The merged paragraph formatting of the selection; a null property differs across the selection.</summary>
    [Parameter] public ParagraphFormat Format { get; set; }

    /// <summary>True to show lengths in centimetres, false for inches.</summary>
    [Parameter] public bool Metric { get; set; } = true;

    private string Unit => DocumentUnits.Unit(Metric);

    private double MaxLength => DocumentUnits.FromTwips(DocumentCss.MaxTwips, Metric);

    private double LengthStep => Metric ? 0.1 : 0.05;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        var p = Format ?? new ParagraphFormat();
        (_alignment, _left, _right) = (p.Alignment, Length(p.IndentLeft), Length(p.IndentRight));
        (_special, _by) = p.IndentFirstLine switch
        {
            null => ((string)null, (double?)null),
            0 => ("none", null),
            > 0 and var first => ("firstLine", Length(first)),
            var hanging => ("hanging", Length(-hanging))
        };
        (_spaceBefore, _spaceAfter) = (p.SpacingBefore / 20.0, p.SpacingAfter / 20.0);
        (_line, _at) = p.Line switch
        {
            null => ((string)null, (double?)null),
            { Rule: LineRule.Auto, Value: 240 } => ("single", null),
            { Rule: LineRule.Auto, Value: 360 } => ("onePointFive", null),
            { Rule: LineRule.Auto, Value: 480 } => ("double", null),
            { Rule: LineRule.Auto } line => ("multiple", Math.Round(line.Value / 240.0, 2)),
            { Rule: LineRule.AtLeast } line => ("atLeast", line.Value / 20.0),
            { } line => ("exactly", line.Value / 20.0)
        };
        (_widow, _keepNext, _keepLines, _pageBreak) = (p.WidowControl, p.KeepWithNext, p.KeepLines, p.PageBreakBefore);
        _initial = After;
    }

    internal static Func<ParagraphFormat, ParagraphFormat> Changes(ParagraphFormat before, ParagraphFormat after) => p => p with
    {
        Alignment = MudExDocumentFontDialog.Pick(before.Alignment, after.Alignment, p.Alignment),
        IndentLeft = MudExDocumentFontDialog.Pick(before.IndentLeft, after.IndentLeft, p.IndentLeft),
        IndentRight = MudExDocumentFontDialog.Pick(before.IndentRight, after.IndentRight, p.IndentRight),
        IndentFirstLine = MudExDocumentFontDialog.Pick(before.IndentFirstLine, after.IndentFirstLine, p.IndentFirstLine),
        SpacingBefore = MudExDocumentFontDialog.Pick(before.SpacingBefore, after.SpacingBefore, p.SpacingBefore),
        SpacingAfter = MudExDocumentFontDialog.Pick(before.SpacingAfter, after.SpacingAfter, p.SpacingAfter),
        Line = MudExDocumentFontDialog.Pick(before.Line, after.Line, p.Line),
        WidowControl = MudExDocumentFontDialog.Pick(before.WidowControl, after.WidowControl, p.WidowControl),
        KeepWithNext = MudExDocumentFontDialog.Pick(before.KeepWithNext, after.KeepWithNext, p.KeepWithNext),
        KeepLines = MudExDocumentFontDialog.Pick(before.KeepLines, after.KeepLines, p.KeepLines),
        PageBreakBefore = MudExDocumentFontDialog.Pick(before.PageBreakBefore, after.PageBreakBefore, p.PageBreakBefore)
    };

    private ParagraphFormat After => new()
    {
        Alignment = _alignment,
        IndentLeft = Twips(_left, -DocumentCss.MaxTwips),
        IndentRight = Twips(_right, -DocumentCss.MaxTwips),
        IndentFirstLine = _special switch
        {
            "none" => 0,
            "firstLine" => Twips(_by, 0) ?? DefaultSpecialTwips,
            "hanging" => -(Twips(_by, 0) ?? DefaultSpecialTwips),
            _ => null
        },
        SpacingBefore = Points(_spaceBefore),
        SpacingAfter = Points(_spaceAfter),
        Line = _line switch
        {
            "single" => new LineSpacing(240, LineRule.Auto),
            "onePointFive" => new LineSpacing(360, LineRule.Auto),
            "double" => new LineSpacing(480, LineRule.Auto),
            "multiple" => new LineSpacing(Finite(_at) is { } lines ? (int)Math.Round(Math.Clamp(lines * 240, 15, DocumentCss.MaxTwips)) : 720, LineRule.Auto),
            "atLeast" => new LineSpacing(Points(_at) ?? 240, LineRule.AtLeast),
            "exactly" => new LineSpacing(Points(_at) ?? 240, LineRule.Exact),
            _ => null
        },
        WidowControl = _widow,
        KeepWithNext = _keepNext,
        KeepLines = _keepLines,
        PageBreakBefore = _pageBreak
    };

    private double? Length(int? twips) => twips is { } value ? DocumentUnits.FromTwips(value, Metric) : null;

    private int? Twips(double? value, int min) => Finite(value) is { } v ? Math.Clamp(DocumentUnits.ToTwips(v, Metric), min, DocumentCss.MaxTwips) : null;

    private static int? Points(double? value) => Finite(value) is { } v ? (int)Math.Round(Math.Clamp(v * 20, 0, DocumentCss.MaxTwips)) : null;

    private static double? Finite(double? value) => value is { } v && double.IsFinite(v) ? v : null;

    private void Ok() => MudDialog.Close(DialogResult.Ok(Changes(_initial, After)));

    private void Cancel() => MudDialog.Cancel();
}
