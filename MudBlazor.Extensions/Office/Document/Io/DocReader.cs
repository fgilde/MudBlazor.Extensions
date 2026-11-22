using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Xml;

namespace MudBlazor.Extensions.Office.Document;

internal static class DocReader
{
    private static readonly Encoding Ansi = CodePagesEncodingProvider.Instance.GetEncoding(1252);

    private static readonly string[] Colors =
    [
        "auto", "#000000", "#0000FF", "#00FFFF", "#00FF00", "#FF00FF", "#FF0000", "#FFFF00", "#FFFFFF", "#000080", "#008080", "#008000", "#800080", "#800000",
        "#808000", "#808080", "#C0C0C0"
    ];

    private static readonly Dictionary<int, string> Underlines = new()
    {
        [0] = "none", [1] = "single", [2] = "words", [3] = "double", [4] = "dotted", [6] = "thick", [7] = "dash", [9] = "dotDash", [10] = "dotDotDash",
        [11] = "wave", [20] = "dottedHeavy", [23] = "dashedHeavy", [25] = "dashDotHeavy", [26] = "dashDotDotHeavy", [27] = "wavyHeavy", [39] = "dashLong",
        [43] = "wavyDouble", [55] = "dashLongHeavy"
    };

    internal static TextDocument Read(byte[] bytes)
    {
        var file = CompoundFile.Open(bytes);
        var main = file.ReadStream("WordDocument");
        if (U16(main, 0) != 0xA5EC) throw Invalid("The file is not a Word document.");
        if (U16(main, 2) < 0xC0) throw new NotSupportedException("Word documents older than Word 97 are not supported.");
        var flags = U16(main, 0x0A);
        if ((flags & 0x0100) != 0) throw new NotSupportedException("Encrypted Word documents are not supported.");
        var table = file.ReadStream((flags & 0x0200) != 0 ? "1Table" : "0Table");
        var longs = 0x22 + 2 * U16(main, 0x20) + 2;
        var pairs = longs + 4 * U16(main, longs - 2) + 2;
        var count = U16(main, pairs - 2);
        var length = (int)U32(main, longs + 12);
        if (length is < 0 or > HtmlReader.MaxLength) throw Invalid("The document text is longer than 32 Mi characters.");

        var fonts = Fonts(Part(15));
        var builder = new Builder(Runs(main, Part(12), (page, i, n) => Chpx(page, i, n, fonts)), Runs(main, Part(13), Papx));
        foreach (var (start, size, compressed) in Pieces(main, Part(33), length))
        {
            var text = compressed ? Ansi.GetString(main, (int)start, size) : null;
            for (var i = 0; i < size; i++)
                builder.Add(compressed ? text[i] : (char)U16(main, start + 2L * i), start + (compressed ? i : 2L * i), compressed ? 1 : 2);
        }

        return builder.Build();

        byte[] Part(int index) => index < count ? Slice(table, U32(main, pairs + 8 * index), U32(main, pairs + 8 * index + 4)) : [];
    }

    private static List<(long Start, int Size, bool Compressed)> Pieces(byte[] main, byte[] clx, int length)
    {
        var at = 0L;
        while (at < clx.Length && clx[at] == 1) at += 3 + U16(clx, at + 1);
        if (B(clx, at) != 2) throw Invalid("The document has no piece table.");
        var plc = Slice(clx, at + 5, U32(clx, at + 1));
        var count = (plc.Length - 4) / 12;
        if (count < 1 || (plc.Length - 4) % 12 != 0 || U32(plc, 0) != 0 || U32(plc, 4 * count) < length) throw Invalid("The document has a broken piece table.");
        var pieces = new List<(long, int, bool)>();
        for (var i = 0; i < count; i++)
        {
            var (cp, next, fc) = (U32(plc, 4 * i), U32(plc, 4 * i + 4), U32(plc, 4 * (count + 1) + 8 * i + 2));
            var compressed = (fc & 0x40000000) != 0;
            var start = compressed ? (fc & 0x3FFFFFFF) / 2 : fc & 0x3FFFFFFF;
            if (next <= cp || start + (long)(next - cp) * (compressed ? 1 : 2) > main.Length) throw Invalid("The document has a broken piece table.");
            if (cp < length) pieces.Add((start, (int)(Math.Min(next, (uint)length) - cp), compressed));
        }

        return pieces;
    }

