using System.Text;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Notebook.NotebookBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Notebook;

using MudBlazor.Extensions.Office.Notebook;

public class NotebookLimitsTests
{
    private static byte[] Noise(int length, int seed = 1)
    {
        var data = new byte[length];
        new Random(seed).NextBytes(data);
        Png.CopyTo(data, 0);
        return data;
    }

    [Fact]
    public void AddImage_OverTheLimitOfTheLoadedNotebook_Refused()
    {
        var notebook = new NotebookBuilder().Page("p1").Load(new NotebookLoadOptions { MaxEntryBytes = 5000, MaxTotalBytes = 8000 });
        var page = notebook.Sections[0].Pages[0];

        notebook.Execute(new AddImageCommand(page, Noise(4000), "image/png", 0, 0, 100));
        Assert.Throws<InvalidOperationException>(() => notebook.Execute(new AddImageCommand(page, Noise(4000, 2), "image/png", 0, 0, 100)));
        Assert.Throws<InvalidOperationException>(() => notebook.Execute(new AddImageCommand(page, Noise(6000, 3), "image/png", 0, 0, 100)));

        Assert.Single(page.Elements);
        notebook.Undo.Undo();
        Assert.False(notebook.Undo.CanUndo);
    }

    [Fact]
    public void Save_OverTheLimit_ThrowsAndLeavesTargetUntouched()
    {
        var notebook = Notebook.Create();
        notebook.Limits = new NotebookLoadOptions { MaxTotalBytes = 50 };
        using var target = new MemoryStream();

        Assert.Throws<InvalidOperationException>(() => notebook.Save(target));

        Assert.Equal(0, target.Length);
    }

    [Fact]
    public void Load_FileAtTheUnpackedLimit_OpensDespiteZipOverhead()
    {
        var notebook = Notebook.Create();
        notebook.Execute(new AddImageCommand(notebook.Sections[0].Pages[0], Noise(100_000), "image/png", 0, 0, 100));
        var file = Save(notebook);
        var unpacked = Unzip(file).Values.Sum(d => (long)d.Length);
        Assert.True(file.Length > unpacked);

        var options = new NotebookLoadOptions { MaxEntryBytes = unpacked, MaxTotalBytes = unpacked };
        var loaded = Notebook.Load(new MemoryStream(file), options);

        Assert.Same(options, loaded.Limits);
        Assert.Equal(file.Length, Save(loaded).Length);
    }

    [Fact]
    public void Load_CorruptPictureAndForeignEntry_Skipped()
    {
        var builder = new NotebookBuilder().Page("p1", Image("i", "m1") + "," + Image("j", "m2")).Media("m1", "png", [1, 2, 3]).Media("m2", "png", [4, 5, 6]);
        builder.Entries["extra/notes.txt"] = Encoding.UTF8.GetBytes("foreign");
        var zip = CorruptData(CorruptData(builder.Build(), "media/m1.png"), "extra/notes.txt");

        var notebook = Notebook.Load(new MemoryStream(zip));

        Assert.Null(notebook.GetMedia("m1"));
        Assert.Equal([4, 5, 6], notebook.GetMedia("m2"));
        Assert.Equal(2, Page(notebook, "p1").Elements.Count);
        var saved = Unzip(Save(notebook));
        Assert.DoesNotContain("media/m1.png", saved.Keys);
        Assert.DoesNotContain("extra/notes.txt", saved.Keys);
        Assert.Contains("media/m2.png", saved.Keys);
    }

    [Fact]
    public void Save_MediumReferencedOnlyFromManifestOrForeignEntry_Kept()
    {
        var builder = new NotebookBuilder { ManifestExtra = ",\"cover\":\"c1\"" }.Page("p1").Media("c1", "png", [1]).Media("c2", "png", [2]).Media("c3", "png", [3]);
        builder.Entries["extra/index.json"] = Encoding.UTF8.GetBytes("{\"icon\":\"c2\"}");

        var saved = Unzip(Save(builder.Load()));

        Assert.Contains("media/c1.png", saved.Keys);
        Assert.Contains("media/c2.png", saved.Keys);
        Assert.DoesNotContain("media/c3.png", saved.Keys);
    }

    [Fact]
    public void AddText_And_AddShape_CreateElements_EmptyTextAddsNothing()
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        var text = new AddTextCommand(page, 10, 20, 300, [new NoteParagraph { Runs = [new NoteRun("Hello", new NoteRunFormat(Bold: true))] }]);
        var shape = new AddShapeCommand(page, NoteShapeKind.Line, new NoteBounds(5, 6, 70, 0), "#FF0000", 3, null, flipX: true);
        var empty = new AddTextCommand(page, 0, 0, 100, [new NoteParagraph()]);

        notebook.Execute(text);
        notebook.Execute(shape);
        notebook.Execute(empty);

        Assert.Equal([text.Text, shape.Shape], page.Elements);
        Assert.Null(empty.Text);
        var reloaded = Page(Reload(notebook), page.Id);
        Assert.Equal("Hello", ((TextElement)reloaded.Elements[0]).Paragraphs[0].Runs[0].Text);
        Assert.True(((ShapeElement)reloaded.Elements[1]) is { Kind: NoteShapeKind.Line, Stroke: "#FF0000", StrokeWidth: 3, FlipX: true, Width: 70 });
        Assert.Throws<ArgumentException>(() => notebook.Execute(new AddShapeCommand(page, NoteShapeKind.Rectangle, new NoteBounds(0, 0, 1, 1), "red")));
        Assert.Throws<ArgumentOutOfRangeException>(() => notebook.Execute(new AddTextCommand(page, double.NaN, 0, 100, [new NoteParagraph { Runs = [new NoteRun("x")] }])));
        notebook.Undo.Undo();
        notebook.Undo.Undo();
        Assert.False(notebook.Undo.CanUndo);
    }

    [Theory]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"Infinity\" height=\"10\"/>", "image/svg+xml")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1e400 10\"/>", "image/svg+xml")]
    public void AddImage_InfiniteSize_FallsBackOrClamps(string content, string type)
    {
        var notebook = Notebook.Create();
        var add = new AddImageCommand(notebook.Sections[0].Pages[0], Encoding.UTF8.GetBytes(content), type, 0, 0, 600);

        notebook.Execute(add);

        Assert.True(double.IsFinite(add.Image.Width) && double.IsFinite(add.Image.Height) && add.Image.Width > 0 && add.Image.Height > 0);
    }

    [Fact]
    public void AddImage_LyingHeader_ClampedToMaxSide()
    {
        var notebook = Notebook.Create();
        var add = new AddImageCommand(notebook.Sections[0].Pages[0], NotebookCommandTests.PngOfSize(1, int.MaxValue), "image/png", 0, 0, 600);

        notebook.Execute(add);

        Assert.True(add.Image.Height <= 10000);
    }

    [Fact]
    public void Resize_Stroke_KeepsBoundsEqualToHullPlusHalfWidth()
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        var stroke = new AddStrokeCommand(page, [new InkPoint(10, 10, 0.5), new InkPoint(110, 60, 0.5)], "#000000", 10, InkTool.Pen);
        notebook.Execute(stroke);

        notebook.Execute(new ResizeElementsCommand(page, new Dictionary<string, NoteBounds> { [stroke.Stroke.Id] = new(0, 0, 60, 35) }));

        var ink = (InkElement)page.Elements.Single();
        Assert.Equal(new NoteBounds(0, 0, 60, 35), ink.Bounds);
        Assert.Equal([new InkPoint(5, 5, 0.5), new InkPoint(55, 30, 0.5)], ink.Points);
    }
}
