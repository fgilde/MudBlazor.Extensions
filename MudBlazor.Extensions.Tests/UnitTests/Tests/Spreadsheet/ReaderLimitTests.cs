using System.Text;
using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Spreadsheet;

[CollectionDefinition(nameof(ReaderLimitTests), DisableParallelization = true)]
public class ReaderLimitCollection;

[Collection(nameof(ReaderLimitTests))]
public class ReaderLimitTests
{
    private static void WithLimit(int limit, Action test)
    {
        var previous = Workbook.MaxCells;
        Workbook.MaxCells = limit;
        try
        {
            test();
        }
        finally
        {
            Workbook.MaxCells = previous;
        }
    }

    private static string Cells(int count) => "<row r=\"1\">" + string.Concat(Enumerable.Range(0, count).Select(_ => "<c><v>1</v></c>")) + "</row>";

    [Fact]
    public void Xlsx_MoreCellsThanTheLimit_Throws() => WithLimit(5, () =>
    {
        XlsxReader.Read(new MemoryStream(XlsxReadTests.Package([XlsxReadTests.Sheet(Cells(5))])));
        Assert.Throws<InvalidDataException>(() =>
            XlsxReader.Read(new MemoryStream(XlsxReadTests.Package([XlsxReadTests.Sheet(Cells(3)), XlsxReadTests.Sheet(Cells(3))]))));
    });

    [Fact]
    public void Xlsx_MoreSharedStringsThanTheLimit_Throws() => WithLimit(5, () =>
        Assert.Throws<InvalidDataException>(() => XlsxReader.Read(new MemoryStream(XlsxReadTests.Package([XlsxReadTests.Sheet("")],
            string.Concat(Enumerable.Repeat("<si/>", 6)))))));

    [Fact]
    public void Csv_MoreCellsThanTheLimit_Throws() => WithLimit(5, () =>
    {
        CsvFormat.Read(new MemoryStream(Encoding.UTF8.GetBytes("1,2,3\n4,5")));
        Assert.Throws<InvalidDataException>(() => CsvFormat.Read(new MemoryStream(Encoding.UTF8.GetBytes("1,2,3\n4,5,6"))));
    });

    [Fact]
    public void Limit_DoesNotApplyToEditing() => WithLimit(2, () =>
    {
        var wb = Workbook.Create();
        for (var i = 0; i < 5; i++) wb.Execute(new SetValueCommand(wb.Sheets[0], new CellRef(i, 0), CellValue.Number(i)));
        Assert.Equal(5, wb.Sheets[0].UsedCells().Count());
    });
}
