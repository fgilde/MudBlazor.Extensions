using MainSample.WebAssembly.Examples.Notebook;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Notebook;

using MudBlazor.Extensions.Office.Notebook;

public class NotebookSamplesTests
{
    private static string SamplePath([System.Runtime.CompilerServices.CallerFilePath] string file = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, "../../../../Samples/MainSample.WebAssembly/wwwroot/sample-data/sample.mxnb"));

    [Fact]
    public void NotebookSamples_RoundTrip()
    {
        var notebook = NotebookSamples.CreateNotebook();
        var reloaded = NotebookBuilder.Reload(notebook);

        Assert.Equal(2, reloaded.Sections.Count);
        Assert.All(reloaded.Sections.SelectMany(s => s.Pages), p => Assert.Null(p.LoadError));
        var elements = reloaded.Sections.SelectMany(s => s.Pages).SelectMany(p => p.Elements).ToList();
        Assert.Contains(elements, e => e is TextElement t && t.Paragraphs.Any(p => p.Heading != NoteHeading.None));
        Assert.Contains(elements, e => e is TextElement t && t.Paragraphs.Any(p => p.List == NoteListKind.Bullet));
        Assert.Contains(elements, e => e is TextElement t && t.Paragraphs.Any(p => p.List == NoteListKind.Number && p.Indent == 1));
        Assert.Contains(elements, e => e is TextElement t && t.Paragraphs.Any(p => p.List == NoteListKind.Todo && p.Checked));
        Assert.Contains(elements, e => e is TableElement { Rows.Count: >= 3 });
        Assert.Contains(elements, e => e is ImageElement { MediaId: not null });
        Assert.Contains(elements, e => e is ShapeElement);
        Assert.Contains(elements, e => e is InkElement { Tool: InkTool.Pen });
        Assert.Contains(elements, e => e is InkElement { Tool: InkTool.Highlighter });
        Assert.Contains(elements, e => e is InkElement { Points.Count: 1 });
    }

    [Fact]
    public void SampleFile_MatchesGenerator()
    {
        Assert.Equal(NotebookSamples.CreateFile(), File.ReadAllBytes(SamplePath()));
    }
}
