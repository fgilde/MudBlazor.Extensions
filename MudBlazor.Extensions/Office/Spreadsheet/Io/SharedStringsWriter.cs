using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal sealed class SharedStringsWriter
{
    private static readonly Regex EscapeLike = new("_x[0-9A-Fa-f]{4}_", RegexOptions.CultureInvariant);

    private readonly Dictionary<string, int> _plain = new(StringComparer.Ordinal);
    private readonly Dictionary<int, int> _rich = new();
    private readonly List<object> _items = [];

    internal int References { get; private set; }

    internal int Count => _items.Count;

    internal int Add(string text)
    {
        References++;
        if (_plain.TryGetValue(text, out var index)) return index;
        _plain[text] = index = _items.Count;
        _items.Add(text);
        return index;
    }

    internal int AddRich(int sourceIndex, XElement si)
    {
        References++;
        if (_rich.TryGetValue(sourceIndex, out var index)) return index;
        _rich[sourceIndex] = index = _items.Count;
        _items.Add(si);
        return index;
    }

    internal void Write(Stream stream, XNamespace ns)
    {
        using var w = XmlWriter.Create(stream, SheetXmlWriter.Settings);
        w.WriteStartDocument(true);
        w.WriteStartElement("sst", ns.NamespaceName);
        w.WriteAttributeString("count", References.ToString(System.Globalization.CultureInfo.InvariantCulture));
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

    private static bool Valid(char c) => c is >= ' ' and < (char)0xD800 or '\t' or '\n' or > (char)0xDFFF and < (char)0xFFFE;

    internal static string Escape(string s, bool literals = true)
    {
        var plain = true;
        foreach (var c in s)
        {
            if (Valid(c)) continue;
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

            if (Valid(c)) sb.Append(c);
            else sb.Append("_x").Append(((int)c).ToString("X4", System.Globalization.CultureInfo.InvariantCulture)).Append('_');
        }

        return sb.ToString();
    }
}
