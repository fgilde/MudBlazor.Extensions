namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>Adds a section with one empty page.</summary>
/// <param name="index">The position of the new section, from 0 to the section count.</param>
/// <param name="name">The section name.</param>
/// <param name="color">The tab colour as <c>#RRGGBB</c>.</param>
public sealed class AddSectionCommand(int index, string name, string color) : IDocumentCommand<Notebook>
{
    private StructureEdit _edit;

    /// <summary>The created section, available after the command was applied.</summary>
    public NotebookSection Section { get; private set; }

    /// <inheritdoc />
    public string Description => "New section";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = StructureEdit.Run(document, _edit, NotebookChangeKind.Sections, () =>
    {
        if (index < 0 || index > document.SectionList.Count) throw new ArgumentOutOfRangeException(nameof(index));
        Section = new NotebookSection { Name = name ?? "", Color = NotebookEdit.Color(color, nameof(color)) };
        Section.PageList.Add(new NotebookPage());
        document.SectionList.Insert(index, Section);
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Renames a section.</summary>
/// <param name="section">The section.</param>
/// <param name="name">The new name.</param>
public sealed class RenameSectionCommand(NotebookSection section, string name) : IDocumentCommand<Notebook>
{
    private string _before;

    /// <inheritdoc />
    public string Description => "Rename section";

    /// <inheritdoc />
    public void Apply(Notebook document)
    {
        NotebookEdit.IndexOf(document, section);
        if (section.Name == (name ?? "")) return;
        _before = section.Name;
        section.Name = name ?? "";
        document.Notify(null, NotebookChangeKind.Sections);
    }

    /// <inheritdoc />
    public void Revert(Notebook document)
    {
        section.Name = _before;
        document.Notify(null, NotebookChangeKind.Sections);
    }
}

/// <summary>Sets the tab colour of a section.</summary>
/// <param name="section">The section.</param>
/// <param name="color">The colour as <c>#RRGGBB</c>.</param>
public sealed class SetSectionColorCommand(NotebookSection section, string color) : IDocumentCommand<Notebook>
{
    private string _before;

    /// <inheritdoc />
    public string Description => "Section color";

    /// <inheritdoc />
    public void Apply(Notebook document)
    {
        NotebookEdit.IndexOf(document, section);
        NotebookEdit.Color(color, nameof(color));
        if (section.Color == color) return;
        _before = section.Color;
        section.Color = color;
        document.Notify(null, NotebookChangeKind.Sections);
    }

    /// <inheritdoc />
    public void Revert(Notebook document)
    {
        section.Color = _before;
        document.Notify(null, NotebookChangeKind.Sections);
    }
}

/// <summary>Deletes a section with its pages. The last section cannot be deleted.</summary>
/// <param name="section">The section.</param>
public sealed class DeleteSectionCommand(NotebookSection section) : IDocumentCommand<Notebook>
{
    private StructureEdit _edit;

    /// <inheritdoc />
    public string Description => "Delete section";

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The section is the only one.</exception>
    public void Apply(Notebook document) => _edit = StructureEdit.Run(document, _edit, NotebookChangeKind.Sections, () =>
    {
        NotebookEdit.IndexOf(document, section);
        if (document.SectionList.Count == 1) throw new InvalidOperationException("A notebook keeps at least one section.");
        document.SectionList.Remove(section);
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Moves a section to a new position.</summary>
/// <param name="section">The section.</param>
/// <param name="targetIndex">The position in the current section list before which the section is placed, from 0 to the section count.</param>
public sealed class MoveSectionCommand(NotebookSection section, int targetIndex) : IDocumentCommand<Notebook>
{
    private StructureEdit _edit;

    /// <inheritdoc />
    public string Description => "Move section";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = StructureEdit.Run(document, _edit, NotebookChangeKind.Sections, () =>
    {
        var from = NotebookEdit.IndexOf(document, section);
        if (targetIndex < 0 || targetIndex > document.SectionList.Count) throw new ArgumentOutOfRangeException(nameof(targetIndex));
        document.SectionList.RemoveAt(from);
        document.SectionList.Insert(targetIndex > from ? targetIndex - 1 : targetIndex, section);
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Adds an empty page to a section.</summary>
/// <param name="section">The section.</param>
/// <param name="index">The position of the new page, from 0 to the page count of the section.</param>
/// <param name="title">The page title.</param>
public sealed class AddPageCommand(NotebookSection section, int index, string title = "") : IDocumentCommand<Notebook>
{
    private StructureEdit _edit;

    /// <summary>The created page, available after the command was applied.</summary>
    public NotebookPage Page { get; private set; }

    /// <inheritdoc />
    public string Description => "New page";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = StructureEdit.Run(document, _edit, NotebookChangeKind.Pages, () =>
    {
        NotebookEdit.IndexOf(document, section);
        if (index < 0 || index > section.PageList.Count) throw new ArgumentOutOfRangeException(nameof(index));
        Page = new NotebookPage { Title = title ?? "" };
        section.PageList.Insert(index, Page);
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Sets the title of a page.</summary>
/// <param name="page">The page.</param>
/// <param name="title">The new title.</param>
public sealed class RenamePageCommand(NotebookPage page, string title) : IDocumentCommand<Notebook>
{
    private string _before;

    /// <inheritdoc />
    public string Description => "Rename page";

    /// <inheritdoc />
    public void Apply(Notebook document)
    {
        NotebookEdit.Editable(document, page);
        if (page.Title == (title ?? "")) return;
        _before = page.Title;
        page.Title = title ?? "";
        document.Notify(page, NotebookChangeKind.Pages);
    }

    /// <inheritdoc />
    public void Revert(Notebook document)
    {
        page.Title = _before;
        document.Notify(page, NotebookChangeKind.Pages);
    }
}

/// <summary>
/// Deletes pages, also from several sections. A section that loses all its pages gets one empty page; deleting the only, blank page of a section
/// changes nothing.
/// </summary>
/// <param name="pages">The pages.</param>
public sealed class DeletePagesCommand(IReadOnlyList<NotebookPage> pages) : IDocumentCommand<Notebook>
{
    private readonly Dictionary<NotebookSection, NotebookPage> _fresh = new();
    private StructureEdit _edit;

    /// <inheritdoc />
    public string Description => "Delete page";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = StructureEdit.Run(document, _edit, NotebookChangeKind.Pages, () =>
    {
        foreach (var (section, page) in NotebookEdit.Ordered(document, pages))
        {
            if (section.PageList.Count == 1 && page is { LoadError: null, Title.Length: 0, ElementList.Count: 0 }) continue;
            section.PageList.Remove(page);
            NotebookEdit.AddPageIfEmpty(section, _fresh);
        }
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Moves pages as a block, also across sections; they keep their order. A section that loses all its pages gets one empty page.</summary>
/// <param name="pages">The pages.</param>
/// <param name="targetSection">The section that receives the pages.</param>
/// <param name="targetIndex">The position in the current page list of the target section before which the pages are placed, from 0 to its page count.</param>
public sealed class MovePagesCommand(IReadOnlyList<NotebookPage> pages, NotebookSection targetSection, int targetIndex) : IDocumentCommand<Notebook>
{
    private readonly Dictionary<NotebookSection, NotebookPage> _fresh = new();
    private StructureEdit _edit;

    /// <inheritdoc />
    public string Description => "Move page";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = StructureEdit.Run(document, _edit, NotebookChangeKind.Pages, () =>
    {
        var ordered = NotebookEdit.Ordered(document, pages);
        NotebookEdit.IndexOf(document, targetSection);
        if (targetIndex < 0 || targetIndex > targetSection.PageList.Count) throw new ArgumentOutOfRangeException(nameof(targetIndex));
        var moving = ordered.Select(e => e.Page).ToList();
        var at = targetIndex - targetSection.PageList.Take(targetIndex).Count(moving.Contains);
        foreach (var (section, page) in ordered) section.PageList.Remove(page);
        targetSection.PageList.InsertRange(at, moving);
        foreach (var section in ordered.Select(e => e.Section).Distinct()) NotebookEdit.AddPageIfEmpty(section, _fresh);
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Makes pages pages or subpages.</summary>
/// <param name="pages">The pages.</param>
/// <param name="level">0 for a page, 1 or 2 for a subpage.</param>
public sealed class SetPageLevelCommand(IReadOnlyList<NotebookPage> pages, int level) : IDocumentCommand<Notebook>
{
    private List<(NotebookPage Page, int Level)> _before;

    /// <inheritdoc />
    public string Description => "Page level";

    /// <inheritdoc />
    public void Apply(Notebook document)
    {
        if (level is < 0 or > NotebookPage.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
        var ordered = NotebookEdit.Ordered(document, pages);
        foreach (var (_, page) in ordered) NotebookEdit.Editable(document, page);
        _before = ordered.Where(e => e.Page.Level != level).Select(e => (e.Page, e.Page.Level)).ToList();
        foreach (var (page, _) in _before)
        {
            page.Level = level;
            document.Notify(page, NotebookChangeKind.Pages);
        }
    }

    /// <inheritdoc />
    public void Revert(Notebook document)
    {
        foreach (var (page, old) in _before)
        {
            page.Level = old;
            document.Notify(page, NotebookChangeKind.Pages);
        }
    }
}

/// <summary>Copies pages with their content; in each section the copies are inserted as a block after the last of the copied pages.</summary>
/// <param name="pages">The pages.</param>
public sealed class DuplicatePagesCommand(IReadOnlyList<NotebookPage> pages) : IDocumentCommand<Notebook>
{
    private StructureEdit _edit;

    /// <summary>The copies in order, available after the command was applied.</summary>
    public IReadOnlyList<NotebookPage> Copies { get; private set; }

    /// <inheritdoc />
    public string Description => "Duplicate page";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = StructureEdit.Run(document, _edit, NotebookChangeKind.Pages, () =>
    {
        var ordered = NotebookEdit.Ordered(document, pages);
        foreach (var (_, page) in ordered) NotebookEdit.Editable(document, page);
        var copies = new List<NotebookPage>();
        foreach (var group in ordered.GroupBy(e => e.Section))
        {
            var block = group.Select(e => Copy(e.Page)).ToList();
            group.Key.PageList.InsertRange(group.Key.PageList.IndexOf(group.Last().Page) + 1, block);
            copies.AddRange(block);
        }

        Copies = copies;
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);

    private static NotebookPage Copy(NotebookPage source)
    {
        var copy = new NotebookPage { Title = source.Title, Level = source.Level, Extra = source.Extra };
        copy.ElementList.AddRange(source.ElementList.Select(e => e.Clone()));
        return copy;
    }
}
