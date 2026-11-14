using System.Globalization;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>A data series of a <see cref="SheetChart"/>. References are formula text without equals sign, for example <c>Sheet1!$B$2:$B$5</c>.</summary>
/// <param name="NameRef">The cell holding the series name, or null when <paramref name="Name"/> is literal text.</param>
/// <param name="Name">The series name as stored in the file; <see cref="ChartDataResolver"/> reads it from <paramref name="NameRef"/>.</param>
/// <param name="CategoriesRef">The category labels (the X values of a scatter chart), or null for 1, 2, 3 …</param>
/// <param name="ValuesRef">The values.</param>
/// <param name="Color">The fill or line color as #RRGGBB, or null for the default palette.</param>
public sealed record ChartSeries(string NameRef, string Name, string CategoriesRef, string ValuesRef, string Color)
{
    internal IReadOnlyList<string> CachedCategories { get; init; } = [];

    internal IReadOnlyList<double> CachedValues { get; init; } = [];
}

/// <summary>The values a chart shows, read from the cells its series reference.</summary>
/// <param name="Categories">The category labels, shared by all series.</param>
/// <param name="Series">Name and values of each series; a cell without a number gives <see cref="double.NaN"/>.</param>
public sealed record ChartData(IReadOnlyList<string> Categories, IReadOnlyList<(string Name, double[] Values)> Series);

/// <summary>Reads the data of a chart from the workbook.</summary>
public static class ChartDataResolver
{
    /// <summary>
    /// Reads names, categories and values of the chart's series from their cells. Formula cells give their calculated (or cached) values; nothing
    /// is recalculated. A reference that cannot be resolved, for example to another workbook, falls back to the values cached in the chart.
    /// </summary>
    /// <param name="wb">The workbook.</param>
    /// <param name="chart">The chart.</param>
    /// <returns>The data; empty for <see cref="ChartKind.Other"/>.</returns>
    public static ChartData Resolve(Workbook wb, SheetChart chart)
    {
        ArgumentNullException.ThrowIfNull(wb);
        ArgumentNullException.ThrowIfNull(chart);
        var host = wb.Sheets.FirstOrDefault(s => s.Charts.Contains(chart)) ?? wb.Sheets[0];
        List<string> categories = null;
        var series = new List<(string, double[])>();
        foreach (var s in chart.Series)
        {
            var name = s.NameRef != null && Cells(wb, host, s.NameRef) is [var first, ..] ? Display(wb, first) : s.Name;
            var values = s.ValuesRef != null && Cells(wb, host, s.ValuesRef) is { } cells
                ? cells.Select(c => c.Value.Kind == CellValueKind.Number ? c.Value.NumberValue : double.NaN).ToArray()
                : s.CachedValues.ToArray();
            series.Add((name, values));
            categories ??= s.CategoriesRef != null && Cells(wb, host, s.CategoriesRef) is { } labels ? labels.Select(c => Display(wb, c)).ToList()
                : s.CategoriesRef != null ? [.. s.CachedCategories]
                : null;
        }

        var count = series.Select(s => s.Item2.Length).DefaultIfEmpty(0).Max();
        return new ChartData(categories ?? Enumerable.Range(1, count).Select(i => i.ToString(CultureInfo.CurrentCulture)).ToList(), series);
    }

    internal static List<(Worksheet Sheet, CellRange Range)> Sources(Workbook wb, SheetChart chart)
    {
        var host = wb.Sheets.FirstOrDefault(s => s.Charts.Contains(chart)) ?? wb.Sheets[0];
        var list = new List<(Worksheet, CellRange)>();
        foreach (var reference in chart.Series.SelectMany(s => new[] { s.NameRef, s.CategoriesRef, s.ValuesRef }).Where(r => r != null))
        {
            if (RuleFormulas.Parse(reference) is not { } node) continue;
            var result = RuleFormulas.Calculate(new FormulaEvaluator(FunctionRegistry.Default), node, wb, host, default);
            if (result.Kind == CalcValueKind.Reference) list.AddRange(result.Areas.Select(x => (x.Sheet, x.Range)));
        }

        return list;
    }

    internal static List<(Worksheet Sheet, CellRef Cell, CellValue Value)> Cells(Workbook wb, Worksheet host, string reference)
    {
        if (RuleFormulas.Parse(reference) is not { } node) return null;
        var result = RuleFormulas.Calculate(new FormulaEvaluator(FunctionRegistry.Default), node, wb, host, default);
        if (result.Kind != CalcValueKind.Reference) return null;
        var list = new List<(Worksheet, CellRef, CellValue)>();
        foreach (var (sheet, range) in result.Areas)
            foreach (var c in FormulaEvaluator.TrimToUsed(sheet, range).Cells())
                list.Add((sheet, c, FormulaEvaluator.GetValue(sheet, c)));
        return list;
    }

    private static string Display(Workbook wb, (Worksheet Sheet, CellRef Cell, CellValue Value) c) =>
        c.Value.Kind == CellValueKind.Empty ? "" : NumberFormat.Format(c.Value, wb.Styles[c.Sheet.EffectiveStyleId(c.Cell)].NumberFormat, CultureInfo.CurrentCulture, out _);
}
