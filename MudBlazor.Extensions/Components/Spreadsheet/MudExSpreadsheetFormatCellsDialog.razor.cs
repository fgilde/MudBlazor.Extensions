using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The Format Cells dialog of <see cref="MudExSpreadsheetEditor"/> with the tabs Number, Alignment, Font, Border and Fill.
/// Closes with the edited <see cref="Office.Spreadsheet.CellStyle"/> as result data.
/// </summary>
public partial class MudExSpreadsheetFormatCellsDialog
{
    private const string Custom = "Custom";
    private static readonly string[] Categories =
        ["General", "Number", "Currency", "Accounting", "Short Date", "Long Date", "Time", "Percentage", "Fraction", "Scientific", "Text", Custom];

    private static readonly (string Name, Func<CellBorder, CellBorderSide> Get, Func<CellBorder, CellBorderSide, CellBorder> Set)[] Sides =
    [
        ("Left", b => b.Left, (b, s) => b with { Left = s }),
        ("Right", b => b.Right, (b, s) => b with { Right = s }),
        ("Top", b => b.Top, (b, s) => b with { Top = s }),
        ("Bottom", b => b.Bottom, (b, s) => b with { Bottom = s })
    ];

    private static readonly Regex HexColor = new("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant);

    private CellStyle _style = CellStyle.Default;
    private string _category = "General";
    private int _decimals = 2;
    private bool _thousands;
    private int _tab;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The style shown when the dialog opens, usually the style of the active cell.</summary>
    [Parameter] public CellStyle CellStyle { get; set; }

    /// <summary>Value shown in the number format sample.</summary>
    [Parameter] public CellValue Sample { get; set; } = CellValue.Number(1234.5678);

    private bool HasDecimals => _category is "Number" or "Currency" or "Accounting" or "Percentage" or "Scientific";

    private string Preview => NumberFormat.Format(Sample, _style.NumberFormat, CultureInfo.CurrentCulture, out _);

    private string BorderColor => Sides.Select(s => s.Get(_style.Border).Color).FirstOrDefault(c => c != null);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _style = CellStyle ?? CellStyle.Default;
        _category = Categories.FirstOrDefault(c => c != Custom && CodeOf(c, 2, false) == _style.NumberFormat)
                    ?? (_style.NumberFormat == "#,##0.00" ? "Number" : Custom);
        _thousands = _style.NumberFormat.Contains(',');
    }

    private static string CodeOf(string category, int decimals, bool thousands)
    {
        var code = MudExSpreadsheetEditor.NumberFormatCode(category);
        if (code == null)
            return null;
        if (category == "Number" && thousands)
            code = "#,##0.00";
        for (var i = 2; i > decimals; i--)
            code = MudExSpreadsheetEditor.ChangeDecimals(code, -1, default);
        for (var i = 2; i < decimals; i++)
            code = MudExSpreadsheetEditor.ChangeDecimals(code, 1, default);
        return code;
    }

    private void Rebuild()
    {
        if (CodeOf(_category, _decimals, _thousands) is { } code)
            _style = _style with { NumberFormat = code };
    }

    private void SetCategory(string category)
    {
        _category = category;
        _decimals = 2;
        Rebuild();
    }

    private void SetDecimals(int decimals)
    {
        _decimals = decimals;
        Rebuild();
    }

    private void SetThousands(bool thousands)
    {
        _thousands = thousands;
        Rebuild();
    }

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return;
        _style = _style with { NumberFormat = code };
        _category = Custom;
    }

    private void Align(Func<CellAlignment, CellAlignment> change) => _style = _style with { Alignment = change(_style.Alignment) };

    private void SetFont(Func<CellFont, CellFont> change) => _style = _style with { Font = change(_style.Font) };

    private void SetBorder(Func<CellBorder, CellBorder> change) => _style = _style with { Border = change(_style.Border) };

    private void SetBorderColor(string color) =>
        SetBorder(b => Sides.Aggregate(b, (border, side) => side.Set(border, side.Get(border) with { Color = color })));

    private static CellBorder AllSides(CellBorderStyle style)
    {
        var side = new CellBorderSide { Style = style };
        return new CellBorder { Left = side, Right = side, Top = side, Bottom = side };
    }

    private static string ColorOf(ChangeEventArgs e) => e.Value?.ToString() is { } value && HexColor.IsMatch(value) ? value.ToUpperInvariant() : null;

    private void Ok() => MudDialog.Close(DialogResult.Ok(_style));

    private void Cancel() => MudDialog.Cancel();
}