    private static List<(uint Start, uint End, T Value)> Runs<T>(byte[] main, byte[] plc, Func<byte[], int, int, T> parse)
    {
        var runs = new List<(uint Start, uint End, T Value)>();
        if (plc.Length < 4) return runs;
        var count = (plc.Length - 4) / 8;
        if ((plc.Length - 4) % 8 != 0 || count > main.Length / 512) throw Invalid("The document has a broken formatting table.");
        for (var i = 0; i < count; i++)
        {
            var page = Slice(main, (U32(plc, 4 * (count + 1) + 4 * i) & 0x3FFFFF) * 512L, 512);
            for (var j = 0; j < page[511]; j++) runs.Add((U32(page, 4 * j), U32(page, 4 * j + 4), parse(page, j, page[511])));
        }

        runs.Sort((a, b) => a.Start.CompareTo(b.Start));
        return runs;
    }

    private static CharacterRun Chpx(byte[] page, int index, int count, List<string> fonts)
    {
        var offset = 2 * B(page, 4 * (count + 1) + index);
        var run = new CharacterRun(new RunFormat(), null, '\0', 0);
        if (offset == 0) return run;
        var end = offset + 1 + B(page, offset);
        if (end > 511) throw Invalid("The document has a broken character format.");
        var format = run.Format;
        foreach (var (sprm, at) in Sprms(page, offset + 1, end))
        {
            if (sprm is >= 0x0835 and <= 0x0837 && page[at] == 0x81) run = run with { Opposite = run.Opposite | 1 << (sprm - 0x0835) };
            if (sprm == 0x6A09) run = run with { SymbolFont = U16(page, at) < fonts.Count ? fonts[U16(page, at)] : null, Symbol = (char)U16(page, at + 2) };
            format = sprm switch
            {
                0x0835 => format with { Bold = Toggle(page[at]) },
                0x0836 => format with { Italic = Toggle(page[at]) },
                0x0837 => format with { Strike = Toggle(page[at]) },
                0x2A3E => format with { Underline = Underlines.GetValueOrDefault(page[at], "single") },
                0x4A43 when U16(page, at) > 0 => format with { SizePt = U16(page, at) / 2.0 },
                0x2A42 when page[at] < Colors.Length => format with { Color = Colors[page[at]] },
                0x6870 => format with { Color = page[at + 3] == 0xFF ? "auto" : $"#{page[at]:X2}{page[at + 1]:X2}{page[at + 2]:X2}" },
                0x4A4F when U16(page, at) < fonts.Count => format with { Font = fonts[U16(page, at)] },
                _ => format
            };
        }

        return run with { Format = format };
    }

    private static (ParagraphAlignment? Alignment, int Style, bool RowEnd) Papx(byte[] page, int index, int count)
    {
        var offset = 2 * B(page, 4 * (count + 1) + 13 * index);
        if (offset == 0) return (null, 0, false);
        var (start, size) = B(page, offset) is var cb and > 0 ? (offset + 1, 2 * cb - 1) : (offset + 2, 2 * B(page, offset + 1));
        if (size < 2 || start + size > 511) throw Invalid("The document has a broken paragraph format.");
        (ParagraphAlignment? Alignment, int Style, bool RowEnd) result = (null, U16(page, start), false);
        foreach (var (sprm, at) in Sprms(page, start + 2, start + size))
            if (sprm is 0x2403 or 0x2461) result.Alignment = page[at] switch { 0 => ParagraphAlignment.Left, 1 => ParagraphAlignment.Center, 2 => ParagraphAlignment.Right, _ => ParagraphAlignment.Justify };
            else if (sprm is 0x2417 or 0x244C) result.RowEnd = page[at] != 0;
        return result;
    }

    private static int ChangedTabsLength(byte[] data, int operand, int end)
    {
        if (operand + 1 >= end) return -1;
        var added = operand + 2 + 4 * data[operand + 1];
        return added < end ? 1 + 1 + 4 * data[operand + 1] + 1 + 3 * data[added] : -1;
    }

