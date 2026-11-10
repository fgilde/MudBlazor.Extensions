using System.Xml.Linq;
using MudBlazor.Extensions.Office.Spreadsheet;
using static MudBlazor.Extensions.Office.Presentation.PptxReader;

namespace MudBlazor.Extensions.Office.Presentation;

internal static class TextBodyXml
{
    private static readonly string[] AfterFill =
        ["effectLst", "effectDag", "highlight", "uLnTx", "uLn", "uFillTx", "uFill", "latin", "ea", "cs", "sym", "hlinkClick", "hlinkMouseOver", "rtl", "extLst"];

    internal static void Write(XElement shape, TextBody body)
    {
        if (shape.Name == P + "cxnSp") return;
        var txBody = shape.Element(P + "txBody");
        if (body == null)
        {
            txBody?.Remove();
            return;
        }

        txBody ??= SlideXml.Place(shape, new XElement(P + "txBody", new XElement(A + "bodyPr"), new XElement(A + "lstStyle")), "extLst");
        var bodyPr = txBody.Element(A + "bodyPr");
        if (bodyPr == null) txBody.AddFirst(bodyPr = new XElement(A + "bodyPr"));
        Set(bodyPr, "anchor", TextStyles.ParseAnchor((string)bodyPr.Attribute("anchor")), body.Anchor, a => a switch
        {
            TextAnchor.Middle => "ctr",
            TextAnchor.Bottom => "b",
            _ => "t"
        });
        Set(bodyPr, "lIns", Long((string)bodyPr.Attribute("lIns")), body.InsetLeft, Format);
        Set(bodyPr, "tIns", Long((string)bodyPr.Attribute("tIns")), body.InsetTop, Format);
        Set(bodyPr, "rIns", Long((string)bodyPr.Attribute("rIns")), body.InsetRight, Format);
        Set(bodyPr, "bIns", Long((string)bodyPr.Attribute("bIns")), body.InsetBottom, Format);
        Set(bodyPr, "wrap", ShapeReader.ParseWrap((string)bodyPr.Attribute("wrap")), body.Wrap, w => w ? "square" : "none");
        txBody.Elements(A + "p").Remove();
        txBody.Add(body.Paragraphs.Count == 0 ? [new XElement(A + "p")] : body.Paragraphs.Select(Paragraph));
    }

    internal static void WriteNotes(XDocument notes, string text)
    {
        if (NotesText(notes) == text || NotesBody(notes) is not { } body) return;
        var txBody = body.Element(P + "txBody") ?? SlideXml.Place(body, new XElement(P + "txBody", new XElement(A + "bodyPr"), new XElement(A + "lstStyle")), "extLst");
        var template = txBody.Elements(A + "p").FirstOrDefault();
        var pPr = template?.Element(A + "pPr");
        var rPr = template?.Elements(A + "r").FirstOrDefault()?.Element(A + "rPr");
        txBody.Elements(A + "p").Remove();
        txBody.Add(text.Split('\n').Select(line => new XElement(A + "p", pPr == null ? null : new XElement(pPr),
            line.Length == 0 ? null : new XElement(A + "r", rPr == null ? null : new XElement(rPr), new XElement(A + "t", SharedStringsWriter.Escape(line))))));
    }

    private static XElement Paragraph(TextParagraph paragraph)
    {
        var p = paragraph.Source != null ? new XElement(paragraph.Source) : new XElement(A + "p");
        p.Elements().Where(e => RunText(e) != null).Remove();
        WriteParagraphProperties(p, paragraph);
        var end = p.Element(A + "endParaRPr");
        XElement previous = null;
        foreach (var run in paragraph.Runs)
        {
            var r = Run(run, previous);
            previous = r.Element(A + "rPr");
            if (end != null) end.AddBeforeSelf(r);
            else p.Add(r);
        }

        if (paragraph.EndFormat == null) end?.Remove();
        else if (end != null) WriteRunFormat(end, paragraph.EndFormat);
        else
        {
            p.Add(end = new XElement(A + "endParaRPr"));
            WriteRunFormat(end, paragraph.EndFormat);
        }

        return p;
    }

