using MainSample.WebAssembly.Examples.Spreadsheet;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Spreadsheet;

public class FeatureSamplesTests
{
    public static TheoryData<string> Builders => ["overview", "data", "charts"];

    private static Workbook Build(string name) => name switch
    {
        "overview" => SpreadsheetFeatureSamples.CreateOverview(),
        "data" => SpreadsheetFeatureSamples.CreateData(),
        _ => SpreadsheetFeatureSamples.CreateCharts()
    };

    [Theory]
    [MemberData(nameof(Builders))]
    public void Workbook_SurvivesSaveAndLoad(string name)
    {
        var wb = Build(name);
        using var ms = new MemoryStream();
        XlsxWriter.Write(wb, ms);
        ms.Position = 0;
        var loaded = XlsxReader.Read(ms);
        Assert.Equal(wb.Sheets.Count, loaded.Sheets.Count);
        Assert.Equal(wb.Sheets.Sum(s => s.Charts.Count), loaded.Sheets.Sum(s => s.Charts.Count));
        Assert.Equal(wb.Sheets.Sum(s => s.Tables.Count), loaded.Sheets.Sum(s => s.Tables.Count));
        Assert.Equal(wb.Sheets.Sum(s => s.DataValidations.Count), loaded.Sheets.Sum(s => s.DataValidations.Count));
    }

    [Fact]
    public void Overview_HasEveryFeature()
    {
        var sheet = SpreadsheetFeatureSamples.CreateOverview().Sheets[0];
        Assert.Single(sheet.Charts);
        Assert.Single(sheet.Tables);
        Assert.Single(sheet.ConditionalFormats);
        Assert.Single(sheet.DataValidations);
        Assert.Single(sheet.Notes);
        Assert.Single(sheet.Links);
        Assert.Equal([1, 2], sheet.FilteredRows.Order());
    }

    [Fact]
    public void Data_SortedByRegionThenCalculatedAmount()
    {
        var sheet = SpreadsheetFeatureSamples.CreateData().Sheets[1];
        var rows = Enumerable.Range(1, 12).Select(r => (Region: sheet[new CellRef(r, 0)].Value.TextValue, Amount: sheet[new CellRef(r, 4)].Value.NumberValue)).ToList();
        Assert.All(rows, r => Assert.True(r.Amount > 0));
        Assert.All(rows.Zip(rows.Skip(1)), p => Assert.True(string.Compare(p.First.Region, p.Second.Region, StringComparison.CurrentCultureIgnoreCase) < 0
                                                             || p.First.Region == p.Second.Region && p.First.Amount >= p.Second.Amount));
    }

    [Theory]
    [MemberData(nameof(Builders))]
    public async Task Editor_RendersWorkbook(string name)
    {
        await using var context = new BunitContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        context.Render<MudBlazor.MudPopoverProvider>();
        var cut = context.Render<MudExSpreadsheetEditor>(p => p.Add(x => x.Workbook, Build(name)));
        Assert.NotEmpty(cut.Markup);
    }
}