    private static IEnumerable<(int Sprm, int Operand)> Sprms(byte[] data, int start, int end)
    {
        while (start + 2 <= end)
        {
            var sprm = U16(data, start);
            var operand = start + 2;
            var size = (sprm >> 13) switch
            {
                0 or 1 => 1,
                2 or 4 or 5 => 2,
                3 => 4,
                7 => 3,
                _ when sprm == 0xD608 && operand + 2 <= end => U16(data, operand) + 1,
                _ when sprm == 0xC615 && operand < end && data[operand] == 255 => ChangedTabsLength(data, operand, end),
                _ => operand < end ? data[operand] + 1 : -1
            };
            if (size < 0 || operand + size > end) throw Invalid("The document has a broken property list.");
            yield return (sprm, operand);
            start = operand + size;
        }
    }

    private static List<string> Fonts(byte[] table)
    {
        var fonts = new List<string>();
        if (table.Length < 4 || U16(table, 0) == 0xFFFF) return fonts;
        var at = 4L;
        for (var i = 0; i < U16(table, 0); i++)
        {
            var font = Slice(table, at + 1, B(table, at));
            var name = new StringBuilder();
            for (var o = 39; o + 1 < font.Length && U16(font, o) is var c and > 0; o += 2)
                if (XmlConvert.IsXmlChar((char)c) && !char.IsControl((char)c)) name.Append((char)c);
            fonts.Add(name.ToString());
            at += 1 + font.Length;
        }

        return fonts;
    }

    private static int Find<T>(List<(uint Start, uint End, T Value)> runs, long fc)
    {
        var (low, high) = (0, runs.Count - 1);
        while (low <= high)
        {
            var middle = (low + high) >>> 1;
            if (runs[middle].End <= fc) low = middle + 1;
            else if (runs[middle].Start > fc) high = middle - 1;
            else return middle;
        }

        return -1;
    }

    private static bool? Toggle(byte value) => value switch { 0 => false, 1 => true, _ => null };

    private static byte[] Slice(byte[] data, long offset, long length) =>
        offset >= 0 && length >= 0 && offset + length <= data.Length ? data.AsSpan((int)offset, (int)length).ToArray() : throw Invalid("The document refers to data beyond its end.");

    private static int B(byte[] data, long at) => at >= 0 && at < data.Length ? data[at] : throw Invalid("The document is truncated.");

