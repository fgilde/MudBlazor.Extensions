using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public class DocumentEditorShortcutTests : DocumentEditorFormattingTestBase
{
    public static TheoryData<string> Keys => [.. DocumentShortcuts.Map.Keys];

    [Theory]
    [MemberData(nameof(Keys))]
    public async Task EveryShortcut_ChangesItsProperty(string key)
    {
        var document = DocumentSurfaceRenderTests.Created("Hello world", "Second");
        var id = document.Blocks[0].Id;
        if (key == "Ctrl+Space")
            Bold(document, 0, 0, 5);
        if (key == "Ctrl+Shift+M")
            document.Execute(new SetParagraphFormatCommand([id], p => p with { IndentLeft = 1440 }, "Indent"));
        if (key == "Ctrl+Shift+N")
            document.Execute(new ApplyParagraphStyleCommand([id], "Heading1"));
        var saved = 0;
        var cut = Render(p => p.Add(x => x.TextDocument, document).Add(x => x.SaveRequested, (MudExFileSaveRequest r) =>
        {
            r.Handled = true;
            saved++;
        }));
        var version = document.Version;
        RunFormat First() => Run(document, 0, 2);
        ParagraphFormat Paragraph() => P(document).Format;
        Action check = key switch
        {
            "Ctrl+B" => () => Assert.True(First().Bold),
            "Ctrl+I" => () => Assert.True(First().Italic),
            "Ctrl+U" => () => Assert.Equal("single", First().Underline),
            "Ctrl+Shift+D" => () => Assert.Equal("double", First().Underline),
            "Ctrl+=" => () => Assert.Equal(VerticalAlign.Subscript, First().VerticalAlign),
            "Ctrl+Shift+=" => () => Assert.Equal(VerticalAlign.Superscript, First().VerticalAlign),
            "Ctrl+Space" => () => Assert.NotEqual(true, First().Bold),
            "Ctrl+]" or "Ctrl+Shift+." => () => Assert.Equal(12, First().SizePt),
            "Ctrl+[" or "Ctrl+Shift+," => () => Assert.Equal(10, First().SizePt),
            "Ctrl+L" => () => Assert.Equal(ParagraphAlignment.Left, Paragraph().Alignment),
            "Ctrl+E" => () => Assert.Equal(ParagraphAlignment.Center, Paragraph().Alignment),
            "Ctrl+R" => () => Assert.Equal(ParagraphAlignment.Right, Paragraph().Alignment),
            "Ctrl+J" => () => Assert.Equal(ParagraphAlignment.Justify, Paragraph().Alignment),
            "Ctrl+M" or "Ctrl+Shift+M" => () => Assert.Equal(720, Paragraph().IndentLeft),
            "Ctrl+Shift+L" => () => Assert.Equal(Bullet, document.ListLabel(P(document))),
            "Ctrl+1" => () => Assert.Equal(new LineSpacing(240, LineRule.Auto), Paragraph().Line),
            "Ctrl+5" => () => Assert.Equal(new LineSpacing(360, LineRule.Auto), Paragraph().Line),
            "Ctrl+2" => () => Assert.Equal(new LineSpacing(480, LineRule.Auto), Paragraph().Line),
            "Ctrl+0" => () => Assert.Equal(240, Paragraph().SpacingBefore),
            "Ctrl+Alt+1" or "Ctrl+Alt+2" or "Ctrl+Alt+3" => () => Assert.Equal("Heading" + key[^1], Paragraph().StyleId),
            "Ctrl+Shift+N" => () => Assert.Null(Paragraph().StyleId),
            "Ctrl+S" => () => Assert.Equal(1, saved),
            "Ctrl+D" => () =>
            {
                Assert.Equal(version, document.Version);
                Assert.Contains(key, Surface(cut).Shortcuts!);
                Assert.NotEmpty(_dialogs.FindAll(".mud-ex-docfont-dialog"));
            },
            "Ctrl+F" or "Ctrl+H" => () =>
            {
                Assert.Equal(version, document.Version);
                Assert.Contains(key, Surface(cut).Shortcuts!);
                Assert.NotEmpty(cut.FindAll(".mud-ex-doc-find"));
            },
            _ => throw new InvalidOperationException($"No expectation for {key}.")
        };

        await cut.InvokeAsync(() => Surface(cut).OnCommandAsync(new SurfaceSync(Seen, [], new SurfaceSelection(new SurfacePoint(id, 0), new SurfacePoint(id, 5)), false), null, "shortcut", key));
        await cut.InvokeAsync(() => Task.Delay(20));

        cut.WaitForAssertion(check);
        if (key is not ("Ctrl+D" or "Ctrl+F" or "Ctrl+H" or "Ctrl+S"))
            Assert.Equal("Second", P(document, 1).Text);
    }
}
