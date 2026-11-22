using System.Globalization;
using System.Text;

namespace MudBlazor.Extensions.Office.Document;

internal static class RtfWriter
{
    private const long EmuPerTwip = 635;
    private const double EmuPerPixel = 9525;
    private const string Border = @"\brdrs\brdrw10";
    private const int MaxColumns = 64;
    private const int HexChunk = 32768;
    internal const int MaxImageBytes = 16 * 1024 * 1024;
    internal static readonly HashSet<string> ImageTypes = ["image/png", "image/jpeg"];

    internal static void Write(TextDocument document, Stream target)
    {
        using var writer = new StreamWriter(target, Encoding.ASCII, 65536, true);
        new Writer(document).Run(writer);
    }

    private sealed class Writer(TextDocument document)
    {
        private readonly List<(StringBuilder Text, byte[] Image)> _parts = [];
        private StringBuilder _body = new();
        private readonly List<string> _fonts = [document.EffectiveRunFormat(new Paragraph(), null).Font?.Replace(";", "") is { Length: > 0 } font ? font : "Calibri"];
        private readonly List<string> _colors = [];

        public void Run(TextWriter output)
        {
            Blocks(document.Blocks, false);
            var rtf = new StringBuilder(@"{\rtf1\ansi\ansicpg1252\deff0{\fonttbl");
            for (var i = 0; i < _fonts.Count; i++) rtf.Append(@"{\f").Append(N(i)).Append(@"\fnil ").Append(Escape(_fonts[i])).Append(";}");
            rtf.Append(@"}{\colortbl;");
            foreach (var color in _colors)
                rtf.Append(@"\red").Append(N(Convert.ToInt32(color[1..3], 16))).Append(@"\green").Append(N(Convert.ToInt32(color[3..5], 16)))
                    .Append(@"\blue").Append(N(Convert.ToInt32(color[5..7], 16))).Append(';');
            rtf.Append('}');
            if (document.Properties.Title is { Length: > 0 } title) rtf.Append(@"{\info{\title ").Append(Escape(title)).Append("}}");
            var page = document.Sections[^1].Page;
            rtf.Append(@"\paperw").Append(N(page.Width)).Append(@"\paperh").Append(N(page.Height)).Append(@"\margl").Append(N(page.MarginLeft))
                .Append(@"\margr").Append(N(page.MarginRight)).Append(@"\margt").Append(N(page.MarginTop)).Append(@"\margb").Append(N(page.MarginBottom));
            if (page.Orientation == PageOrientation.Landscape) rtf.Append(@"\landscape");
            output.Write(rtf.Append('\n'));
            foreach (var (text, image) in _parts)
            {
                output.Write(text);
                for (var at = 0; at < image.Length; at += HexChunk) output.Write(Convert.ToHexString(image.AsSpan(at, Math.Min(HexChunk, image.Length - at))));
                output.Write('}');
            }

            output.Write(_body.Append("}\n"));
        }

        private void Blocks(IEnumerable<Block> blocks, bool inTable)
        {
            foreach (var block in blocks)
                switch (block)
                {
                    case Paragraph paragraph:
                        Paragraph(paragraph, inTable, @"\par");
                        break;
                    case Table table when inTable:
                        foreach (var row in table.Rows)
                            _body.Append(@"\pard\plain\intbl ").Append(Escape(string.Join('\t', TextFormat.Cells(row, c => TextFormat.CellText(document, c.Blocks))))).Append(@"\par").Append('\n');
                        break;
                    case Table table:
                        Table(table);
                        break;
                    case OpaqueBlock { Visible: true, Preview: { } preview }:
                        Blocks(preview, inTable);
                        break;
                }
        }

