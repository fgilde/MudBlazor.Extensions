using System.Globalization;
using System.Xml.Linq;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Office;

/// <summary>Reads the cached values of a DrawingML chart part.</summary>
public static class ChartSnapshotReader
{
    private const int MaxPoints = 100_000;
    private static readonly XNamespace C = "http://schemas.openxmlformats.org/drawingml/2006/chart";
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly string[] Fallback = ["#4472C4", "#ED7D31", "#A5A5A5", "#FFC000", "#5B9BD5", "#70AD47"];

    private static readonly Dictionary<string, ChartKind> Kinds = new()
    {
        ["barChart"] = ChartKind.Column, ["lineChart"] = ChartKind.Line, ["pieChart"] = ChartKind.Pie, ["areaChart"] = ChartKind.Area, ["scatterChart"] = ChartKind.Scatter
    };

    /// <summary>Reads kind, title and the cached values of a chart part.</summary>
    /// <param name="chartPart">The chart part.</param>
    /// <param name="resolveColor">Resolves a color written as <c>#RRGGBB</c> or <c>scheme:name</c> to <c>#RRGGBB</c>, or null.</param>
    /// <returns>The snapshot, or null for chart types that cannot be drawn and for charts without cached values.</returns>
    public static ChartSnapshot Read(XDocument chartPart, Func<string, string> resolveColor)
    {
        var chart = chartPart?.Root?.Element(C + "chart");
        var types = chart?.Element(C + "plotArea")?.Elements().Where(e => e.Name.Namespace == C && e.Name.LocalName.EndsWith("Chart", StringComparison.Ordinal)).ToList() ?? [];
        if (types.Count != 1 || !Kinds.TryGetValue(types[0].Name.LocalName, out var kind)) return null;
        if (kind == ChartKind.Column && (string)types[0].Element(C + "barDir")?.Attribute("val") == "bar") kind = ChartKind.Bar;
        var sers = types[0].Elements(C + "ser").ToList();
        var valueCaches = sers.Select(s => Cache((s.Element(C + "val") ?? s.Element(C + "yVal"))?.Elements().FirstOrDefault())).ToList();
        if (valueCaches.All(v => v == null)) return null;
        var series = sers.Select((s, i) => new ChartSnapshotSeries(
            Name(s), (valueCaches[i] ?? []).Select(v => double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? (double?)d : null).ToList(),
            Color(s, i, resolveColor))).ToList();
        var categories = sers.Select(s => Cache((s.Element(C + "cat") ?? s.Element(C + "xVal"))?.Elements().FirstOrDefault())).FirstOrDefault(c => c != null) ?? [];
        return new ChartSnapshot(kind, Title(chart, series), categories, series, chart.Element(C + "legend") != null)
        {
            Stacked = (string)types[0].Element(C + "grouping")?.Attribute("val") is "stacked" or "percentStacked"
        };
    }

    private static string Name(XElement ser) =>
        Cache(ser.Element(C + "tx")?.Element(C + "strRef"))?.FirstOrDefault() ?? ser.Element(C + "tx")?.Element(C + "v")?.Value ?? "";

    private static string Title(XElement chart, List<ChartSnapshotSeries> series)
    {
        if (chart.Element(C + "title") is not { } title) return "";
        var tx = title.Element(C + "tx");
        if (tx?.Element(C + "rich") is { } rich)
            return string.Join("\n", rich.Elements(A + "p").Select(p => string.Concat(p.Descendants(A + "t").Select(t => t.Value))));
        if (tx?.Element(C + "strRef") is { } reference) return Cache(reference)?.FirstOrDefault() ?? "";
        return series.Count == 1 ? series[0].Name : "";
    }

    private static string Color(XElement ser, int index, Func<string, string> resolveColor)
    {
        var spPr = ser.Element(C + "spPr");
        var fill = spPr?.Element(A + "solidFill") ?? spPr?.Element(A + "ln")?.Element(A + "solidFill");
        var explicitColor = ColorResolver.FromXml(fill?.Elements().FirstOrDefault());
        return (explicitColor == null ? null : resolveColor(explicitColor))
               ?? resolveColor($"scheme:accent{index % 6 + 1}") ?? Fallback[index % Fallback.Length];
    }

    private static List<string> Cache(XElement reference)
    {
        var cache = reference?.Elements().FirstOrDefault(e => e.Name.Namespace == C && e.Name.LocalName is "strCache" or "numCache");
        if (cache == null) return null;
        var points = cache.Elements(C + "pt").ToList();
        var count = int.TryParse((string)cache.Element(C + "ptCount")?.Attribute("val"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
            ? Math.Clamp(n, 0, MaxPoints)
            : points.Count;
        var list = Enumerable.Repeat("", count).ToList();
        foreach (var pt in points)
            if (int.TryParse((string)pt.Attribute("idx"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) && i >= 0 && i < count)
                list[i] = pt.Element(C + "v")?.Value ?? "";
        return list;
    }
}
