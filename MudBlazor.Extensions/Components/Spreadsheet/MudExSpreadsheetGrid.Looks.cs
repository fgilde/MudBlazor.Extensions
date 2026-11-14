using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components.Rendering;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

public partial class MudExSpreadsheetGrid
{
    private readonly Dictionary<CellStyle, CellLook> _composedLooks = new();
    private ConditionalFormatEvaluator _conditional;
    private Worksheet _conditionalSheet;

    private ConditionalFormatEvaluator Conditional
    {
        get
        {
            if (_conditional != null && ReferenceEquals(_conditionalSheet, _sheet)) return _conditional;
            _conditional?.Dispose();
            _conditionalSheet = _sheet;
            return _conditional = new ConditionalFormatEvaluator(_workbook, _sheet);
        }
    }

    private void DisposeConditional()
    {
        _conditional?.Dispose();
        _conditional = null;
        _conditionalSheet = null;
    }

    private void ClearLooks()
    {
        _looks.Clear();
        _composedLooks.Clear();
    }

    private bool HasRules => _sheet.ConditionalFormats.Count > 0;

    private CellLook LookAt(CellRef at, int styleId, out ConditionalResult conditional)
    {
        conditional = HasRules ? Conditional.Evaluate(at) : null;
        var table = _sheet.Tables.Count > 0 ? _sheet.TableAt(at) : null;
        var layer = SpreadsheetTableStyles.Format(table, at);
        if (layer == null && conditional?.Style == null) return Look(styleId);
        var style = _workbook.Styles[styleId];
        if (layer != null) style = Beneath(layer, style).ApplyTo(style);
        if (conditional?.Style is { } dxf) style = dxf.ApplyTo(style);
        if (_composedLooks.TryGetValue(style, out var look)) return look;
        if (_composedLooks.Count > 4096) _composedLooks.Clear();
        return _composedLooks[style] = BuildLook(style);
    }

    private static DifferentialStyle Beneath(DifferentialStyle layer, CellStyle own)
    {
        static CellBorderSide Free(CellBorderSide side, CellBorderSide mine) => mine is { Style: not CellBorderStyle.None } ? null : side;
        return layer with
        {
            Fill = own.Fill.Color != null ? null : layer.Fill,
            Font = layer.Font == null || own.Font.Color == null ? layer.Font : layer.Font with { Color = null },
            Border = layer.Border == null ? null : new DifferentialBorder
            {
                Top = Free(layer.Border.Top, own.Border.Top), Bottom = Free(layer.Border.Bottom, own.Border.Bottom),
                Left = Free(layer.Border.Left, own.Border.Left), Right = Free(layer.Border.Right, own.Border.Right)
            }
        };
    }

    private static string ConditionalBox(ConditionalResult cf)
    {
        if (cf == null) return "";
        var box = new StringBuilder();
        if (SafeColor(cf.ScaleColor) is { } scale) box.Append("background:").Append(scale).Append(';');
        if (cf.BarFraction is { } fraction)
        {
            var color = SafeColor(cf.BarColor) ?? "#638EC6";
            var pct = (fraction * 100).ToString("0.##", CultureInfo.InvariantCulture);
            var end = color.Length == 7 ? color + "55" : color;
            box.Append("background-image:linear-gradient(90deg,").Append(color).Append(" 0%,").Append(end).Append(' ').Append(pct).Append("%,transparent ")
                .Append(pct).Append("%);");
        }

        return box.ToString();
    }

    private static void BuildIcon(RenderTreeBuilder b, string id)
    {
        if (IconOf(id) is not var (icon, color)) return;
        b.OpenComponent<MudIcon>(0);
        b.AddComponentParameter(1, nameof(MudIcon.Icon), icon);
        b.AddComponentParameter(2, nameof(MudIcon.Class), "mud-ex-ssg-icon");
        b.AddComponentParameter(3, nameof(MudIcon.Style), "color:" + color);
        b.CloseComponent();
    }

