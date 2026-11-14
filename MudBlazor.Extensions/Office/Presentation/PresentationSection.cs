using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>A named section of a presentation: a run of consecutive slides.</summary>
public sealed class PresentationSection
{
    /// <summary>The name of the section PowerPoint creates before the first section when the deck had none.</summary>
    public const string DefaultName = "Default Section";

    private readonly Presentation _owner;

    internal PresentationSection(Presentation owner, string id, string name)
    {
        _owner = owner;
        Id = id;
        Name = name;
    }

    /// <summary>The section id, a GUID in braces.</summary>
    public string Id { get; }

    /// <summary>The section name.</summary>
    public string Name { get; internal set; }

    /// <summary>The slides of the section in show order; empty for an empty section or a section that was removed.</summary>
    public IReadOnlyList<Slide> Slides =>
        _owner.SectionList.IndexOf(this) is var index and >= 0
            ? _owner.SlideList.GetRange(_owner.SectionList.Take(index).Sum(s => s.Count), Count)
            : [];

    internal int Count { get; set; }

    internal XElement Source { get; init; }

    internal static string NewId() => Guid.NewGuid().ToString("B").ToUpperInvariant();
}
