using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Presentation;

internal sealed class PptxSlidePart
{
    public Slide Slide { get; init; }

    public string PartName { get; init; }

    public string RelationshipId { get; init; }

    public uint SlideId { get; init; }

    public XDocument Xml { get; init; }

    public IReadOnlyList<OpcRelationship> Relationships { get; init; }

    public string NotesPart { get; init; }

    public XDocument NotesXml { get; init; }
}

internal sealed class PptxPackage
{
    public OpcPackage Opc { get; init; }

    public string PresentationPart { get; init; }

    public XDocument PresentationXml { get; init; }

    public string NotesMasterPart { get; init; }

    public List<PptxSlidePart> Slides { get; } = [];
}
