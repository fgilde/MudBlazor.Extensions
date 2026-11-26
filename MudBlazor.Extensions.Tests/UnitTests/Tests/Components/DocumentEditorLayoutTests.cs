using Bunit;
using MudBlazor.Extensions.Components;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public class DocumentEditorLayoutTests : DocumentEditorFormattingTestBase
{
    [Fact]
    public async Task LayoutTab_ChangesMarginsOrientationAndSize_InOneUndoStepEach()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document);
        await Select(cut, 0, 0, 0);

        await Run(cut, "wd-margins-narrow");
        Assert.Equal((720, 720), (document.Sections[0].Page.MarginTop, document.Sections[0].Page.MarginLeft));
        await Run(cut, "wd-orientation-landscape");
        Assert.Equal(PageOrientation.Landscape, document.Sections[0].Page.Orientation);
        Assert.True(document.Sections[0].Page.Width > document.Sections[0].Page.Height);
        await Run(cut, "wd-size-letter");
        Assert.Equal((15840, 12240), (document.Sections[0].Page.Width, document.Sections[0].Page.Height));

        document.Undo.Undo();
        Assert.Equal(PageSetup.A4.Height, document.Sections[0].Page.Width);
        document.Undo.Undo();
        document.Undo.Undo();
        Assert.Equal(PageSetup.A4, document.Sections[0].Page);
    }

    [Fact]
    public async Task PageSetupDialog_OpensAndAppliesItsResult()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document);
        await Select(cut, 0, 0, 0);

        _ = cut.InvokeAsync(() => cut.Instance.OpenPageSetupAsync());
        _dialogs.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-docpagesetup-dialog")));
        _dialogs.Find(".mud-ex-docpagesetup-ok").Click();
        _dialogs.WaitForAssertion(() => Assert.Empty(_dialogs.FindAll(".mud-ex-docpagesetup-dialog")));
        Assert.Equal(PageSetup.A4.Width, document.Sections[0].Page.Width);
    }

    [Fact]
    public void ViewDefaultsToPages_CanBeSetAndSwitched_AndStatusBarShowsThePage()
    {
        var cut = Render(DocumentSurfaceRenderTests.Created("Hello"));
        Assert.Equal(MudExDocumentView.Pages, cut.Instance.CurrentView);
        Assert.DoesNotContain("Page 2 of 3", cut.Markup);

        var web = Render(p => p.Add(x => x.TextDocument, DocumentSurfaceRenderTests.Created("Hello")).Add(x => x.DocumentView, MudExDocumentView.Web));
        Assert.Equal(MudExDocumentView.Web, web.Instance.CurrentView);
        web.InvokeAsync(() => Command(web, "wd-view-pages").ExecuteAsync()).Wait();
        Assert.Equal(MudExDocumentView.Pages, web.Instance.CurrentView);

        var bar = _context.Render<MudExDocumentStatusBar>(p => p.Add(x => x.Pages, (2, 3)));
        Assert.Contains("Page 2 of 3", bar.Markup);
    }

    [Fact]
    public void Surface_SendsPageGeometryAndPageBreakBefore()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        document.Execute(new SetParagraphFormatCommand([document.Blocks[0].Id], f => f with { PageBreakBefore = true }, "Page break before"));
        var render = new DocumentSurfaceRender(document);

        Assert.Contains("--mx-ph:1122.53px", render.SheetCss());
        Assert.Contains("break-before:page", render.Render(document.Blocks[0]).Css);
    }
}
