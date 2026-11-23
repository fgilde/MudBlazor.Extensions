using System.Net;
using System.Text;

namespace MudBlazor.Extensions.Office.Document;

internal static class HtmlWriter
{
    internal static readonly HashSet<string> ImageTypes = ["image/png", "image/jpeg", "image/gif", "image/bmp"];

    internal static string Write(TextDocument document) => new Writer(document).Run();

    internal static bool IsPreformatted(TextDocument document, Paragraph paragraph) =>
        paragraph.Inlines.Any(i => i is TextRun) &&
        paragraph.Inlines.Where(i => i is TextRun or Tab).All(i => document.EffectiveRunFormat(paragraph, i).Font == HtmlReader.Monospace);

    private sealed class Writer(TextDocument document)
    {
        private readonly StringBuilder _html = new();
        private readonly RunFormat _defaults = document.EffectiveRunFormat(new Paragraph(), null);
        private readonly ParagraphFormat _paragraphDefaults = document.EffectiveParagraphFormat(new Paragraph());

        public string Run()
        {
            _html.Append("<!DOCTYPE html>\n<html><head><meta charset=\"utf-8\"><title>").Append(Encode(document.Properties.Title ?? "")).Append("</title>\n<style>body{");
            if (DocumentCss.FontName(_defaults.Font) is { } font) _html.Append("font-family:'").Append(font).Append("';");
            if (DocumentCss.Size(_defaults.SizePt) is { } size) _html.Append("font-size:").Append(DocumentCss.Number(size)).Append("pt;");
            _html.Append("}h1,h2,h3,h4,h5,h6{font-size:1em;font-weight:normal}table{border-collapse:collapse}td{border:1px solid #000;padding:0 5.4pt;vertical-align:top}")
                .Append("</style>\n</head><body>\n");
            Blocks(document.Blocks);
            return _html.Append("</body></html>\n").ToString();
        }

        private void Blocks(IEnumerable<Block> blocks)
        {
            var lists = new List<(string Tag, string Type, int NumId)>();
            foreach (var block in blocks)
                if (block is Paragraph paragraph)
                    Paragraph(paragraph, lists);
                else
                {
                    CloseLists(lists);
                    if (block is Table table) Table(table);
                    else if (block is OpaqueBlock { Visible: true, Preview: { } preview }) Blocks(preview);
                }

            CloseLists(lists);
        }

        private void Paragraph(Paragraph paragraph, List<(string Tag, string Type, int NumId)> lists)
        {
            var format = document.EffectiveParagraphFormat(paragraph);
            if (format.PageBreakBefore == true)
            {
                CloseLists(lists);
                _html.Append("<div style=\"break-after:page\"></div>");
            }

            var level = format.Numbering is { } numbering ? document.Numbering.Level(numbering) : null;
            var heading = format.OutlineLevel is { } outline and < 9 ? "h" + Math.Min(outline + 1, 6) : null;
            var tag = heading ?? (level != null ? null : IsPreformatted(document, paragraph) ? "pre" : "p");
            var css = DocumentCss.Paragraph(format, _paragraphDefaults, level != null, tag != "pre" && NeedsPreWrap(paragraph.Text));
            if (level != null)
            {
                Item(lists, format.Numbering!.Value, level.Format);
                _html.Append(tag == null ? Open("li", css) : "<li>");
            }
            else
                CloseLists(lists);

            var open = tag == null ? "" : Open(tag, css);
            _html.Append(open);
            var empty = true;
            for (var i = 0; i < paragraph.Inlines.Count;)
            {
                var link = paragraph.Inlines[i].Link;
                var j = i;
                while (j < paragraph.Inlines.Count && ReferenceEquals(paragraph.Inlines[j].Link, link) && paragraph.Inlines[j] is not Break { Kind: BreakKind.Page }) j++;
                var href = link?.SafeTarget;
                if (href != null && j > i) _html.Append("<a href=\"").Append(Encode(href)).Append("\">");
                for (var k = i; k < j; k++) empty &= !Inline(paragraph, paragraph.Inlines[k]);
                if (href != null && j > i) _html.Append("</a>");
                if (j < paragraph.Inlines.Count && paragraph.Inlines[j] is Break { Kind: BreakKind.Page })
                {
                    if (tag == null)
                        _html.Append("<div style=\"break-after:page\"></div>");
                    else
                        _html.Append("</").Append(tag).Append("><div style=\"break-after:page\"></div>").Append(open);
                    j++;
                }

                i = j;
            }

            if (empty) _html.Append("<br>");
            if (tag != null) _html.Append("</").Append(tag).Append(level != null ? ">" : ">\n");
        }

