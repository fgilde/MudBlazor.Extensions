using System.Xml;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office;

internal sealed class BoundedXmlReader(XmlReader inner, int maxDepth = BoundedXmlReader.MaxDepth, long maxElements = long.MaxValue) : XmlReader
{
    internal const int MaxDepth = 256;

    internal long Elements { get; private set; }

    internal static XDocument Load(XmlReader reader, int maxDepth = MaxDepth, long maxElements = long.MaxValue) =>
        Load(reader, maxDepth, maxElements, out _);

    internal static XDocument Load(Stream stream) => Load(XmlReader.Create(stream, ReaderSettings(false, DtdProcessing.Prohibit)));

    internal static XDocument Parse(string text, bool preserveWhitespace = false, DtdProcessing dtd = DtdProcessing.Prohibit) =>
        Load(XmlReader.Create(new StringReader(text), ReaderSettings(preserveWhitespace, dtd)));

    private static XmlReaderSettings ReaderSettings(bool preserveWhitespace, DtdProcessing dtd) =>
        new() { DtdProcessing = dtd, XmlResolver = null, IgnoreWhitespace = !preserveWhitespace };

    internal static XDocument Load(XmlReader reader, int maxDepth, long maxElements, out long elements)
    {
        using var bounded = new BoundedXmlReader(reader, maxDepth, maxElements);
        var document = XDocument.Load(bounded);
        elements = bounded.Elements;
        return document;
    }

    public override bool Read()
    {
        if (!inner.Read()) return false;
        if (inner.NodeType != XmlNodeType.Element) return true;
        if (inner.Depth >= maxDepth) throw LoadLimit.Exceeded($"The XML is nested deeper than {maxDepth} levels.");
        if (++Elements > maxElements) throw LoadLimit.Exceeded("The XML has too many elements.");
        return true;
    }

    public override int AttributeCount => inner.AttributeCount;

    public override string BaseURI => inner.BaseURI;

    public override int Depth => inner.Depth;

    public override bool EOF => inner.EOF;

    public override bool HasValue => inner.HasValue;

    public override bool IsDefault => inner.IsDefault;

    public override bool IsEmptyElement => inner.IsEmptyElement;

    public override string LocalName => inner.LocalName;

    public override string Name => inner.Name;

    public override string NamespaceURI => inner.NamespaceURI;

    public override XmlNameTable NameTable => inner.NameTable;

    public override XmlNodeType NodeType => inner.NodeType;

    public override string Prefix => inner.Prefix;

    public override char QuoteChar => inner.QuoteChar;

    public override ReadState ReadState => inner.ReadState;

    public override XmlReaderSettings Settings => inner.Settings;

    public override string Value => inner.Value;

    public override string XmlLang => inner.XmlLang;

    public override XmlSpace XmlSpace => inner.XmlSpace;

    public override bool CanResolveEntity => inner.CanResolveEntity;

    public override string GetAttribute(int i) => inner.GetAttribute(i);

    public override string GetAttribute(string name) => inner.GetAttribute(name);

    public override string GetAttribute(string name, string namespaceURI) => inner.GetAttribute(name, namespaceURI);

    public override string LookupNamespace(string prefix) => inner.LookupNamespace(prefix);

    public override void MoveToAttribute(int i) => inner.MoveToAttribute(i);

    public override bool MoveToAttribute(string name) => inner.MoveToAttribute(name);

    public override bool MoveToAttribute(string name, string ns) => inner.MoveToAttribute(name, ns);

    public override bool MoveToElement() => inner.MoveToElement();

    public override bool MoveToFirstAttribute() => inner.MoveToFirstAttribute();

    public override bool MoveToNextAttribute() => inner.MoveToNextAttribute();

    public override bool ReadAttributeValue() => inner.ReadAttributeValue();

    public override void ResolveEntity() => inner.ResolveEntity();

    public override void Close() => inner.Close();

    protected override void Dispose(bool disposing)
    {
        if (disposing) inner.Dispose();
        base.Dispose(disposing);
    }
}
