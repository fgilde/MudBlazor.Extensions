namespace MudBlazor.Extensions.Core;

public class ExcelFile
{
    public List<ExcelSheet> Sheets { get; set; } = new List<ExcelSheet>();
}

public class ExcelSheet
{
    public string Name { get; set; }
    public List<ExcelRow> Rows { get; set; } = new List<ExcelRow>();
}

public class ExcelRow
{
    public Dictionary<string, object> Cells { get; set; } = new Dictionary<string, object>();
}
