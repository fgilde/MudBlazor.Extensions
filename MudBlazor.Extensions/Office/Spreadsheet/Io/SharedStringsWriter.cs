using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal sealed class SharedStringsWriter
{
    private static readonly Regex EscapeLike = new("_x[0-9A-Fa-f]{4}_", RegexOptions.CultureInvariant);

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
        w.WriteString(Escape(text));
        w.WriteEndElement();
    }

    private static bool Valid(char c, bool breaks = false) =>
        c is >= ' ' and < (char)0xD800 or > (char)0xDFFF and < (char)0xFFFE || !breaks && c is '\t' or '\n';

    internal static string Escape(string s, bool literals = true, bool breaks = false)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var plain = true;
        foreach (var c in s)
        {
            if (Valid(c, breaks)) continue;
            plain = false;
            break;
        }

        if (plain && (!literals || !s.Contains("_x", StringComparison.Ordinal))) return s;
        var escaped = literals ? EscapeLike.Replace(s, m => "_x005F" + m.Value) : s;
        var sb = new StringBuilder(escaped.Length + 8);
        for (var i = 0; i < escaped.Length; i++)
        {
            var c = escaped[i];
            if (char.IsHighSurrogate(c) && i + 1 < escaped.Length && char.IsLowSurrogate(escaped[i + 1]))
            {
                sb.Append(c).Append(escaped[++i]);
                continue;
            }

            if (Valid(c, breaks)) sb.Append(c);
            else sb.Append("_x").Append(((int)c).ToString("X4", System.Globalization.CultureInfo.InvariantCulture)).Append('_');
        }

        return sb.ToString();
    }
}
