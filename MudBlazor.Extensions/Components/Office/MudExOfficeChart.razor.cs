using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Shows a <see cref="ChartSnapshot"/>: column, line, pie and scatter charts with <see cref="MudChart{T}"/>, and area and horizontal bar charts as SVG.
/// It renders again only when the snapshot or the size changed.
/// </summary>
public partial class MudExOfficeChart
{
    private const double TitleHeight = 24;
    private const double LegendHeight = 22;
    private static readonly string[] DefaultColors = ["#4472C4", "#ED7D31", "#A5A5A5", "#FFC000", "#5B9BD5", "#70AD47", "#264478", "#9E480E", "#636363", "#997300"];

    private ChartSnapshot _snapshot;
    private (double Width, double Height) _size;
    private List<(string Name, double[] Values)> _values = [];
    private bool _changed = true;
    private List<ChartSeries<double>> _series = [];
    private string[] _labels = [];
    private ChartOptions _options = new();
    private (double Width, double Height) _plot;
    private readonly List<SvgShape> _shapes = [];
    private readonly List<SvgLine> _lines = [];
    private readonly List<SvgText> _texts = [];

    private readonly record struct SvgShape(string Path, double X, double Y, double Width, double Height, string Color);

    private readonly record struct SvgLine(double X1, double Y1, double X2, double Y2);

    private readonly record struct SvgText(double X, double Y, string Text, string Anchor);

    /// <summary>The chart to show. A new instance renders the chart again.</summary>
    [Parameter] public ChartSnapshot Snapshot { get; set; }

    /// <summary>Width in pixels.</summary>
    [Parameter] public double Width { get; set; }

    /// <summary>Height in pixels.</summary>
    [Parameter] public double Height { get; set; }

