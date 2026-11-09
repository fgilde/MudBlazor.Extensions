using System.Text;
using ExcelDataReader;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>Reads legacy Excel 97-2003 workbooks (<c>.xls</c>) into a <see cref="Workbook"/>.</summary>
public static class XlsReader
{
    /// <summary>
    /// Reads the cell values of every sheet with their number formats, plus sheet names and visibility. Formulas, other
    /// styles and layout are not read; formula cells keep their last calculated value.
    /// </summary>
    /// <param name="stream">The file; it is left open.</param>
    /// <returns>The workbook, without a source package (saving writes a new xlsx).</returns>
    public static Workbook Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration { LeaveOpen = true });
        var wb = new Workbook();
        var formats = new Dictionary<string, int>();
        do
        {
            var ws = new Worksheet(reader.Name, wb) { Hidden = !string.Equals(reader.VisibleState, "visible", StringComparison.OrdinalIgnoreCase) };
            wb.SheetList.Add(ws);
            for (var row = 0; reader.Read() && row < CellRef.MaxRows; row++)
            {
                for (var col = 0; col < reader.FieldCount && col < CellRef.MaxColumns; col++)
                {
                    var value = reader.GetCellError(col) is { } error ? CellValue.Error(Map(error)) : Convert(reader.GetValue(col));
                    if (value.Kind == CellValueKind.Empty) continue;
                    var cell = ws.Ensure(new CellRef(row, col));
                    cell.Value = value;
                    if (value.Kind != CellValueKind.Number || reader.GetNumberFormatString(col) is not { } code || code == NumberFormat.General) continue;
                    if (!formats.TryGetValue(code, out var id))
                        formats[code] = id = wb.Styles.GetOrAdd(CellStyle.Default with { NumberFormat = code });
                    cell.StyleId = id;
                }
            }
        } while (reader.NextResult());

        return wb;
    }

    private static CellValue Convert(object value) => value switch
    {
        double d => CellValue.Number(d),
        int i => CellValue.Number(i),
        bool b => CellValue.Boolean(b),
        DateTime dt => CellValue.Number(dt.ToOADate()),
        TimeSpan ts => CellValue.Number(ts.TotalDays),
        string s => CellValue.Text(s),
        null => CellValue.Empty,
        _ => CellValue.Text(System.Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture))
    };

    private static CellError Map(ExcelDataReader.CellError error) => error switch
    {
        ExcelDataReader.CellError.NULL => CellError.Null,
        ExcelDataReader.CellError.DIV0 => CellError.Div0,
        ExcelDataReader.CellError.REF => CellError.Ref,
        ExcelDataReader.CellError.NAME => CellError.Name,
        ExcelDataReader.CellError.NUM => CellError.Num,
        ExcelDataReader.CellError.NA => CellError.NA,
        _ => CellError.Value
    };
}
