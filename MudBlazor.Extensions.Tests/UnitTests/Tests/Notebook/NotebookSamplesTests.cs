using System.Text.Json;
using System.Text.Json.Nodes;
using MainSample.WebAssembly.Examples.Notebook;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Notebook;

using MudBlazor.Extensions.Office.Notebook;

public class NotebookSamplesTests
{
    private static readonly string[] IgnoredProperties = ["id", "created", "modified"];

    public static TheoryData<string> SampleFiles => new() { "MainSample.WebAssembly", "MudEx.WebComponents" };

    private static string SamplePath(string project, [System.Runtime.CompilerServices.CallerFilePath] string file = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, $"../../../../Samples/{project}/wwwroot/sample-data/sample.mxnb"));

    [Fact]
    public void NotebookSamples_RoundTrip()
    {
        var notebook = NotebookSamples.CreateNotebook();
        var reloaded = NotebookBuilder.Reload(notebook);

        Assert.Equal(2, reloaded.Sections.Count);
        Assert.Equal([3, 2], reloaded.Sections.Select(s => s.Pages.Count));
        Assert.All(reloaded.Sections.SelectMany(s => s.Pages), p => Assert.Null(p.LoadError));
        var elements = reloaded.Sections.SelectMany(s => s.Pages).SelectMany(p => p.Elements).ToList();
        Assert.Contains(elements, e => e is TextElement t && t.Paragraphs.Any(p => p.Heading != NoteHeading.None));
        Assert.Contains(elements, e => e is TextElement t && t.Paragraphs.Any(p => p.List == NoteListKind.Bullet));
        Assert.Contains(elements, e => e is TextElement t && t.Paragraphs.Any(p => p.List == NoteListKind.Number && p.Indent == 1));
        Assert.Contains(elements, e => e is TextElement t && t.Paragraphs.Any(p => p.List == NoteListKind.Todo && p.Checked));
        Assert.Contains(elements, e => e is TableElement { Rows.Count: >= 3, HeaderRow: true });
        Assert.Contains(elements, e => e is ImageElement { MediaId: not null, Width: 160, Height: 100 });
        Assert.Contains(elements, e => e is ShapeElement);
        Assert.Contains(elements, e => e is InkElement { Tool: InkTool.Pen });
        Assert.Contains(elements, e => e is InkElement { Tool: InkTool.Highlighter });
        Assert.Contains(elements, e => e is InkElement { Points.Count: 1 });
    }

    [Theory]
    [MemberData(nameof(SampleFiles))]
    public void SampleFile_MatchesGenerator(string project)
    {
        using var file = File.OpenRead(SamplePath(project));

        Assert.Equal(Describe(NotebookSamples.CreateNotebook()), Describe(Notebook.Load(file)));
    }

    private static string Describe(Notebook notebook)
    {
        var pages = notebook.Sections.SelectMany(s => s.Pages).Select(p => Strip(JsonSerializer.SerializeToNode(NotebookJson.ToDto(p), NotebookJsonContext.Default.PageDto)!));
        return string.Join("\n", notebook.Sections.Select(s => $"{s.Name} {s.Color} {s.Pages.Count}").Concat(pages));
    }

    private static string Strip(JsonNode node)
    {
        foreach (var child in node is JsonObject o ? o.Select(p => p.Value) : node is JsonArray a ? a.AsEnumerable() : [])
            if (child != null) Strip(child);
        if (node is JsonObject obj)
            foreach (var name in IgnoredProperties) obj.Remove(name);
        return node.ToJsonString();
    }
}
