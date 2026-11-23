using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The font dialog of <see cref="MudExDocumentEditor"/>: font, size, underline style, colour and the character effects. A property that differs
/// across the selection starts empty. Closes with a <c>Func&lt;RunFormat, RunFormat&gt;</c> that changes only the properties the user changed.
/// </summary>
public partial class MudExDocumentFontDialog
{
    private static readonly (string Value, string Text)[] Underlines =
        [("none", "(none)"), ("single", "Single"), ("double", "Double"), ("thick", "Thick"), ("dotted", "Dotted"), ("dash", "Dashed"), ("wave", "Wave"), ("words", "Words only")];

    private RunFormat _before;
    private string _font;
    private double? _size;
    private string _underline;
    private string _color;
    private bool? _bold;
    private bool? _italic;
    private bool? _strike;
    private bool? _doubleStrike;
    private bool? _superscript;
    private bool? _subscript;
    private bool? _smallCaps;
    private bool? _caps;
    private bool? _hidden;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The merged character formatting of the selection; a null property differs across the selection.</summary>
    [Parameter] public RunFormat Format { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        var f = Format ?? new RunFormat();
        (_font, _size, _underline, _color) = (f.Font, f.SizePt, f.Underline, DocumentCss.Color(f.Color));
        (_bold, _italic, _strike, _doubleStrike, _smallCaps, _caps, _hidden) = (f.Bold, f.Italic, f.Strike, f.DoubleStrike, f.SmallCaps, f.Caps, f.Hidden);
        (_superscript, _subscript) = (f.VerticalAlign == null ? null : f.VerticalAlign == VerticalAlign.Superscript, f.VerticalAlign == null ? null : f.VerticalAlign == VerticalAlign.Subscript);
        _before = After;
    }

    internal static Func<RunFormat, RunFormat> Changes(RunFormat before, RunFormat after) => f => f with
    {
        Font = Pick(before.Font, after.Font, f.Font),
        SizePt = Pick(before.SizePt, after.SizePt, f.SizePt),
        Underline = Pick(before.Underline, after.Underline, f.Underline),
        Color = Pick(before.Color, after.Color, f.Color),
        Bold = Pick(before.Bold, after.Bold, f.Bold),
        Italic = Pick(before.Italic, after.Italic, f.Italic),
        Strike = Pick(before.Strike, after.Strike, f.Strike),
        DoubleStrike = Pick(before.DoubleStrike, after.DoubleStrike, f.DoubleStrike),
        VerticalAlign = Pick(before.VerticalAlign, after.VerticalAlign, f.VerticalAlign),
        SmallCaps = Pick(before.SmallCaps, after.SmallCaps, f.SmallCaps),
        Caps = Pick(before.Caps, after.Caps, f.Caps),
        Hidden = Pick(before.Hidden, after.Hidden, f.Hidden)
    };

    internal static T Pick<T>(T before, T after, T current) => after is not null && !EqualityComparer<T>.Default.Equals(before, after) ? after : current;

    private RunFormat After => new()
    {
        Font = _font?.Trim() is { Length: > 0 and <= 64 } font ? font : null,
        SizePt = _size is { } size && double.IsFinite(size) ? Math.Clamp(Math.Round(size * 2) / 2, 1, 1638) : null,
        Underline = Underlines.Any(u => u.Value == _underline) ? _underline : null,
        Color = DocumentCss.Color(_color),
        Bold = _bold,
        Italic = _italic,
        Strike = _strike,
        DoubleStrike = _doubleStrike,
        VerticalAlign = _superscript == true ? VerticalAlign.Superscript : _subscript == true ? VerticalAlign.Subscript : _superscript == false && _subscript == false ? VerticalAlign.Baseline : null,
        SmallCaps = _smallCaps,
        Caps = _caps,
        Hidden = _hidden
    };

    private Task<IEnumerable<string>> SearchFontsAsync(string text, CancellationToken token) =>
        Task.FromResult(string.IsNullOrWhiteSpace(text) || text == _before.Font
            ? MudExNotebookEditor.Fonts.AsEnumerable()
            : MudExNotebookEditor.Fonts.Where(f => f.Contains(text.Trim(), StringComparison.OrdinalIgnoreCase)));

    private void SetColor(ChangeEventArgs e) => _color = DocumentCss.Color(e.Value as string) ?? _color;

    private void SetSuperscript(bool? value)
    {
        _superscript = value;
        if (value == true)
            _subscript = false;
    }

    private void SetSubscript(bool? value)
    {
        _subscript = value;
        if (value == true)
            _superscript = false;
    }

    private void Ok() => MudDialog.Close(DialogResult.Ok(Changes(_before, After)));

    private void Cancel() => MudDialog.Cancel();
}
