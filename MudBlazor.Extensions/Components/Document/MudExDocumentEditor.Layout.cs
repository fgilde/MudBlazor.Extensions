using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Document;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components;

public partial class MudExDocumentEditor
{
    private static readonly (string Name, int Top, int Side)[] MarginPresets = [("Normal", 1440, 1440), ("Narrow", 720, 720), ("Wide", 1440, 2880)];

    private MudExDocumentView? _viewChosen;
    private (int Current, int Total) _pages;

    /// <summary>
    /// How the document is laid out: as pages or as one continuous sheet. Unset: pages for Word, RTF and OpenDocument files and new documents, the
    /// continuous sheet for web pages, Markdown and plain text. A choice made in the View tab is kept for the lifetime of the editor instance.
    /// </summary>
    [Parameter] public MudExDocumentView? DocumentView { get; set; }

    /// <summary>The layout currently shown.</summary>
    public MudExDocumentView CurrentView => _viewChosen ?? DocumentView ?? (_loaded is DocumentFileFormat.Html or DocumentFileFormat.Markdown or DocumentFileFormat.Text
        ? MudExDocumentView.Web : MudExDocumentView.Pages);

    private bool Metric => DocumentUnits.Metric(CultureInfo.CurrentCulture);

    private PageSetup CurrentPage => _document == null || _document.Sections.Count == 0 ? null : SectionAt(Selection).Page;

    private Section SectionAt(DocumentRange range)
    {
        try
        {
            return range.Start?.BlockId is { } id ? _document.SectionOf(id) : _document.Sections[0];
        }
        catch (ArgumentException)
        {
            return _document.Sections[0];
        }
    }

    private Task OnPagesChanged((int Current, int Total) pages)
    {
        _pages = pages;
        return Task.CompletedTask;
    }

    private void SetView(MudExDocumentView view)
    {
        _viewChosen = view;
        _pages = default;
    }

    private Task PrintAsync() => _surface?.PrintAsync() ?? Task.CompletedTask;

    internal Task SetPageAsync(Func<PageSetup, PageSetup> change) => RunAsync(range =>
    {
        if (!Live(range) || _document.Sections.Count == 0)
            return;
        var section = SectionAt(range);
        var page = change(section.Page);
        if (page != section.Page && page.Width > 0 && page.Height > 0 && page.TextWidth > 0 && page.Height - page.MarginTop - page.MarginBottom > 0)
            _document.Execute(new SetPageSetupCommand(section.Id, page), range);
    });

    internal Task SetMarginsAsync(int top, int side) => SetPageAsync(p => p with { MarginTop = top, MarginBottom = top, MarginLeft = side, MarginRight = side });

    internal Task SetOrientationAsync(PageOrientation orientation) => SetPageAsync(p => p.Orientation == orientation ? p : p with
    {
        Orientation = orientation, Width = p.Height, Height = p.Width, MarginTop = p.MarginLeft, MarginBottom = p.MarginRight, MarginLeft = p.MarginBottom, MarginRight = p.MarginTop
    });

    internal Task SetPaperSizeAsync(string name) => MudExDocumentPageSetupDialog.Sizes.TryGetValue(name ?? "", out var size)
        ? SetPageAsync(p => p with
        {
            Width = p.Orientation == PageOrientation.Landscape ? size.Height : size.Width, Height = p.Orientation == PageOrientation.Landscape ? size.Width : size.Height
        })
        : Task.CompletedTask;

    private bool IsPaperSize(string name) =>
        CurrentPage is { } p && MudExDocumentPageSetupDialog.Sizes[name] is var s && Math.Abs(Math.Min(p.Width, p.Height) - s.Width) <= 20 && Math.Abs(Math.Max(p.Width, p.Height) - s.Height) <= 20;

    private bool IsMargin(string name) => CurrentPage is { } p && MarginPresets.Single(m => m.Name == name) is var m && p.MarginTop == m.Top && p.MarginBottom == m.Top && p.MarginLeft == m.Side && p.MarginRight == m.Side;

    internal async Task OpenPageSetupAsync()
    {
        (DocumentRange Range, PageSetup Page) info = default;
        await RunAsync(range => info = Live(range) && _document.Sections.Count > 0 ? (range, SectionAt(range).Page) : default);
        if (info.Page == null)
            return;
        var reference = await DialogService.ShowExAsync<MudExDocumentPageSetupDialog>(TryLocalize("Page Setup"), new DialogParameters
        {
            { nameof(MudExDocumentPageSetupDialog.Page), info.Page },
            { nameof(MudExDocumentPageSetupDialog.Metric), Metric },
            { nameof(MudExDocumentPageSetupDialog.Localizer), Localizer }
        }, DialogOptionsEx.EditorDialogOptions(DialogOptions));
        var result = await reference.Result;
        if (await ReselectAsync(info.Range) && result is { Canceled: false, Data: PageSetup page })
            await SetPageAsync(_ => page);
        await FocusSurfaceAsync();
    }

    private double IndentUnit(int? twips) => DocumentUnits.FromTwips(twips ?? 0, Metric);

    internal Task SetIndentAsync(bool left, double value) => double.IsFinite(value) && value >= 0
        ? SetParagraphAsync(p => left ? p with { IndentLeft = DocumentUnits.ToTwips(value, Metric) } : p with { IndentRight = DocumentUnits.ToTwips(value, Metric) }, "Indent")
        : Task.CompletedTask;

    internal Task SetSpacingAsync(bool before, double points) => double.IsFinite(points) && points >= 0
        ? SetParagraphAsync(p => before ? p with { SpacingBefore = (int)Math.Round(points * 20) } : p with { SpacingAfter = (int)Math.Round(points * 20) }, "Spacing")
        : Task.CompletedTask;
}
