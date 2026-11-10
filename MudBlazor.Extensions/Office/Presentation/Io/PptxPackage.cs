using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>A slide of the source package with its original XML.</summary>
internal sealed class PptxSlidePart
{
    /// <summary>The slide read into the model.</summary>
    public Slide Slide { get; init; }

    /// <summary>The slide part name.</summary>
    public string PartName { get; init; }

    /// <summary>The relationship id of the slide in the presentation rels.</summary>
    public string RelationshipId { get; init; }

    /// <summary>The <c>p:sldId</c> id.</summary>
    public uint SlideId { get; init; }

    /// <summary>The original slide XML.</summary>
    public XDocument Xml { get; init; }

    /// <summary>The original relationships of the slide.</summary>
    public IReadOnlyList<OpcRelationship> Relationships { get; init; }

    /// <summary>The notes slide part name, or null.</summary>
    public string NotesPart { get; init; }

    /// <summary>The original notes slide XML, or null.</summary>
    public XDocument NotesXml { get; init; }
}

/// <summary>The source package of a presentation; parts the model does not change are copied from it unchanged.</summary>
internal sealed class PptxPackage
{
    /// <summary>The opened package.</summary>
    public OpcPackage Opc { get; init; }

    /// <summary>The presentation part name, usually <c>/ppt/presentation.xml</c>.</summary>
    public string PresentationPart { get; init; }

    /// <summary>The original presentation XML.</summary>
    public XDocument PresentationXml { get; init; }

    /// <summary>The notes master part name, or null if the presentation has none.</summary>
    public string NotesMasterPart { get; init; }

    /// <summary>The slides of the source in file order.</summary>
    public List<PptxSlidePart> Slides { get; } = [];
}