    private static int U16(byte[] data, long at) => at >= 0 && at + 2 <= data.Length ? BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan((int)at)) : throw Invalid("The document is truncated.");

    private static uint U32(byte[] data, long at) => at >= 0 && at + 4 <= data.Length ? BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan((int)at)) : throw Invalid("The document is truncated.");

    private static InvalidDataException Invalid(string message) => new(message);

    private static string StyleId(int istd) => istd is >= 1 and <= 6 ? "Heading" + istd : null;

    private readonly record struct CharacterRun(RunFormat Format, string SymbolFont, char Symbol, int Opposite);

    private sealed class Builder(
        List<(uint Start, uint End, CharacterRun Value)> characters,
        List<(uint Start, uint End, (ParagraphAlignment? Alignment, int Style, bool RowEnd) Value)> paragraphs)
    {
        private readonly TextDocument _document = TextDocument.Create();
        private readonly Dictionary<(int Run, int Style), RunFormat> _resolved = new();
        private readonly List<Block> _blocks = [];
        private readonly List<Inline> _inlines = [];
        private readonly StringBuilder _run = new();
        private readonly Stack<bool> _fields = new();
        private RunFormat _format = new();
        private int _code;
        private int _footnotes;
        private char _high;

        public void Add(char c, long fc, int width)
        {
            var high = _high;
            _high = '\0';
            switch (c)
            {
                case '\r' or '\a':
                    End(fc);
                    return;
                case '\u0013':
                    _fields.Push(true);
                    _code++;
                    return;
                case '\u0014' when _fields.TryPeek(out var code) && code:
                    _fields.Pop();
                    _fields.Push(false);
                    _code--;
                    return;
                case '\u0015':
                    if (_fields.TryPop(out var open) && open) _code--;
                    return;
            }

            if (_code > 0) return;
            var index = Find(characters, fc);
            var run = index < 0 ? new CharacterRun(new RunFormat(), null, '\0', 0) : characters[index].Value;
            var format = run.Opposite == 0 ? run.Format : Resolve(index, run, fc);
            if (c == '(' && run.Symbol != '\0')
            {
                if (run.Symbol >= ' ' && XmlConvert.IsXmlChar(run.Symbol)) Append(run.Symbol, run.SymbolFont != null ? format with { Font = run.SymbolFont } : format);
                return;
            }

            switch (c)
            {
                case '\f':
                    Append(c, format);
                    if (Find(paragraphs, fc) is var paragraph and >= 0 && paragraphs[paragraph].End == fc + width) End(fc);
                    break;
                case '\t' or '\v':
                    Append(c, format);
                    break;
                case '\u0002':
                    Flush();
                    _inlines.Add(new OpaqueInline("footnote", (++_footnotes).ToString(CultureInfo.InvariantCulture)) { Format = format });
                    break;
                case '\u001E' or '\u001F':
                    Append(c == '\u001E' ? '\u2011' : '\u00AD', format);
                    break;
                case var _ when char.IsHighSurrogate(c):
                    _high = c;
                    break;
                case var _ when char.IsLowSurrogate(c):
                    if (high != '\0')
                    {
                        Append(high, format);
                        Append(c, format);
                    }

                    break;
                case var _ when c >= ' ' && XmlConvert.IsXmlChar(c):
                    Append(c, format);
                    break;
            }
        }

        public TextDocument Build()
        {
            if (_run.Length > 0 || _inlines.Count > 0 || _blocks.Count == 0) End(-1);
            _document.BlockList.Clear();
            _document.BlockList.AddRange(_blocks.Select(_document.Identify));
            _document.InvalidateIndex();
            return _document;
        }

        private RunFormat Resolve(int index, CharacterRun run, long fc)
        {
            var paragraph = Find(paragraphs, fc);
            var style = paragraph < 0 ? 0 : paragraphs[paragraph].Value.Style;
            if (_resolved.TryGetValue((index, style), out var format)) return format;
            var styleId = AddStyle(style);
            var basis = EffectiveFormat.Run(_document.Styles, _document.Theme, null, new Paragraph { Format = new ParagraphFormat { StyleId = styleId } }, null);
            format = run.Format;
            if ((run.Opposite & 1) != 0) format = format with { Bold = basis.Bold != true };
            if ((run.Opposite & 2) != 0) format = format with { Italic = basis.Italic != true };
            if ((run.Opposite & 4) != 0) format = format with { Strike = basis.Strike != true };
            return _resolved[(index, style)] = format;
        }

        private string AddStyle(int istd)
        {
            var styleId = StyleId(istd);
            if (styleId != null) _document.Styles = StyleDefinitions.With(_document.Styles, styleId, StyleType.Paragraph);
            return styleId;
        }

        private void Append(char c, RunFormat format)
        {
            if (_run.Length > 0 && format != _format) Flush();
            _format = format;
            _run.Append(c);
        }

        private void Flush()
        {
            if (_run.Length > 0) _inlines.AddRange(InlineEdit.Lines(_run.ToString(), _format, null, null)[0]);
            _run.Clear();
        }

        private void End(long fc)
        {
            while (_code > 0 && _fields.TryPop(out var code))
                if (code)
                    _code--;
            Flush();
            var index = Find(paragraphs, fc);
            var (alignment, style, rowEnd) = index < 0 ? default : paragraphs[index].Value;
            if (rowEnd && _inlines.Count == 0) return;
            var styleId = AddStyle(style);
            _blocks.Add(new Paragraph
            {
                Format = new ParagraphFormat { Alignment = alignment, StyleId = styleId, OutlineLevel = style is >= 7 and <= 9 ? style - 1 : null },
                Inlines = InlineEdit.Normalize(_inlines.ToList())
            });
            _inlines.Clear();
        }
    }
}
