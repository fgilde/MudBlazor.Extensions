using System.Globalization;
using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Options;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public class DocumentEditorStylesDialogsTests : DocumentEditorFormattingTestBase
{
    private static IRenderedComponent<MudExRibbonGallery<MudExDocumentEditor.StyleItem>> Gallery(IRenderedComponent<MudExDocumentEditor> cut) =>
        cut.FindComponent<MudExRibbonGallery<MudExDocumentEditor.StyleItem>>();

    private static Task Pick(IRenderedComponent<MudExDocumentEditor> cut, string id) =>
        cut.InvokeAsync(() => Gallery(cut).Instance.SelectedItemChanged.InvokeAsync(Gallery(cut).Instance.Items!.Single(i => i.Id == id)));

    private void Launch(IRenderedComponent<MudExDocumentEditor> cut, string group, string dialog)
    {
        cut.FindAll(".mud-ex-ribbon-dialog-launcher").Single(b => b.GetAttribute("aria-label")!.EndsWith(": " + group)).Click();
        _dialogs.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(dialog)));
    }

    private Task Choose<T>(string cls, T value)
    {
        var select = _dialogs.FindComponents<MudSelect<T>>().Single(s => s.Instance.Class?.Contains(cls) == true);
        return _dialogs.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(value));
    }

    private Task Check(string cls, bool? value)
    {
        var box = _dialogs.FindComponents<MudCheckBox<bool?>>().Single(s => s.Instance.Class?.Contains(cls) == true);
        return _dialogs.InvokeAsync(() => box.Instance.ValueChanged.InvokeAsync(value));
    }

    [Fact]
    public async Task Gallery_ShowsQuickStylesAndMissingBuiltIns()
    {
        var document = DocumentSurfaceRenderTests.Created("one");
        var cut = Render(document);
        await Select(cut, 0, 1, 1);

        var items = Gallery(cut).Instance.Items!.ToList();

        Assert.Equal(document.Styles.Gallery.Select(s => s.Id), items.Select(i => i.Id));
        Assert.Contains(items, i => i is { Id: "Heading1", Name: "Heading 1", Type: StyleType.Paragraph });
        Assert.Contains(items, i => i is { Id: "Strong", Name: "Strong", Type: StyleType.Character });
        Assert.Null(document.Styles.Find("Heading1"));
        Assert.Contains("font-size:16pt", items.Single(i => i.Id == "Heading1").Css);
        Assert.Contains("font-weight:bold", items.Single(i => i.Id == "Strong").Css);
        Assert.Equal("Normal", Gallery(cut).Instance.SelectedItem?.Id);
    }

    [Fact]
    public async Task Gallery_PickHeading1_AppliesStyle_AddsDefinition()
    {
        var document = DocumentSurfaceRenderTests.Created("one", "two");
        var cut = Render(document);
        await Select(cut, 0, 1, 1);

        await Pick(cut, "Heading1");

        Assert.Equal("Heading1", P(document).Format.StyleId);
        Assert.Null(P(document, 1).Format.StyleId);
        Assert.NotNull(document.Styles.Find("Heading1"));
        cut.WaitForAssertion(() => Assert.Equal("Heading1", Gallery(cut).Instance.SelectedItem?.Id));
        await Select(cut, 1, 1, 1);
        cut.WaitForAssertion(() => Assert.Equal("Normal", Gallery(cut).Instance.SelectedItem?.Id));
    }

    [Fact]
    public async Task Gallery_PickStrong_AppliesCharacterStyleToSelection()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello world");
        var cut = Render(document);
        await Select(cut, 0, 0, 5);

        await Pick(cut, "Strong");

        Assert.Equal("Strong", P(document).Inlines[0].Format.StyleId);
        Assert.True(Run(document, 0, 2).Bold);
        Assert.NotEqual(true, Run(document, 0, 8).Bold);
        cut.WaitForAssertion(() => Assert.Equal("Strong", Gallery(cut).Instance.SelectedItem?.Id));
    }

    [Fact]
    public async Task UpdateStyleFromSelection_ChangesStyle_OneStep()
    {
        var document = DocumentSurfaceRenderTests.Created("one", "two");
        document.Execute(new ApplyParagraphStyleCommand([document.Blocks[0].Id, document.Blocks[1].Id], "Heading1"));
        document.Execute(new SetRunFormatCommand(new DocumentRange(document.PositionOf(document.Blocks[0].Id, 0), document.PositionOf(document.Blocks[0].Id, 3)), f => f with { Italic = true }, "Italic"));
        var cut = Render(document);
        await Select(cut, 0, 1, 1);
        var step = document.Undo.UndoDescription;

        cut.WaitForAssertion(() => Assert.Contains("Heading 1", Command(cut, "wd-style-update").Text));
        await Run(cut, "wd-style-update");

        Assert.True(document.Styles.Find("Heading1")!.RunFormat.Italic);
        Assert.True(Run(document, 1, 1).Italic);
        await Run(cut, "wd-undo");
        Assert.NotEqual(true, Run(document, 1, 1).Italic);
        Assert.True(Run(document, 0, 1).Italic);
        Assert.Equal(step, document.Undo.UndoDescription);
    }

    [Fact]
    public void Units_German_Cm_UnitedStates_Inch_Invariant_Cm()
    {
        Assert.True(DocumentUnits.Metric(new CultureInfo("de-DE")));
        Assert.False(DocumentUnits.Metric(new CultureInfo("en-US")));
        Assert.True(DocumentUnits.Metric(CultureInfo.InvariantCulture));
        Assert.Equal(("cm", "in"), (DocumentUnits.Unit(true), DocumentUnits.Unit(false)));
    }

    [Fact]
    public void Units_RoundTrip_1440TwipsIs254CmAnd1In()
    {
        Assert.Equal(2.54, DocumentUnits.FromTwips(1440, true));
        Assert.Equal(1, DocumentUnits.FromTwips(1440, false));
        Assert.Equal(1440, DocumentUnits.ToTwips(2.54, true));
        Assert.Equal(1440, DocumentUnits.ToTwips(1, false));
        Assert.Equal(567, DocumentUnits.ToTwips(1, true));
        Assert.Equal(0.5, DocumentUnits.FromTwips(720, false));
    }

    [Fact]
    public async Task FontDialog_ChangesOnlyTouchedProperties()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello world");
        Bold(document, 0, 0, 3);
        var cut = Render(document);
        var id = document.Blocks[0].Id;

        await cut.InvokeAsync(() => Surface(cut).OnCommandAsync(new SurfaceSync(Seen, [], new SurfaceSelection(new SurfacePoint(id, 0), new SurfacePoint(id, 11)), false), null, "shortcut", "Ctrl+D"));
        _dialogs.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-docfont-size input")));
        bool? Box(string cls) => _dialogs.FindComponents<MudCheckBox<bool?>>().Single(c => c.Instance.Class!.Contains(cls)).Instance.Value;
        Assert.Null(Box("mud-ex-docfont-bold"));
        Assert.Equal((false, false, false), (Box("mud-ex-docfont-italic"), Box("mud-ex-docfont-superscript"), Box("mud-ex-docfont-subscript")));
        Assert.Equal("none", _dialogs.FindComponents<MudSelect<string>>().Single(c => c.Instance.Class!.Contains("mud-ex-docfont-underline")).Instance.Value);
        _dialogs.Find(".mud-ex-docfont-size input").Change("20");
        await Check("mud-ex-docfont-small-caps", true);
        _dialogs.Find(".mud-ex-docfont-ok").Click();

        cut.WaitForAssertion(() => Assert.All(Enumerable.Range(0, 11), i => Assert.Equal(20, Run(document, 0, i).SizePt)));
        Assert.All(Enumerable.Range(0, 11), i => Assert.True(Run(document, 0, i).SmallCaps));
        Assert.All(Enumerable.Range(0, 3), i => Assert.True(Run(document, 0, i).Bold));
        Assert.All(Enumerable.Range(3, 8), i => Assert.NotEqual(true, Run(document, 0, i).Bold));
        Assert.All(Enumerable.Range(0, 11), i => Assert.Null(Run(document, 0, i).Underline));
        await Run(cut, "wd-undo");
        Assert.Equal(11, Run(document, 0, 5).SizePt);
        Assert.NotEqual(true, Run(document, 0, 5).SmallCaps);
    }

    [Fact]
    public async Task FontDialog_SelectionRemovedWhileOpen_ApplyIsASilentNoOp()
    {
        var document = DocumentSurfaceRenderTests.Created("one", "two");
        var second = document.Blocks[1].Id;
        var cut = Render(document);
        await Select(cut, 1, 0, 3);

        Launch(cut, "Font", ".mud-ex-docfont-dialog");
        _dialogs.Find(".mud-ex-docfont-size input").Change("20");
        await cut.InvokeAsync(() => document.Execute(new MergeParagraphsCommand(document.Blocks[0].Id)));
        Assert.Null(document.FindBlock(second));
        var version = document.Version;
        _dialogs.Find(".mud-ex-docfont-ok").Click();
        await cut.InvokeAsync(() => Task.Delay(50));

        Assert.Equal(version, document.Version);
        Assert.Empty(_dialogs.FindAll(".mud-dialog"));
    }

    [Fact]
    public async Task ParagraphDialog_Hanging1cm_SetsIndentFirstLineMinus567()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var document = DocumentSurfaceRenderTests.Created("one", "two");
            var cut = Render(document);
            await Select(cut, 0, 1, 1);

            Launch(cut, "Paragraph", ".mud-ex-docpara-dialog");
            Assert.Contains("cm", _dialogs.Find(".mud-ex-docpara-by").TextContent);
            Assert.Equal(0, _dialogs.FindComponents<MudNumericField<double?>>().Single(c => c.Instance.Class!.Contains("mud-ex-docpara-indent-left")).Instance.Value);
            await Choose("mud-ex-docpara-special", "hanging");
            _dialogs.Find(".mud-ex-docpara-by input").Change("1");
            await Choose("mud-ex-docpara-line", "onePointFive");
            _dialogs.Find(".mud-ex-docpara-ok").Click();

            cut.WaitForAssertion(() => Assert.Equal(-567, P(document).Format.IndentFirstLine));
            Assert.Equal(new LineSpacing(360, LineRule.Auto), P(document).Format.Line);
            Assert.Null(P(document).Format.IndentLeft);
            Assert.Null(P(document).Format.Alignment);
            Assert.Null(P(document, 1).Format.IndentFirstLine);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Theory]
    [InlineData("Font", ".mud-ex-docfont-dialog", null, "mud-dialog-width-sm")]
    [InlineData("Font", ".mud-ex-docfont-dialog", MaxWidth.Large, "mud-dialog-width-lg")]
    [InlineData("Paragraph", ".mud-ex-docpara-dialog", null, "mud-dialog-width-sm")]
    [InlineData("Paragraph", ".mud-ex-docpara-dialog", MaxWidth.Medium, "mud-dialog-width-md")]
    public async Task Dialogs_UseTheDialogOptionsParameter(string group, string dialog, MaxWidth? maxWidth, string expected)
    {
        var options = maxWidth is { } w ? new DialogOptionsEx { MaxWidth = w, FullWidth = true } : null;
        var document = DocumentSurfaceRenderTests.Created("one");
        var cut = Render(p => p.Add(x => x.TextDocument, document).Add(x => x.DialogOptions, options));
        await Select(cut, 0, 1, 1);

        Launch(cut, group, dialog);

        Assert.Contains(expected, _dialogs.Find(dialog).ClassList);
    }
}
