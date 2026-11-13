using System.Globalization;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>Date, slide number and footer of slides, as in the header and footer dialog.</summary>
/// <param name="ShowDate">True to show the date.</param>
/// <param name="AutomaticDate">True for a date field that shows the current date, false for <paramref name="FixedDate"/>.</param>
/// <param name="FixedDate">The fixed date text.</param>
/// <param name="ShowSlideNumber">True to show the slide number.</param>
/// <param name="ShowFooter">True to show the footer.</param>
/// <param name="FooterText">The footer text.</param>
/// <param name="HideOnTitleSlides">True to leave out slides with a title slide layout.</param>
public sealed record HeaderFooterOptions(bool ShowDate, bool AutomaticDate, string FixedDate, bool ShowSlideNumber, bool ShowFooter, string FooterText, bool HideOnTitleSlides)
{
    /// <summary>Reads the current options of a slide.</summary>
    /// <param name="slide">The slide.</param>
    /// <returns>The options.</returns>
    public static HeaderFooterOptions From(Slide slide)
    {
        ArgumentNullException.ThrowIfNull(slide);
        var date = HeaderFooter.Find(slide, "dt");
        var automatic = date == null || HeaderFooter.Runs(date).Any(r => HeaderFooter.Kind(r.FieldType) == "datetime");
        var footer = HeaderFooter.Find(slide, "ftr");
        return new HeaderFooterOptions(date != null, automatic, automatic ? "" : HeaderFooter.Text(date), HeaderFooter.Find(slide, "sldNum") != null,
            footer != null, footer == null ? "" : HeaderFooter.Text(footer), slide.Owner?.ShowSpecialPlaceholdersOnTitleSlide == false);
    }
}

/// <summary>
/// Adds or removes the date, slide number and footer placeholders of slides. They take their position from the layout; a part whose placeholder the layout
/// lacks is skipped.
/// </summary>
/// <param name="slides">The slides.</param>
/// <param name="options">The options.</param>
public sealed class SetHeaderFooterCommand(IReadOnlyList<Slide> slides, HeaderFooterOptions options) : IDocumentCommand<Presentation>
{
    private readonly SlideChanges _changes = new();
    private bool _titlePlaceholders;

    /// <inheritdoc />
    public string Description => "Header and footer";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        ArgumentNullException.ThrowIfNull(options);
        var ordered = SlideEdit.Ordered(document, slides);
        _titlePlaceholders = document.ShowSpecialPlaceholdersOnTitleSlide;
        _changes.Apply(document, ordered, slide => Change(document, slide));
        if (_titlePlaceholders == !options.HideOnTitleSlides) return;
        document.ShowSpecialPlaceholdersOnTitleSlide = !options.HideOnTitleSlides;
        document.Notify(null, PresentationChangeKind.Content);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        _changes.Revert(document);
        if (document.ShowSpecialPlaceholdersOnTitleSlide == _titlePlaceholders) return;
        document.ShowSpecialPlaceholdersOnTitleSlide = _titlePlaceholders;
        document.Notify(null, PresentationChangeKind.Content);
    }

    private void Change(Presentation document, Slide slide)
    {
        var show = !(options.HideOnTitleSlides && slide.Layout?.Type == "title");
        Set(slide, "dt", show && options.ShowDate,
            options.AutomaticDate ? DateTime.Today.ToString("M/d/yyyy", CultureInfo.InvariantCulture) : options.FixedDate ?? "", options.AutomaticDate ? "datetime1" : null);
        Set(slide, "ftr", show && options.ShowFooter, options.FooterText ?? "", null);
        Set(slide, "sldNum", show && options.ShowSlideNumber, (document.FirstSlideNumber + document.SlideList.IndexOf(slide)).ToString(CultureInfo.InvariantCulture), "slidenum");
    }

    private static void Set(Slide slide, string type, bool show, string text, string field)
    {
        if (slide.Layout?.ShapeList.OfType<AutoShape>().FirstOrDefault(s => s.Placeholder?.Type == type) is not { } placeholder) return;
        var shape = HeaderFooter.Find(slide, type);
        if (!show)
        {
            slide.ShapeList.Remove(shape);
            return;
        }

        var runs = shape == null ? [] : HeaderFooter.Runs(shape).ToList();
        if (field == null ? shape != null && runs.All(r => r.FieldType == null) && HeaderFooter.Text(shape) == text : runs.Any(r => HeaderFooter.Kind(r.FieldType) == HeaderFooter.Kind(field)))
            return;
        var run = Run(runs.FirstOrDefault(), text, field);
        if (shape == null)
        {
            slide.ShapeList.Add(SlideEdit.EmptyPlaceholder(placeholder, ShapeEdit.NextId(slide), new TextBody { Paragraphs = [new TextParagraph { Runs = [run] }] }));
            return;
        }

        var body = shape.Text ?? new TextBody();
        shape.Text = body with { Paragraphs = [(body.Paragraphs.FirstOrDefault() ?? new TextParagraph()) with { Runs = [run] }] };
    }

    private static TextRun Run(TextRun previous, string text, string field)
    {
        if (field == null) return previous is { FieldType: null } ? previous with { Text = text } : new TextRun(text, previous?.Format);
        var properties = previous?.Source?.Element(PptxReader.A + "rPr");
        return new TextRun(text, previous?.Format)
        {
            FieldType = field,
            Source = new XElement(PptxReader.A + "fld", new XAttribute("id", Guid.NewGuid().ToString("B").ToUpperInvariant()), new XAttribute("type", field),
                properties == null ? null : new XElement(properties), new XElement(PptxReader.A + "t", OoxmlText.Escape(text)))
        };
    }
}

internal static class HeaderFooter
{
    internal static AutoShape Find(Slide slide, string type) => slide.ShapeList.OfType<AutoShape>().FirstOrDefault(s => s.Placeholder?.Type == type);

    internal static IEnumerable<TextRun> Runs(AutoShape shape) => shape.Text?.Paragraphs.SelectMany(p => p.Runs) ?? [];

    internal static string Text(AutoShape shape) => string.Join("\n", shape.Text?.Paragraphs.Select(p => string.Concat(p.Runs.Select(r => r.Text))) ?? []);

    internal static string Kind(string fieldType) => fieldType?.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
}