    private static void WriteParagraphProperties(XElement p, TextParagraph paragraph)
    {
        var pPr = p.Element(A + "pPr");
        var level = ShapeReader.ParseLevel(pPr);
        var align = TextStyles.ParseAlign((string)pPr?.Attribute("algn"));
        var bullet = ShapeReader.ParseBullet(pPr);
        if (level == paragraph.Level && align == paragraph.Align && bullet == paragraph.Bullet) return;
        if (pPr == null) p.AddFirst(pPr = new XElement(A + "pPr"));
        if (level != paragraph.Level) pPr.SetAttributeValue("lvl", paragraph.Level == 0 ? null : paragraph.Level);
        Set(pPr, "algn", align, paragraph.Align, a => a switch
        {
            TextAlign.Center => "ctr",
            TextAlign.Right => "r",
            TextAlign.Justify => "just",
            _ => "l"
        });
        if (bullet == paragraph.Bullet) return;
        pPr.Elements().Where(b => b.Name.LocalName is "buNone" or "buChar" or "buAutoNum" or "buBlip").Remove();
        SlideXml.Place(pPr, paragraph.Bullet switch
        {
            true => new XElement(A + "buChar", new XAttribute("char", "•")),
            false => new XElement(A + "buNone"),
            null => null
        }, "tabLst", "defRPr", "extLst");
    }

    private static XElement Run(TextRun run, XElement previous)
    {
        var name = run.Text == "\n" ? A + "br" : A + "r";
        XElement r;
        if (run.Source != null && (run.Source.Name == name || name == A + "r" && run.Source.Name == A + "fld")) r = new XElement(run.Source);
        else
        {
            var template = run.Source?.Element(A + "rPr") ?? previous;
            r = new XElement(name, template == null ? null : new XElement(template));
        }

        var rPr = r.Element(A + "rPr");
        if (rPr == null && run.Format != new RunFormat()) r.AddFirst(rPr = new XElement(A + "rPr"));
        if (rPr != null) WriteRunFormat(rPr, run.Format);
        if (name == A + "br") return r;
        var t = r.Element(A + "t");
        if (t == null) r.Add(t = new XElement(A + "t"));
        if (SheetXmlReader.Decode(t.Value) == run.Text) return r;
        t.Value = SharedStringsWriter.Escape(run.Text);
        if (r.Name != A + "fld") return r;
        r.Name = A + "r";
        r.RemoveAttributes();
        r.Element(A + "pPr")?.Remove();
        return r;
    }

    private static void WriteRunFormat(XElement rPr, RunFormat format)
    {
        var before = ReadRunFormat(rPr);
        if (before == format) return;
        Set(rPr, "b", before.Bold, format.Bold, Flag);
        Set(rPr, "i", before.Italic, format.Italic, Flag);
        Set(rPr, "u", before.Underline, format.Underline, u => u ? "sng" : "none");
        Set(rPr, "strike", before.Strike, format.Strike, s => s ? "sngStrike" : "noStrike");
        Set(rPr, "sz", before.SizePt, format.SizePt, s => Format((long)Math.Round(s * 100)));
        var fill = format.Color == null ? null : SlideXml.SolidFill(format.Color);
        if (before.Color != format.Color && (fill != null || format.Color == null))
        {
            rPr.Elements().Where(f => SlideXml.Fills.Contains(f.Name.LocalName)).Remove();
            SlideXml.Place(rPr, fill, AfterFill);
        }

        if (before.Font == format.Font) return;
        var latin = rPr.Element(A + "latin");
        if (format.Font == null) latin?.Remove();
        else (latin ?? SlideXml.Place(rPr, new XElement(A + "latin"), "ea", "cs", "sym", "hlinkClick", "hlinkMouseOver", "rtl", "extLst")).SetAttributeValue("typeface", format.Font);
    }

    private static void Set<T>(XElement element, string name, T? current, T? value, Func<T, string> format) where T : struct
    {
        if (!Nullable.Equals(current, value)) element.SetAttributeValue(name, value is { } v ? format(v) : null);
    }

    private static string Flag(bool value) => value ? "1" : "0";

    private static string Format(long value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
