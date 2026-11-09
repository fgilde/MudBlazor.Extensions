using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>Reads and writes delimited text (CSV, TSV) as defined by RFC 4180.</summary>
public static class CsvFormat
{
    private static readonly char[] Delimiters = [',', ';', '\t', '|'];

    private static readonly Regex DotThousands = new(@"^[+-]?[1-9]\d{0,2}(?:\.\d{3})+$", RegexOptions.CultureInvariant);

    private static readonly Regex PlainNumber = new(@"^[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?$", RegexOptions.CultureInvariant);

    /// <summary>
    /// Reads delimited text into a workbook with one sheet. The encoding comes from the byte order mark, else UTF-8 if the bytes
    /// are valid UTF-8, else Windows-1252. Plain numbers with a dot as decimal separator are read as numbers; other fields are
    /// converted like typed input in <see cref="CultureInfo.CurrentCulture"/> (dates, percentages, currency).
    /// </summary>
    /// <param name="stream">The text; it is read to the end and left open.</param>
    /// <param name="delimiter">The field delimiter, or null to detect one of <c>,</c> <c>;</c> tab <c>|</c> from the first lines.</param>
    /// <returns>The workbook.</returns>
    public static Workbook Read(Stream stream, char? delimiter = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var text = Decode(stream);
        var sep = delimiter ?? Detect(text);
        var wb = new Workbook();
        var ws = new Worksheet("Sheet1", wb);
        wb.SheetList.Add(ws);
        var culture = CultureInfo.CurrentCulture;
        var formats = new Dictionary<string, int>();
        var field = new StringBuilder();
        int row = 0, col = 0;
        bool quoted = false, inQuotes = false;

        void Emit()
        {
            if (field.Length > 0 && row < CellRef.MaxRows && col < CellRef.MaxColumns)
            {
                var (value, format) = Convert(field.ToString(), culture);
                var cell = ws.Ensure(new CellRef(row, col));
                cell.Value = value;
                if (format != null)
                {
                    if (!formats.TryGetValue(format, out var id))
                        formats[format] = id = wb.Styles.GetOrAdd(CellStyle.Default with { NumberFormat = format });
                    cell.StyleId = id;
                }
            }

            field.Clear();
            quoted = false;
        }

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (inQuotes)
            {
                if (c != '"') field.Append(c);
                else if (i + 1 < text.Length && text[i + 1] == '"') field.Append(text[++i]);
                else inQuotes = false;
            }
            else if (c == '"' && field.Length == 0 && !quoted) quoted = inQuotes = true;
            else if (c == sep)
            {
                Emit();
                col++;
            }
            else if (c is '\r' or '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                Emit();
                row++;
                col = 0;
            }
            else field.Append(c);
        }

        Emit();
        return wb;
    }

    /// <summary>
    /// Writes the sheet from A1 to the end of its used range as UTF-8 with byte order mark and CRLF line ends. Values are written
    /// as displayed with their number format in <see cref="CultureInfo.CurrentCulture"/>; numbers in the General format keep
    /// 15 significant digits. Fields containing the delimiter, quotes or line breaks are quoted.
    /// </summary>
    /// <param name="sheet">The sheet to write.</param>
    /// <param name="target">The stream to write to; it is left open.</param>
    /// <param name="delimiter">The field delimiter.</param>
    public static void Write(Worksheet sheet, Stream target, char delimiter = ',')
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(target);
        var culture = CultureInfo.CurrentCulture;
        var styles = sheet.Workbook?.Styles;
        using var writer = new StreamWriter(target, new UTF8Encoding(true), 65536, true) { NewLine = "\r\n" };
        var used = sheet.UsedRange;
        if (sheet[used.Start] == null && used.IsSingleCell) return;
        var special = new[] { delimiter, '"', '\r', '\n' };
        for (var r = 0; r <= used.Bottom; r++)
        {
            for (var c = 0; c <= used.Right; c++)
            {
                if (c > 0) writer.Write(delimiter);
                var cell = sheet[new CellRef(r, c)];
                if (cell == null || cell.Value.Kind == CellValueKind.Empty) continue;
                var format = styles?[sheet.EffectiveStyleId(new CellRef(r, c))].NumberFormat ?? NumberFormat.General;
                var text = cell.Value.Kind == CellValueKind.Number && format == NumberFormat.General
                    ? cell.Value.NumberValue.ToString("G15", culture)
                    : NumberFormat.Format(cell.Value, format, culture, out _);
                if (text.IndexOfAny(special) >= 0) writer.Write("\"" + text.Replace("\"", "\"\"") + "\"");
                else writer.Write(text);
            }

            writer.WriteLine();
        }
    }

    private static (CellValue, string) Convert(string s, CultureInfo culture)
    {
        if (PlainNumber.IsMatch(s) && !(culture.NumberFormat.NumberGroupSeparator == "." && DotThousands.IsMatch(s))) return (CellValue.Number(double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture)), null);
        if (s[0] is '\'' or '=') return (CellValue.Text(s), null);
        return CellInputParser.Parse(s, culture);
    }

    private static string Decode(Stream stream)
    {
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        var bytes = copy.GetBuffer().AsSpan(0, (int)copy.Length);
        Encoding bom = bytes switch
        {
            [0xEF, 0xBB, 0xBF, ..] => Encoding.UTF8,
            [0xFF, 0xFE, 0, 0, ..] => Encoding.UTF32,
            [0xFF, 0xFE, ..] => Encoding.Unicode,
            [0xFE, 0xFF, ..] => Encoding.BigEndianUnicode,
            [0, 0, 0xFE, 0xFF, ..] => new UTF32Encoding(true, true),
            _ => null
        };
        if (bom != null) return bom.GetString(bytes[bom.Preamble.Length..]);
        try
        {
            return new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252).GetString(bytes);
        }
    }

    private static char Detect(string text)
    {
        var lines = new List<int[]>();
        var counts = new int[Delimiters.Length];
        var any = false;
        var inQuotes = false;
        var culture = CultureInfo.CurrentCulture;
        var decimalSeparator = culture.NumberFormat.NumberDecimalSeparator is [var sep] ? sep : '.';
        for (var i = 0; i < text.Length && lines.Count < 10; i++)
        {
            var c = text[i];
            if (c == '"') inQuotes = !inQuotes;
            if (inQuotes) continue;
            if (c is '\r' or '\n')
            {
                if (any) lines.Add(counts);
                counts = new int[Delimiters.Length];
                any = false;
                continue;
            }

            any = true;
            if (c == decimalSeparator && i > 0 && i + 1 < text.Length && char.IsAsciiDigit(text[i - 1]) && char.IsAsciiDigit(text[i + 1])) continue;
            var d = Array.IndexOf(Delimiters, c);
            if (d >= 0) counts[d]++;
        }

        if (any) lines.Add(counts);
        if (lines.Count == 0) return ',';
        var list = culture.TextInfo.ListSeparator;
        var best = Enumerable.Range(0, Delimiters.Length)
            .OrderByDescending(d => lines.Min(l => l[d]))
            .ThenByDescending(d => lines[0][d])
            .ThenByDescending(d => list.Length == 1 && list[0] == Delimiters[d])
            .First();
        return lines[0][best] > 0 ? Delimiters[best] : ',';
    }
}