        private void Paragraph(Paragraph paragraph, bool inTable, string end)
        {
            var format = document.EffectiveParagraphFormat(paragraph);
            _body.Append(@"\pard\plain");
            if (inTable) _body.Append(@"\intbl");
            ParagraphFormat(format);
            var mark = document.EffectiveRunFormat(paragraph, null);
            if (document.ListLabel(paragraph) is { Length: > 0 } label)
            {
                var level = format.Numbering is { } numbering ? document.Numbering.Level(numbering) : null;
                var labelFormat = level?.LabelFormat is { Font: "Symbol" or "Wingdings" or "Wingdings 2" or "Wingdings 3" or "Webdings" } symbol ? symbol with { Font = null } : level?.LabelFormat;
                Run(labelFormat?.Over(mark) ?? mark, Escape(label) + (level?.Suffix switch { "space" => " ", "nothing" => "", _ => @"\tab" }));
            }

            for (var i = 0; i < paragraph.Inlines.Count;)
            {
                var link = paragraph.Inlines[i].Link;
                var j = i;
                while (j < paragraph.Inlines.Count && ReferenceEquals(paragraph.Inlines[j].Link, link)) j++;
                var href = link?.SafeTarget;
                if (href != null)
                    _body.Append(@"{\field{\*\fldinst{HYPERLINK ").Append(href[0] == '#' ? @"\\l """ + Escape(Field(href[1..])) : "\"" + Escape(Field(href)))
                        .Append(@"""}}{\fldrslt{");
                for (var k = i; k < j; k++) Inline(paragraph, paragraph.Inlines[k]);
                if (href != null) _body.Append("}}}");
                i = j;
            }

            _body.Append(@"\f").Append(N(Font(mark.Font)));
            if (mark.SizePt is { } size) _body.Append(@"\fs").Append(N((long)Math.Round(size * 2)));
            _body.Append(end).Append('\n');
        }

        private void ParagraphFormat(ParagraphFormat format)
        {
            _body.Append(format.Alignment switch
            {
                ParagraphAlignment.Center => @"\qc",
                ParagraphAlignment.Right => @"\qr",
                ParagraphAlignment.Justify => @"\qj",
                _ => @"\ql"
            });
            Word(@"\li", format.IndentLeft);
            Word(@"\fi", format.IndentFirstLine);
            Word(@"\ri", format.IndentRight);
            Word(@"\sb", format.SpacingBefore);
            Word(@"\sa", format.SpacingAfter);
            if (format.Line is { } line)
                _body.Append(@"\sl").Append(N(line.Rule == LineRule.Exact ? -line.Value : line.Value)).Append(line.Rule == LineRule.Auto ? @"\slmult1" : @"\slmult0");
            if (format.KeepWithNext == true) _body.Append(@"\keepn");
            if (format.KeepLines == true) _body.Append(@"\keep");
            if (format.PageBreakBefore == true) _body.Append(@"\pagebb");
            if (format.WidowControl is { } widow) _body.Append(widow ? @"\widctlpar" : @"\nowidctlpar");
            if (format.OutlineLevel is { } outline and >= 0 and < 9) _body.Append(@"\outlinelevel").Append(N(outline));
            foreach (var tab in format.Tabs ?? [])
            {
                if (tab.Alignment == TabAlignment.Clear) continue;
                _body.Append(tab.Alignment switch { TabAlignment.Center => @"\tqc", TabAlignment.Right => @"\tqr", TabAlignment.Decimal => @"\tqdec", _ => "" })
                    .Append(tab.Leader switch { "dot" => @"\tldot", "hyphen" => @"\tlhyph", "underscore" => @"\tlul", "heavy" => @"\tlth", "middleDot" => @"\tlmdot", _ => "" })
                    .Append(tab.Alignment == TabAlignment.Bar ? @"\tb" : @"\tx").Append(N(tab.Position));
            }

            if (Color(format.Shading) is { } shading) _body.Append(@"\cbpat").Append(N(shading));
        }

        private void Word(string word, int? value)
        {
            if (value is { } number and not 0) _body.Append(word).Append(N(number));
        }

        private void Inline(Paragraph paragraph, Inline inline)
        {
            switch (inline)
            {
                case TextRun run:
                    Run(document.EffectiveRunFormat(paragraph, inline), Escape(run.Text));
                    break;
                case Tab:
                    Run(document.EffectiveRunFormat(paragraph, inline), @"\tab");
                    break;
                case Break item:
                    Run(document.EffectiveRunFormat(paragraph, inline), item.Kind switch { BreakKind.Page => @"\page", BreakKind.Column => @"\column", _ => @"\line" });
                    break;
                case OpaqueInline token:
                    Run(document.EffectiveRunFormat(paragraph, inline), Escape(token.DisplayText));
                    break;
                case InlineImage image:
                    Image(image);
                    break;
                case Marker { Kind: "bookmarkStart", Name: { Length: > 0 } name }:
                    _body.Append(@"{\*\bkmkstart ").Append(Escape(name)).Append(@"}{\*\bkmkend ").Append(Escape(name)).Append('}');
                    break;
            }
        }

        private void Run(RunFormat format, string content)
        {
            if (content.Length == 0) return;
            _body.Append('{').Append(@"\f").Append(N(Font(format.Font)));
            if (format.SizePt is { } size) _body.Append(@"\fs").Append(N((long)Math.Round(size * 2)));
            if (format.Bold == true) _body.Append(@"\b");
            if (format.Italic == true) _body.Append(@"\i");
            if (format.Strike == true) _body.Append(@"\strike");
            if (format.DoubleStrike == true) _body.Append(@"\striked1");
            if (format.Caps == true) _body.Append(@"\caps");
            if (format.SmallCaps == true) _body.Append(@"\scaps");
            if (format.Hidden == true) _body.Append(@"\v");
            if (format.Underline is { } underline and not "none")
                _body.Append(underline switch
                {
                    "double" => @"\uldb", "words" => @"\ulw", "dotted" => @"\uld", "dash" => @"\uldash", "wave" => @"\ulwave", "thick" => @"\ulth", _ => @"\ul"
                });
            if (Color(format.Color) is { } color) _body.Append(@"\cf").Append(N(color));
            if (format.Highlight != null && HtmlReader.Highlights.TryGetValue(format.Highlight, out var highlight)) _body.Append(@"\highlight").Append(N(Color(highlight)!.Value));
            else if (Color(format.Shading) is { } shading) _body.Append(@"\chcbpat").Append(N(shading));
            if (format.VerticalAlign == VerticalAlign.Superscript) _body.Append(@"\super");
            else if (format.VerticalAlign == VerticalAlign.Subscript) _body.Append(@"\sub");
            _body.Append(' ').Append(content).Append('}');
        }

        private void Image(InlineImage image)
        {
            var data = document.GetMedia(image.MediaPart);
            var type = document.MediaContentType(image.MediaPart)?.ToLowerInvariant();
            if (data == null || type == null || !ImageTypes.Contains(type) || data.Length > MaxImageBytes) return;
            var (width, height) = OfficeImageSize.Read(data, type) ?? (image.WidthEmu / EmuPerPixel, image.HeightEmu / EmuPerPixel);
            _body.Append(@"{\pict").Append(type == "image/png" ? @"\pngblip" : @"\jpegblip")
                .Append(@"\picw").Append(N(Math.Max(1, (long)Math.Round(width)))).Append(@"\pich").Append(N(Math.Max(1, (long)Math.Round(height))))
                .Append(@"\picwgoal").Append(N(image.WidthEmu / EmuPerTwip)).Append(@"\pichgoal").Append(N(image.HeightEmu / EmuPerTwip)).Append(' ');
            _parts.Add((_body, data));
            _body = new StringBuilder();
        }

        private void Table(Table table)
        {
            var edges = new long[table.Grid.Count + MaxColumns + 1];
            for (var i = 1; i < edges.Length; i++) edges[i] = edges[i - 1] + (i <= table.Grid.Count && table.Grid[i - 1] > 0 ? table.Grid[i - 1] : 1440);
            foreach (var row in table.Rows)
            {
                var layout = Tables.Layout(row);
                _body.Append(@"\trowd\trgaph108");
                if (row.GridBefore > 0) _body.Append(@"\trleft").Append(N(edges[Math.Clamp(row.GridBefore, 0, edges.Length - 1)]));
                foreach (var (column, cell) in layout)
                {
                    if (cell.VMerge == VerticalMerge.Restart) _body.Append(@"\clvmgf");
                    else if (cell.VMerge == VerticalMerge.Continue) _body.Append(@"\clvmrg");
                    _body.Append(@"\clbrdrt").Append(Border).Append(@"\clbrdrl").Append(Border).Append(@"\clbrdrb").Append(Border).Append(@"\clbrdrr").Append(Border);
                    if (Color(document.Theme.Color(cell.Shading)) is { } shading) _body.Append(@"\clcbpat").Append(N(shading));
                    _body.Append(@"\cellx").Append(N(edges[(int)Math.Clamp((long)column + cell.GridSpan, 1, edges.Length - 1)]));
                }

                _body.Append('\n');
                foreach (var (_, cell) in layout)
                {
                    var last = cell.Blocks.Count > 0 ? cell.Blocks[^1] as Paragraph : null;
                    Blocks(last == null ? cell.Blocks : cell.Blocks.Take(cell.Blocks.Count - 1), true);
                    if (last != null) Paragraph(last, true, @"\cell");
                    else _body.Append(@"\pard\plain\intbl\cell").Append('\n');
                }

                _body.Append(@"\row").Append('\n');
            }
        }

        private int Font(string font)
        {
            font = font?.Replace(";", "");
            if (string.IsNullOrEmpty(font)) return 0;
            var index = _fonts.IndexOf(font);
            if (index >= 0) return index;
            _fonts.Add(font);
            return _fonts.Count - 1;
        }

        private int? Color(string color)
        {
            if (color is not { Length: 7 } || color[0] != '#' || !color.Skip(1).All(char.IsAsciiHexDigit)) return null;
            var index = _colors.IndexOf(color.ToUpperInvariant());
            if (index >= 0) return index + 1;
            _colors.Add(color.ToUpperInvariant());
            return _colors.Count;
        }

        private static string N(long value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Field(string argument) => argument.Replace(@"\", @"\\").Replace("\"", "%22");

        private static string Escape(string text)
        {
            var result = new StringBuilder(text.Length);
            foreach (var c in text)
                if (c is '\\' or '{' or '}') result.Append('\\').Append(c);
                else if (c == '\t') result.Append(@"\tab ");
                else if (c is >= ' ' and < (char)0x7F) result.Append(c);
                else result.Append(@"\u").Append(N((short)c)).Append('?');
            return result.ToString();
        }
    }
}
