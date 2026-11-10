using System.Xml;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal sealed class SharedStringsWriter
{
    private readonly Dictionary<string, int> _plain = new(StringComparer.Ordinal);
    private readonly List<object> _items = [];
    private readonly int _seeded = -1;

    internal SharedStringsWriter()
    {
    }

    internal SharedStringsWriter(Stream source)
    {
        using var reader = XmlReader.Create(source, SheetXmlReader.Settings);
        while (!reader.EOF)
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "si")
            {
                reader.Read();
                continue;
            }

            var si = (XElement)XNode.ReadFrom(reader);
            var text = SheetXmlReader.Text(si, out var rich);
            if (!rich) _plain.TryAdd(text, _items.Count);
            _items.Add(si);
        }

        _seeded = _items.Count;
    }

    internal int References { get; private set; }

    internal bool Unchanged => _items.Count == _seeded;

    internal int Add(string text)
    {
        References++;
        if (_plain.TryGetValue(text, out var index)) return index;
        _plain[text] = index = _items.Count;
        _items.Add(text);
        return index;
    }

    internal int AddRich(int sourceIndex)
    {
        References++;
        return sourceIndex;
    }

    internal void Write(Stream stream, XNamespace ns)
    {
        using var w = XmlWriter.Create(stream, SheetXmlWriter.Settings);
        w.WriteStartDocument(true);
        w.WriteStartElement("sst", ns.NamespaceName);
        if (_seeded < 0) w.WriteAttributeString("count", References.ToString(System.Globalization.CultureInfo.InvariantCulture));
        w.WriteAttributeString("uniqueCount", _items.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        foreach (var item in _items)
        {
            if (item is XElement si)
            {
                si.WriteTo(w);
                continue;
            }

            w.WriteStartElement("si", ns.NamespaceName);
            WriteText(w, "t", ns, (string)item);
            w.WriteEndElement();
        }

        w.WriteEndElement();
    }

    internal static void WriteText(XmlWriter w, string element, XNamespace ns, string text)
    {
        w.WriteStartElement(element, ns.NamespaceName);
        if (text.Length > 0 && (char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[^1]))) w.WriteAttributeString("xml", "space", null, "preserve");
        w.WriteString(OoxmlText.Escape(text));
        w.WriteEndElement();
    }
}
