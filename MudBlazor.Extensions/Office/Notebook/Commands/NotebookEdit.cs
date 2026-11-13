namespace MudBlazor.Extensions.Office.Notebook;

internal static class NotebookEdit
{
    public static NotebookPage Editable(Notebook notebook, NotebookPage page)
    {
        SectionOf(notebook, page);
        if (page.LoadError != null) throw new InvalidOperationException("The page could not be read and is read-only.");
        return page;
    }

    public static NotebookSection SectionOf(Notebook notebook, NotebookPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return notebook.SectionList.FirstOrDefault(s => s.PageList.Contains(page)) ?? throw new ArgumentException("The page must belong to the notebook.", nameof(page));
    }

    public static int IndexOf(Notebook notebook, NotebookSection section)
    {
        ArgumentNullException.ThrowIfNull(section);
        var index = notebook.SectionList.IndexOf(section);
        return index >= 0 ? index : throw new ArgumentException("The section must belong to the notebook.", nameof(section));
    }

    public static List<(NotebookSection Section, NotebookPage Page)> Ordered(Notebook notebook, IReadOnlyList<NotebookPage> pages)
    {
        if (pages == null || pages.Count == 0) throw new ArgumentException("No pages given.", nameof(pages));
        return pages.Distinct().Select(p => (Section: SectionOf(notebook, p), Page: p))
            .OrderBy(e => notebook.SectionList.IndexOf(e.Section)).ThenBy(e => e.Section.PageList.IndexOf(e.Page)).ToList();
    }

    public static void AddPageIfEmpty(NotebookSection section, Dictionary<NotebookSection, NotebookPage> fresh)
    {
        if (section.PageList.Count > 0) return;
        if (!fresh.TryGetValue(section, out var page)) fresh[section] = page = new NotebookPage();
        section.PageList.Add(page);
    }

    public static T Find<T>(IEnumerable<NoteElement> elements, string id) where T : NoteElement =>
        elements.FirstOrDefault(e => e.Id == id) as T ?? throw new ArgumentException($"The page has no {typeof(T).Name} with the id {id}.", nameof(id));

    public static T Replace<T>(List<NoteElement> elements, T element) where T : NoteElement
    {
        var copy = (T)element.Clone();
        elements[elements.IndexOf(element)] = copy;
        return copy;
    }

    public static string Color(string value, string name) =>
        NoteValues.IsColor(value) ? value : throw new ArgumentException($"The colour '{value}' is not of the form #RRGGBB.", name);

    public static double Number(double value, string name, double min = double.MinValue, double max = double.MaxValue) =>
        double.IsFinite(value) && value >= min && value <= max ? value : throw new ArgumentOutOfRangeException(name, value, "The value must be a finite number in range.");

    public static double Positive(double value, string name) => Number(value, name, double.Epsilon);

    public static NoteBounds Check(NoteBounds bounds, string name)
    {
        Number(bounds.X, name);
        Number(bounds.Y, name);
        Number(bounds.Width, name, 0);
        Number(bounds.Height, name, 0);
        return bounds;
    }

    public static void Validate(NoteElement element, string name)
    {
        Check(element.Bounds, name);
        switch (element)
        {
            case TextElement text:
                Text(text.Paragraphs, name);
                break;
            case InkElement ink:
                Color(ink.Color, name);
                Number(ink.StrokeWidth, name, 0);
                foreach (var point in ink.Points)
                {
                    Number(point.X, name);
                    Number(point.Y, name);
                    Number(point.Pressure, name, 0, 1);
                }

                break;
            case ShapeElement shape:
                Color(shape.Stroke, name);
                if (shape.Fill != null) Color(shape.Fill, name);
                Number(shape.StrokeWidth, name, 0);
                if (!Enum.IsDefined(shape.Kind)) throw new ArgumentException("The shape kind is not defined.", name);
                break;
            case TableElement table:
                foreach (var width in table.ColumnWidths) Number(width, name, 0);
                if (table.Rows.Any(r => r?.Count != table.ColumnWidths.Count || r.Any(c => c == null)))
                    throw new ArgumentException("Every table row needs one cell per column.", name);
                foreach (var cell in table.Rows.SelectMany(r => r)) Text(cell.Paragraphs, name);
                break;
        }
    }

    public static void FitInk(InkElement ink, IReadOnlyList<InkPoint> pagePoints)
    {
        var pad = ink.StrokeWidth / 2;
        var (left, top) = (pagePoints.Min(p => p.X) - pad, pagePoints.Min(p => p.Y) - pad);
        (ink.X, ink.Y, ink.Width, ink.Height) = (left, top, pagePoints.Max(p => p.X) - left + pad, pagePoints.Max(p => p.Y) - top + pad);
        ink.Points = pagePoints.Select(p => p with { X = p.X - left, Y = p.Y - top }).ToList();
    }

