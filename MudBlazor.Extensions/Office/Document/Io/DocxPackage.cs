using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Document;

internal sealed class DocxPackage
{
    public OpcPackage Opc { get; init; }

    public string MainPart { get; init; }

    public string MainContentType { get; init; }

    public XDocument MainXml { get; init; }

    public IReadOnlyList<OpcRelationship> MainRelationships { get; init; }

    public string StylesPart { get; init; }

    public XDocument StylesXml { get; init; }

    public string NumberingPart { get; init; }

    public XDocument NumberingXml { get; init; }

    public string SettingsPart { get; init; }

    public XDocument SettingsXml { get; init; }

    public string ThemePart { get; init; }

    public IReadOnlyList<Block> Blocks { get; init; }
}
