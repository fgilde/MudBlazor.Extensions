namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>
/// Starts a new section at a slide: the section that contains the slide is split before it. If the presentation has no sections yet and the slide is not the
/// first one, a default section is created for the slides before it.
/// </summary>
/// <param name="firstSlide">The first slide of the new section.</param>
/// <param name="name">The name of the new section.</param>
/// <param name="defaultSectionName">The name of the default section, if one is created.</param>
public sealed class AddSectionCommand(Slide firstSlide, string name, string defaultSectionName = PresentationSection.DefaultName) : IDocumentCommand<Presentation>
{
    private SectionState _before;
    private PresentationSection _default;

    /// <summary>The created section, available after the command was applied.</summary>
    public PresentationSection Section { get; private set; }

    /// <inheritdoc />
    public string Description => "Add section";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        ArgumentNullException.ThrowIfNull(name);
        var index = document.SlideList.IndexOf(firstSlide);
        if (index < 0) throw new ArgumentException("The slide must belong to the presentation.", nameof(firstSlide));
        _before = new SectionState(document);
        Section ??= new PresentationSection(document, PresentationSection.NewId(), name);
        var sections = document.SectionList;
        if (sections.Count == 0)
        {
            if (index > 0)
            {
                _default ??= new PresentationSection(document, PresentationSection.NewId(), defaultSectionName ?? PresentationSection.DefaultName);
                _default.Count = index;
                sections.Add(_default);
            }

            Section.Count = document.SlideList.Count - index;
            sections.Add(Section);
        }
        else
        {
            var split = document.SectionAt(index);
            var start = sections.TakeWhile(s => s != split).Sum(s => s.Count);
            Section.Count = start + split.Count - index;
            split.Count = index - start;
            sections.Insert(sections.IndexOf(split) + 1, Section);
        }

        document.Notify(null, PresentationChangeKind.Sections);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        _before.Restore();
        document.Notify(null, PresentationChangeKind.Sections);
    }
}

/// <summary>Renames a section.</summary>
/// <param name="section">The section.</param>
/// <param name="name">The new name.</param>
public sealed class RenameSectionCommand(PresentationSection section, string name) : IDocumentCommand<Presentation>
{
    private string _before;

    /// <inheritdoc />
    public string Description => "Rename section";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        ArgumentNullException.ThrowIfNull(name);
        SectionState.Validate(document, section);
        if (section.Name == name) return;
        _before = section.Name;
        section.Name = name;
        document.Notify(null, PresentationChangeKind.Sections);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        section.Name = _before;
        document.Notify(null, PresentationChangeKind.Sections);
    }
}

/// <summary>
/// Removes a section. Its slides are deleted, or they join the previous section (the next one for the first section). Removing the last remaining section
/// removes the sections altogether.
/// </summary>
/// <param name="section">The section.</param>
/// <param name="removeSlides">True to delete the slides of the section as well.</param>
public sealed class RemoveSectionCommand(PresentationSection section, bool removeSlides) : IDocumentCommand<Presentation>
{
    private SectionState _before;
    private DeleteSlidesCommand _delete;

    /// <inheritdoc />
    public string Description => "Remove section";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        SectionState.Validate(document, section);
        _before = new SectionState(document);
        if (removeSlides && section.Count > 0)
        {
            _delete ??= new DeleteSlidesCommand(section.Slides.ToList());
            _delete.Apply(document);
        }

        var sections = document.SectionList;
        var index = sections.IndexOf(section);
        if (sections.Count == 1) sections.Clear();
        else
        {
            sections[index > 0 ? index - 1 : 1].Count += section.Count;
            sections.RemoveAt(index);
        }

        document.Notify(null, PresentationChangeKind.Sections);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        _delete?.Revert(document);
        _before.Restore();
        document.Notify(null, PresentationChangeKind.Sections);
    }
}

/// <summary>Removes all sections; the slides stay.</summary>
public sealed class RemoveAllSectionsCommand : IDocumentCommand<Presentation>
{
    private SectionState _before;

    /// <inheritdoc />
    public string Description => "Remove all sections";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        if (document.SectionList.Count == 0) return;
        _before = new SectionState(document);
        document.SectionList.Clear();
        document.Notify(null, PresentationChangeKind.Sections);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        _before.Restore();
        document.Notify(null, PresentationChangeKind.Sections);
    }
}

/// <summary>Moves a section together with its slides.</summary>
/// <param name="section">The section.</param>
/// <param name="targetIndex">The position of the section after the move, from 0 to the section count minus one.</param>
public sealed class MoveSectionCommand(PresentationSection section, int targetIndex) : IDocumentCommand<Presentation>
{
    private SectionState _before;
    private Slide[] _slides;

    /// <inheritdoc />
    public string Description => "Move section";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        SectionState.Validate(document, section);
        var sections = document.SectionList;
        if (targetIndex < 0 || targetIndex >= sections.Count) throw new ArgumentOutOfRangeException(nameof(targetIndex));
        if (sections.IndexOf(section) == targetIndex) return;
        _before = new SectionState(document);
        _slides = document.SlideList.ToArray();
        var members = sections.ToDictionary(s => s, s => s.Slides);
        sections.Remove(section);
        sections.Insert(targetIndex, section);
        document.SlideList.Clear();
        document.SlideList.AddRange(sections.SelectMany(s => members[s]));
        Notify(document);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        document.SlideList.Clear();
        document.SlideList.AddRange(_slides);
        _before.Restore();
        Notify(document);
    }

    private void Notify(Presentation document)
    {
        for (var i = 0; i < _slides.Length; i++)
            if (_slides[i] != document.SlideList[i])
                document.Notify(_slides[i], PresentationChangeKind.SlideOrder);
        document.Notify(null, PresentationChangeKind.Sections);
    }
}

internal sealed class SectionState(Presentation presentation)
{
    private readonly (PresentationSection Section, int Count)[] _sections = presentation.SectionList.Select(s => (s, s.Count)).ToArray();

    internal static void Validate(Presentation presentation, PresentationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);
        if (!presentation.SectionList.Contains(section)) throw new ArgumentException("The section must belong to the presentation.", nameof(section));
    }

    internal void Restore()
    {
        presentation.SectionList.Clear();
        foreach (var (section, count) in _sections)
        {
            section.Count = count;
            presentation.SectionList.Add(section);
        }
    }

    internal void Inserted(int index, int count)
    {
        if (presentation.SectionList.Count > 0) (index == 0 ? presentation.SectionList[0] : presentation.SectionAt(index - 1)).Count += count;
    }

    internal void Removing(int index)
    {
        if (presentation.SectionAt(index) is { } section) section.Count--;
    }
}