    public static IReadOnlyList<NoteParagraph> Text(IReadOnlyList<NoteParagraph> paragraphs, string name)
    {
        ArgumentNullException.ThrowIfNull(paragraphs, name);
        foreach (var paragraph in paragraphs)
        {
            if (paragraph?.Runs == null || paragraph.Runs.Any(r => r?.Text == null || r.Format == null)) throw new ArgumentException("A paragraph, run or format is null.", name);
            if (paragraph.Indent is < 0 or > NoteParagraph.MaxIndent) throw new ArgumentException($"The indent must be from 0 to {NoteParagraph.MaxIndent}.", name);
            if (!Enum.IsDefined(paragraph.List) || !Enum.IsDefined(paragraph.Align) || !Enum.IsDefined(paragraph.Heading))
                throw new ArgumentException("A paragraph has an undefined list kind, alignment or heading.", name);
            foreach (var format in paragraph.Runs.Select(r => r.Format))
            {
                if (format.SizePt is { } size) Number(size, name, double.Epsilon, 1000);
                if (!Optional(format.Color, NoteValues.IsColor) || !Optional(format.Highlight, NoteValues.IsColor) || !Optional(format.Font, NoteValues.IsFont)
                    || !Optional(format.Link, NoteValues.IsSafeLink))
                    throw new ArgumentException("A run has an invalid colour, font or link.", name);
            }
        }

        return paragraphs.ToList();
    }

    public static bool IsEmpty(IReadOnlyList<NoteParagraph> paragraphs) => paragraphs.All(p => p.Runs.All(r => r.Text.Length == 0));

    private static bool Optional(string value, Func<string, bool> valid) => value == null || valid(value);
}

internal sealed class ElementEdit
{
    private readonly NotebookPage _page;
    private readonly NoteElement[] _before;
    private readonly NoteElement[] _after;

    private ElementEdit(NotebookPage page, NoteElement[] before, NoteElement[] after) => (_page, _before, _after) = (page, before, after);

    public static ElementEdit Run(Notebook document, NotebookPage page, ElementEdit done, Action<List<NoteElement>> build)
    {
        if (done != null)
        {
            done.Show(document, done._after, done._before);
            return done;
        }

        NotebookEdit.Editable(document, page);
        var elements = page.ElementList.ToList();
        build(elements);
        if (elements.SequenceEqual(page.ElementList)) return null;
        foreach (var element in elements.Except(page.ElementList)) NotebookEdit.Validate(element, nameof(element));
        var edit = new ElementEdit(page, page.ElementList.ToArray(), elements.ToArray());
        edit.Show(document, edit._after, edit._before);
        return edit;
    }

    public void Revert(Notebook document) => Show(document, _before, _after);

    private void Show(Notebook document, NoteElement[] target, NoteElement[] current)
    {
        _page.ElementList.Clear();
        _page.ElementList.AddRange(target);
        document.Notify(_page, NotebookChangeKind.Content);
        foreach (var element in target.Except(current)) element.Version = _page.Version;
    }
}

internal sealed class StructureEdit
{
    private readonly NotebookChangeKind _kind;
    private readonly State _before;
    private readonly State _after;

    private StructureEdit(NotebookChangeKind kind, State before, State after) => (_kind, _before, _after) = (kind, before, after);

    public static StructureEdit Run(Notebook document, StructureEdit done, NotebookChangeKind kind, Action build)
    {
        if (done != null)
        {
            done.Show(document, done._after);
            return done;
        }

        var before = State.Of(document);
        build();
        var after = State.Of(document);
        if (before.SameAs(after)) return null;
        document.Notify(null, kind);
        return new StructureEdit(kind, before, after);
    }

    public void Revert(Notebook document) => Show(document, _before);

    private void Show(Notebook document, State state)
    {
        state.Restore(document);
        document.Notify(null, _kind);
    }

    private sealed record State(NotebookSection[] Sections, NotebookPage[][] Pages)
    {
        public static State Of(Notebook notebook) => new(notebook.SectionList.ToArray(), notebook.SectionList.Select(s => s.PageList.ToArray()).ToArray());

        public bool SameAs(State other) => Sections.SequenceEqual(other.Sections) && Pages.Zip(other.Pages).All(p => p.First.SequenceEqual(p.Second));

        public void Restore(Notebook notebook)
        {
            notebook.SectionList.Clear();
            notebook.SectionList.AddRange(Sections);
            for (var i = 0; i < Sections.Length; i++)
            {
                Sections[i].PageList.Clear();
                Sections[i].PageList.AddRange(Pages[i]);
            }
        }
    }
}