        private static bool NeedsPreWrap(string text) =>
            text.Contains("  ", StringComparison.Ordinal) || text.Contains('\t') || text.StartsWith(' ') || text.EndsWith(' ') ||
            text.Contains(" \v", StringComparison.Ordinal) || text.Contains("\v ", StringComparison.Ordinal);

        private void Item(List<(string Tag, string Type, int NumId)> lists, NumberingRef numbering, NumberFormat numberFormat)
        {
            var list = numberFormat is NumberFormat.Bullet ? ("ul", null, numbering.NumId) : numberFormat is NumberFormat.None ? ("ul", "none", numbering.NumId) : ("ol", numberFormat switch
            {
                NumberFormat.DecimalZero => "decimal-leading-zero",
                NumberFormat.LowerLetter => "lower-alpha",
                NumberFormat.UpperLetter => "upper-alpha",
                NumberFormat.LowerRoman => "lower-roman",
                NumberFormat.UpperRoman => "upper-roman",
                _ => "decimal"
            }, numbering.NumId);
            while (lists.Count > numbering.Level + 1 || lists.Count == numbering.Level + 1 && lists[^1] != list) CloseList(lists);
            if (lists.Count == numbering.Level + 1) _html.Append("</li>");
            while (lists.Count < numbering.Level + 1)
            {
                _html.Append('<').Append(list.Item1).Append(list.Item2 == null ? "" : $" style=\"list-style-type:{list.Item2}\"").Append('>');
                lists.Add(list);
                if (lists.Count < numbering.Level + 1) _html.Append("<li>");
            }
        }

        private void CloseList(List<(string Tag, string Type, int NumId)> lists)
        {
            _html.Append("</li></").Append(lists[^1].Tag).Append('>');
            lists.RemoveAt(lists.Count - 1);
            if (lists.Count == 0) _html.Append('\n');
        }

        private void CloseLists(List<(string Tag, string Type, int NumId)> lists)
        {
            while (lists.Count > 0) CloseList(lists);
        }

        private bool Inline(Paragraph paragraph, Inline inline)
        {
            switch (inline)
            {
                case Marker { Kind: "bookmarkStart", Name: { Length: > 0 } name }:
                    _html.Append("<a id=\"").Append(Encode(name)).Append("\"></a>");
                    return false;
                case Marker:
                    return false;
                case Break:
                    _html.Append("<br>");
                    return true;
                case InlineImage image:
                    return Image(image);
                case TextRun run:
                    return Span(paragraph, inline, Encode(run.Text));
                case Tab:
                    return Span(paragraph, inline, "\t");
                case OpaqueInline token:
                    return Span(paragraph, inline, Encode(token.DisplayText));
                default:
                    return false;
            }
        }

        private bool Span(Paragraph paragraph, Inline inline, string content)
        {
            var format = document.EffectiveRunFormat(paragraph, inline);
            if (format.Hidden == true || content.Length == 0) return false;
            var css = DocumentCss.Run(format, _defaults);
            if (css.Length == 0) _html.Append(content);
            else _html.Append("<span style=\"").Append(css).Append("\">").Append(content).Append("</span>");
            return true;
        }

        private bool Image(InlineImage image)
        {
            var data = document.GetMedia(image.MediaPart);
            var type = document.MediaContentType(image.MediaPart)?.ToLowerInvariant();
            if (data == null || type == null || !ImageTypes.Contains(type)) return false;
            var (width, height) = DocumentCss.ImageSize(image.WidthEmu, image.HeightEmu);
            _html.Append("<img src=\"data:").Append(type).Append(";base64,").Append(Convert.ToBase64String(data)).Append("\" alt=\"").Append(Encode(image.AltText ?? ""))
                .Append("\" width=\"").Append(width).Append("\" height=\"").Append(height).Append("\">");
            return true;
        }

        private void Table(Table table)
        {
            _html.Append("<table>\n");
            for (var r = 0; r < table.Rows.Count; r++)
            {
                _html.Append("<tr>");
                foreach (var (_, cell, rows) in Tables.Visible(table, r))
                {
                    _html.Append("<td");
                    if (cell.GridSpan > 1) _html.Append(" colspan=\"").Append(cell.GridSpan).Append('"');
                    if (rows > 1) _html.Append(" rowspan=\"").Append(rows).Append('"');
                    if (DocumentCss.Color(document.Theme.Color(cell.Shading)) is { } shading) _html.Append(" style=\"background-color:").Append(shading).Append('"');
                    _html.Append('>');
                    Blocks(cell.Blocks);
                    _html.Append("</td>");
                }

                _html.Append("</tr>\n");
            }

            _html.Append("</table>\n");
        }

        private static string Open(string tag, string css) => css.Length == 0 ? $"<{tag}>" : $"<{tag} style=\"{css}\">";

        private static string Encode(string text) => WebUtility.HtmlEncode(text);
    }
}