    internal static (string Icon, string Color)? IconOf(string id)
    {
        var colon = id?.LastIndexOf(':') ?? -1;
        if (colon < 1 || !int.TryParse(id.AsSpan(colon + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var index)) return null;
        var set = id[..colon];
        var count = char.IsDigit(set[0]) ? set[0] - '0' : 3;
        var level = count > 1 ? Math.Clamp(index, 0, count - 1) / (double)(count - 1) : 1;
        string[] ramp = count switch
        {
            5 => ["#C0504D", "#F79646", "#FFC000", "#9BBB59", "#00B050"],
            4 => ["#C0504D", "#FFC000", "#9BBB59", "#00B050"],
            _ => ["#C0504D", "#FFC000", "#00B050"]
        };
        var color = set.Contains("Gray", StringComparison.Ordinal) ? "#7F7F7F" : ramp[Math.Clamp(index, 0, ramp.Length - 1)];
        string[] arrows = count switch
        {
            5 => [Icons.Material.Filled.ArrowDownward, Icons.Material.Filled.SouthEast, Icons.Material.Filled.ArrowForward, Icons.Material.Filled.NorthEast, Icons.Material.Filled.ArrowUpward],
            4 => [Icons.Material.Filled.ArrowDownward, Icons.Material.Filled.SouthEast, Icons.Material.Filled.NorthEast, Icons.Material.Filled.ArrowUpward],
            _ => [Icons.Material.Filled.ArrowDownward, Icons.Material.Filled.ArrowForward, Icons.Material.Filled.ArrowUpward]
        };
        string Pick(params string[] icons) => icons[Math.Clamp((int)Math.Round(level * (icons.Length - 1)), 0, icons.Length - 1)];
        return set switch
        {
            _ when set.Contains("Arrows", StringComparison.Ordinal) => (arrows[Math.Clamp(index, 0, arrows.Length - 1)], color),
            _ when set.Contains("Triangles", StringComparison.Ordinal) => (Pick(Icons.Material.Filled.ArrowDropDown, Icons.Material.Filled.Remove, Icons.Material.Filled.ArrowDropUp), color),
            _ when set.Contains("Flags", StringComparison.Ordinal) => (Icons.Material.Filled.Flag, color),
            _ when set.Contains("Stars", StringComparison.Ordinal) => (Pick(Icons.Material.Filled.StarBorder, Icons.Material.Filled.StarHalf, Icons.Material.Filled.Star), "#FFC000"),
            _ when set.Contains("Symbols", StringComparison.Ordinal) => (Pick(Icons.Material.Filled.Cancel, Icons.Material.Filled.Error, Icons.Material.Filled.CheckCircle), color),
            _ when set.Contains("Rating", StringComparison.Ordinal) => (Pick(Icons.Material.Filled.SignalCellular0Bar, Icons.Material.Filled.SignalCellularAlt1Bar,
                Icons.Material.Filled.SignalCellularAlt2Bar, Icons.Material.Filled.SignalCellularAlt), "#4472C4"),
            _ when set.Contains("Quarters", StringComparison.Ordinal) || set.Contains("Boxes", StringComparison.Ordinal) =>
                (Pick(Icons.Material.Filled.RadioButtonUnchecked, Icons.Material.Filled.Contrast, Icons.Material.Filled.Circle), "#595959"),
            _ when set.Contains("RedToBlack", StringComparison.Ordinal) => (Icons.Material.Filled.Circle, Pick("#000000", "#7F7F7F", "#FF9999", "#C00000")),
            _ => (Icons.Material.Filled.Circle, color)
        };
    }

    private CellLook Look(int id)
    {
        if (_looks.TryGetValue(id, out var look)) return look;
        return _looks[id] = BuildLook(_workbook.Styles[id]);
    }

    private CellLook BuildLook(CellStyle s)
    {
        var box = new StringBuilder();
        if (SafeColor(s.Fill.Color) is { } fill) box.Append("background:").Append(fill).Append(';');
        Border(box, "left", s.Border.Left);
        Border(box, "right", s.Border.Right);
        Border(box, "top", s.Border.Top);
        Border(box, "bottom", s.Border.Bottom);
        var text = new StringBuilder();
        var f = s.Font;
        var family = SafeFont(f.Name);
        if (family.Length > 0 && family != "Calibri") text.Append("font-family:'").Append(family).Append("',Calibri,Carlito,sans-serif;");
        if (f.Size > 0 && Math.Abs(f.Size - 11) > 0.01) text.Append("font-size:").Append(Px(Math.Round(f.Size * 96 / 72 * _zoom, 2))).Append(';');
        if (f.Bold) text.Append("font-weight:700;");
        if (f.Italic) text.Append("font-style:italic;");
        if (f.Underline || f.Strikethrough)
            text.Append("text-decoration:").Append(f.Underline ? "underline" : "").Append(f.Underline && f.Strikethrough ? " " : "").Append(f.Strikethrough ? "line-through" : "").Append(';');
        if (SafeColor(f.Color) is { } color) text.Append("color:").Append(color).Append(';');
        if (s.Alignment.Indent > 0) text.Append("padding-left:").Append(Px((3 + s.Alignment.Indent * 9) * _zoom)).Append(';');
        return new CellLook(box.ToString(), text.ToString(), s.Alignment.Horizontal, s.Alignment.Vertical, s.Alignment.WrapText, s.NumberFormat);
    }

    private string FontShorthand(int styleId)
    {
        var f = _workbook.Styles[styleId].Font;
        var family = SafeFont(f.Name);
        return $"{(f.Italic ? "italic " : "")}{(f.Bold ? "700 " : "")}{Px(Math.Round((f.Size > 0 ? f.Size : 11) * 96 / 72, 2))} '{(family.Length > 0 ? family : "Calibri")}', Calibri, Carlito, sans-serif";
    }

    private static void Border(StringBuilder sb, string side, CellBorderSide b)
    {
        if (b == null || b.Style == CellBorderStyle.None) return;
        var (width, style) = b.Style switch
        {
            CellBorderStyle.Medium => (2, "solid"),
            CellBorderStyle.Thick => (3, "solid"),
            CellBorderStyle.Dashed => (1, "dashed"),
            CellBorderStyle.Dotted => (1, "dotted"),
            CellBorderStyle.Double => (3, "double"),
            _ => (1, "solid")
        };
        sb.Append("border-").Append(side).Append(':').Append(width).Append("px ").Append(style).Append(' ')
            .Append(SafeColor(b.Color) ?? "var(--mud-palette-text-primary)").Append(';');
    }

    internal static string SafeColor(string color) =>
        color is { Length: 7 or 9 } && color[0] == '#' && color.Skip(1).All(char.IsAsciiHexDigit) ? color : null;

    private static string SafeFont(string name) =>
        name == null ? "" : new string(name.Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_').ToArray()).Trim();

    private static string NamedColor(string name) => name switch
    {
        "Black" or "Blue" or "Cyan" or "Green" or "Magenta" or "Red" or "White" or "Yellow" => name.ToLowerInvariant(),
        _ => null
    };

    internal string DisplayText(CellRef at)
    {
        var cell = _sheet?[at];
        if (cell?.HasContent != true) return "";
        return NumberFormat.Format(cell.Value, _workbook.Styles[_sheet.EffectiveStyleId(at)].NumberFormat, Culture, out _);
    }

    private sealed record CellLook(string Box, string Text, CellHorizontalAlignment Horizontal, CellVerticalAlignment Vertical, bool Wrap, string Format);
}