    private ChartType? MudType => Snapshot?.Kind switch
    {
        ChartKind.Column => Snapshot.Stacked ? ChartType.StackedBar : ChartType.Bar,
        ChartKind.Line => ChartType.Line,
        ChartKind.Pie => ChartType.Pie,
        ChartKind.Scatter => ChartType.ScatterPlot,
        _ => null
    };

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (ReferenceEquals(Snapshot, _snapshot) && _size == (Width, Height)) return;
        _snapshot = Snapshot;
        _size = (Width, Height);
        _changed = true;
        if (Snapshot == null) return;
        _values = [.. Snapshot.Series.Select(x => (x.Name, x.Values.Select(v => v ?? double.NaN).ToArray()))];
        if (MudType is { } type) BuildMudChart(type);
        else BuildSvg();
    }

    /// <inheritdoc />
    protected override bool ShouldRender()
    {
        var changed = _changed;
        _changed = false;
        return changed;
    }

    private RenderFragment Labels => builder =>
    {
        foreach (var label in _texts)
        {
            builder.OpenElement(0, "text");
            builder.AddAttribute(1, "class", "mud-ex-sschart-label");
            builder.AddAttribute(2, "x", F(label.X));
            builder.AddAttribute(3, "y", F(label.Y));
            builder.AddAttribute(4, "text-anchor", label.Anchor);
            builder.AddContent(5, label.Text);
            builder.CloseElement();
        }
    };

    private string Color(int index) => Snapshot.Series[index].Color;

    private static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Number(double value) => value.ToString("G4", CultureInfo.CurrentCulture);

    private void BuildMudChart(ChartType type)
    {
        var series = type == ChartType.Pie ? _values.Take(1) : _values;
        _labels = [.. Snapshot.Categories];
        _series = series.Select(s => new ChartSeries<double>
        {
            Name = s.Name,
            Data = type == ChartType.ScatterPlot
                ? new ChartData<double>(s.Values.Select((v, i) => (X: ScatterX(i), Y: v)).Where(p => !double.IsNaN(p.Y)).ToList())
                : new ChartData<double>(s.Values.Select(v => double.IsNaN(v) ? (double?)null : v).ToList())
        }).ToList();
        _options = new ChartOptions
        {
            ShowLegend = Snapshot.ShowLegend,
            ChartPalette = type == ChartType.Pie
                ? DefaultColors
                : Enumerable.Range(0, _series.Count).Select(Color).ToArray()
        };
    }

    private double ScatterX(int index) =>
        index < Snapshot.Categories.Count && double.TryParse(Snapshot.Categories[index], NumberStyles.Any, CultureInfo.CurrentCulture, out var x) ? x : index + 1;

    private void BuildSvg()
    {
        _shapes.Clear();
        _lines.Clear();
        _texts.Clear();
        _plot = (Math.Max(40, Width), Math.Max(40, Height - (string.IsNullOrEmpty(Snapshot.Title) ? 0 : TitleHeight) - (Snapshot.ShowLegend ? LegendHeight : 0)));
        var count = _values.Select(s => s.Values.Length).DefaultIfEmpty(0).Max();
        if (count == 0) return;
        var values = _values.Select(s => Enumerable.Range(0, count).Select(i => i < s.Values.Length && !double.IsNaN(s.Values[i]) ? s.Values[i] : 0).ToArray()).ToList();
        var (low, high) = Bounds(values);
        if (Snapshot.Kind == ChartKind.Bar) BuildBars(values, count, low, high);
        else BuildAreas(values, count, low, high);
    }

    private (double Low, double High) Bounds(List<double[]> values)
    {
        double low = 0, high = 0;
        for (var i = 0; i < values[0].Length; i++)
        {
            double plus = 0, minus = 0;
            foreach (var s in values)
            {
                if (Snapshot.Stacked)
                {
                    if (s[i] >= 0) plus += s[i];
                    else minus += s[i];
                }
                else
                {
                    plus = Math.Max(plus, s[i]);
                    minus = Math.Min(minus, s[i]);
                }
            }

            high = Math.Max(high, plus);
            low = Math.Min(low, minus);
        }

        return high > low ? (low, high) : (low, low + 1);
    }

    private void BuildAreas(List<double[]> values, int count, double low, double high)
    {
        const double left = 44, right = 10, top = 8, bottom = 20;
        var w = _plot.Width - left - right;
        var h = _plot.Height - top - bottom;
        double X(int i) => left + (count == 1 ? w / 2 : i * w / (count - 1));
        double Y(double v) => top + (high - v) / (high - low) * h;
        var baseLine = Enumerable.Repeat(0d, count).ToArray();
        for (var s = 0; s < values.Count; s++)
        {
            var upper = Snapshot.Stacked ? baseLine.Select((b, i) => b + values[s][i]).ToArray() : values[s];
            var lower = Snapshot.Stacked ? baseLine : new double[count];
            var path = "M" + string.Join(" L", Enumerable.Range(0, count).Select(i => $"{F(X(i))},{F(Y(upper[i]))}"))
                       + " L" + string.Join(" L", Enumerable.Range(0, count).Reverse().Select(i => $"{F(X(i))},{F(Y(lower[i]))}")) + " Z";
            _shapes.Add(new SvgShape(path, 0, 0, 0, 0, Color(s)));
            if (Snapshot.Stacked) baseLine = upper;
        }

        _lines.Add(new SvgLine(left, Y(0), left + w, Y(0)));
        _lines.Add(new SvgLine(left, top, left, top + h));
        _texts.Add(new SvgText(left - 4, top + 8, Number(high), "end"));
        _texts.Add(new SvgText(left - 4, top + h, Number(low), "end"));
        var step = Math.Max(1, (int)Math.Ceiling(count * 60 / Math.Max(1, w)));
        for (var i = 0; i < count; i += step)
            _texts.Add(new SvgText(X(i), top + h + 14, Category(i), "middle"));
    }

    private void BuildBars(List<double[]> values, int count, double low, double high)
    {
        const double left = 70, right = 10, top = 8, bottom = 20;
        var w = _plot.Width - left - right;
        var h = _plot.Height - top - bottom;
        double X(double v) => left + (v - low) / (high - low) * w;
        var band = h / count;
        var thickness = band * 0.7 / (Snapshot.Stacked ? 1 : values.Count);
        for (var i = 0; i < count; i++)
        {
            var y0 = top + h - (i + 1) * band + band * 0.15;
            double plus = 0, minus = 0;
            for (var s = 0; s < values.Count; s++)
            {
                var v = values[s][i];
                var from = Snapshot.Stacked ? v >= 0 ? plus : minus : 0;
                var to = from + v;
                if (Snapshot.Stacked && v >= 0) plus = to;
                else if (Snapshot.Stacked) minus = to;
                var y = Snapshot.Stacked ? y0 : y0 + s * thickness;
                _shapes.Add(new SvgShape(null, X(Math.Min(from, to)), y, Math.Abs(X(to) - X(from)), thickness, Color(s)));
            }

            _texts.Add(new SvgText(left - 4, top + h - i * band - band / 2 + 4, Category(i), "end"));
        }

        _lines.Add(new SvgLine(X(0), top, X(0), top + h));
        _lines.Add(new SvgLine(left, top + h, left + w, top + h));
        _texts.Add(new SvgText(left, top + h + 14, Number(low), "start"));
        _texts.Add(new SvgText(left + w, top + h + 14, Number(high), "end"));
    }

    private string Category(int index) => index < Snapshot.Categories.Count ? Snapshot.Categories[index] : (index + 1).ToString(CultureInfo.CurrentCulture);
}
