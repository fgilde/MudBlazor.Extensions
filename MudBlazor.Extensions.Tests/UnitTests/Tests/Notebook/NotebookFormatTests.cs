using System.Text;
using System.Text.Json.Nodes;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Notebook.NotebookBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Notebook;

using MudBlazor.Extensions.Office.Notebook;

public class NotebookFormatTests
{
    [Fact]
    public void Create_HasOneSectionAndOnePage()
    {
        var notebook = Notebook.Create("Mine");

        Assert.Equal("Mine", notebook.Name);
        var section = Assert.Single(notebook.Sections);
        Assert.Equal("Section 1", section.Name);
        Assert.Matches("^#[0-9A-F]{6}$", section.Color);
        var page = Assert.Single(section.Pages);
        Assert.Empty(page.Elements);
        Assert.Null(page.LoadError);
        Assert.False(notebook.ReadOnlyFormat);
        Assert.False(notebook.Undo.CanUndo);
    }

    [Fact]
    public void RoundTrip_AllElementKinds_EqualsModel()
    {
        var notebook = AllKinds();

        var loaded = Reload(notebook);

        Assert.Equal(notebook.Name, loaded.Name);
        Assert.Equal(notebook.Sections.Count, loaded.Sections.Count);
        for (var s = 0; s < notebook.Sections.Count; s++)
        {
            var (expected, actual) = (notebook.Sections[s], loaded.Sections[s]);
            Assert.Equal((expected.Id, expected.Name, expected.Color), (actual.Id, actual.Name, actual.Color));
            Assert.Equal(expected.Pages.Select(p => (p.Id, p.Title, p.Level, p.Created, p.LastModified)),
                actual.Pages.Select(p => (p.Id, p.Title, p.Level, p.Created, p.LastModified)));
            for (var p = 0; p < expected.Pages.Count; p++)
            {
                Assert.Equal(expected.Pages[p].Elements.Select(e => e.GetType()), actual.Pages[p].Elements.Select(e => e.GetType()));
                for (var e = 0; e < expected.Pages[p].Elements.Count; e++)
                    Assert.Equivalent(expected.Pages[p].Elements[e], actual.Pages[p].Elements[e], strict: true);
            }
        }

        var image = loaded.Sections[0].Pages[0].Elements.OfType<ImageElement>().Single();
        Assert.Equal(Png, loaded.GetMedia(image.MediaId));
    }

    [Fact]
    public void Save_UnchangedPagesAndMedia_ByteIdentical()
    {
        var builder = new NotebookBuilder()
            .Page("p1", Image("i1", "m1"))
            .Page("p2", Text("t", "{\"text\":\"old\"}"))
            .Section("s2")
            .Page("p3", Image("i3", "m2"))
            .Media("m1", "png", [1, 2, 3])
            .Media("m2", "jpg", [4, 5, 6]);
        builder.Entries["thumbnails/cover.bin"] = [9, 9];
        var original = Unzip(builder.Build());
        var notebook = builder.Load();
        var page = Page(notebook, "p2");
        page.Title = "Changed";
        notebook.Notify(page, NotebookChangeKind.Content);

        var saved = Unzip(Save(notebook));

        foreach (var name in new[] { "pages/p1.json", "pages/p3.json", "media/m1.png", "media/m2.jpg", "thumbnails/cover.bin" })
            Assert.Equal(original[name], saved[name]);
        Assert.NotEqual(original["pages/p2.json"], saved["pages/p2.json"]);
        Assert.Equal("Changed", Page(Notebook.Load(new MemoryStream(Save(notebook))), "p2").Title);
    }

    [Fact]
    public void Save_UnusedMedia_Removed_SharedMediaKept()
    {
        var notebook = new NotebookBuilder()
            .Page("p1", Image("a", "m1") + "," + Image("b", "shared"))
            .Page("p2", Image("c", "shared"))
            .Media("m1", "png", [1])
            .Media("shared", "gif", [2])
            .Load();
        var first = Page(notebook, "p1");
        first.ElementList.Clear();
        notebook.Notify(first, NotebookChangeKind.Content);

        var saved = Unzip(Save(notebook));

        Assert.DoesNotContain("media/m1.png", saved.Keys);
        Assert.Equal([2], saved["media/shared.gif"]);

        var second = Page(notebook, "p2");
        second.ElementList.Clear();
        notebook.Notify(second, NotebookChangeKind.Content);

        Assert.DoesNotContain("media/shared.gif", Unzip(Save(notebook)).Keys);
    }

    [Fact]
    public void Load_UnknownJsonProperties_SurviveRoundTrip()
    {
        var builder = new NotebookBuilder { ManifestExtra = ",\"fNotebook\":{\"a\":1}" }
            .Section("s1", ",\"fSection\":true")
            .Page("p1", Text("t", "{\"text\":\"x\",\"fRun\":2}", ",\"fParagraph\":\"p\"").Replace("\"x\":5", "\"x\":5,\"fElement\":[1,2]"), ",\"fPage\":3");
        var notebook = builder.Load();
        var page = Page(notebook, "p1");
        page.Title = "Edited";
        notebook.Notify(page, NotebookChangeKind.Content);

        var saved = Unzip(Save(notebook));

        var manifest = JsonNode.Parse(saved["notebook.json"])!;
        Assert.Equal(1, (int)manifest["fNotebook"]!["a"]!);
        Assert.True((bool)manifest["sections"]![0]!["fSection"]!);
        var json = JsonNode.Parse(saved["pages/p1.json"])!;
        Assert.Equal("Edited", (string)json["title"]!);
        Assert.Equal(3, (int)json["fPage"]!);
        var element = json["elements"]![0]!;
        Assert.Equal("[1,2]", element["fElement"]!.ToJsonString());
        Assert.Equal("p", (string)element["paragraphs"]![0]!["fParagraph"]!);
        Assert.Equal(2, (int)element["paragraphs"]![0]!["runs"]![0]!["fRun"]!);
    }

    [Fact]
    public void Load_NewerMajorVersion_IsReadOnlyFormat()
    {
        var notebook = new NotebookBuilder { Version = "2.1" }.Page("p1").Load();

        Assert.True(notebook.ReadOnlyFormat);
        Assert.Throws<InvalidOperationException>(() => Save(notebook));
        Assert.False(new NotebookBuilder { Version = "1.7" }.Page("p1").Load().ReadOnlyFormat);
    }

    [Theory]
    [InlineData("../evil")]
    [InlineData("pages/../../evil.json")]
    [InlineData("/etc/passwd")]
    [InlineData("C:/windows/evil")]
    [InlineData("pages\\..\\evil")]
    public void Load_ZipSlipEntry_Rejected(string name)
    {
        var builder = new NotebookBuilder().Page("p1");
        builder.Entries[name] = [1];

        Assert.Throws<InvalidDataException>(() => builder.Load());
    }

    [Fact]
    public void Load_OverLimit_Throws()
    {
        var builder = new NotebookBuilder().Page("p1").Media("m1", "png", new byte[5000]).Media("m2", "png", new byte[5000]);

        Assert.Throws<InvalidDataException>(() => builder.Load(new NotebookLoadOptions { MaxEntryBytes = 4000 }));
        Assert.Throws<InvalidDataException>(() => builder.Load(new NotebookLoadOptions { MaxTotalBytes = 8000 }));
        Assert.NotNull(builder.Load(new NotebookLoadOptions { MaxEntryBytes = 6000, MaxTotalBytes = 12000 }));
    }

    [Theory]
    [InlineData("{ \"title\": \"broken\", \"elements\": [")]
    [InlineData("{\"elements\":[{\"type\":\"hologram\",\"id\":\"h\"}]}")]
    [InlineData("{\"elements\":[{\"type\":\"ink\",\"id\":\"i\",\"points\":[1,2]}]}")]
    [InlineData("{\"elements\":[{\"type\":\"ink\",\"id\":\"i\"},{\"type\":\"ink\",\"id\":\"i\"}]}")]
    public void Load_BrokenPageJson_GivesErrorPage_KeepsOriginalOnSave(string json)
    {
        var broken = json.Replace("\"elements\": [", "\"elements\": [ \"media-in-broken\" ", StringComparison.Ordinal);
        var builder = new NotebookBuilder().Page("p1", Image("i", "m1")).RawPage("bad", broken).Page("p3")
            .Media("m1", "png", [1]).Media("media-in-broken", "png", [2]).Media("orphan", "png", [3]);

        var notebook = builder.Load();

        var page = Page(notebook, "bad");
        Assert.False(string.IsNullOrEmpty(page.LoadError));
        Assert.Empty(page.Elements);
        Assert.Null(Page(notebook, "p1").LoadError);
        Assert.Single(Page(notebook, "p1").Elements);
        var saved = Unzip(Save(notebook));
        Assert.Equal(Encoding.UTF8.GetBytes(broken), saved["pages/bad.json"]);
        Assert.Equal(broken.Contains("media-in-broken"), saved.ContainsKey("media/media-in-broken.png"));
        Assert.DoesNotContain("media/orphan.png", saved.Keys);
        Assert.Equal(["p1", "bad", "p3"], Notebook.Load(new MemoryStream(Save(notebook))).Sections[0].Pages.Select(p => p.Id));
    }

    [Theory]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("JavaScript:alert(1)", null)]
    [InlineData("data:text/html,x", null)]
    [InlineData("relative/path", null)]
    [InlineData("https://example.test/x", "https://example.test/x")]
    [InlineData("http://example.test", "http://example.test")]
    [InlineData("mailto:a@example.test", "mailto:a@example.test")]
    public void Load_LinkWithJavascriptScheme_IsDropped(string link, string? expected)
    {
        var notebook = new NotebookBuilder().Page("p1", Text("t", $"{{\"text\":\"x\",\"link\":\"{link}\"}}")).Load();

        var text = (TextElement)Page(notebook, "p1").Elements.Single();

        Assert.Equal(expected, text.Paragraphs[0].Runs[0].Format.Link);
    }

    [Fact]
    public void Media_SameBytes_SameId()
    {
        var notebook = Notebook.Create();

        var first = notebook.AddMedia([1, 2, 3], "image/png");
        var second = notebook.AddMedia([1, 2, 3], "image/png");
        var other = notebook.AddMedia([1, 2, 4], "image/png");

        Assert.Equal(first, second);
        Assert.NotEqual(first, other);
        Assert.Equal([1, 2, 3], notebook.GetMedia(first));
        Assert.Null(notebook.GetMedia("missing"));
        Assert.Throws<ArgumentException>(() => notebook.AddMedia([1], "text/html"));
    }

    [Fact]
    public void ContentBounds_CoversAllElements()
    {
        var page = new NotebookPage();
        Assert.Equal(default, page.ContentBounds);

        page.ElementList.Add(new ShapeElement { Id = "a", X = 10, Y = 20, Width = 30, Height = 40 });
        page.ElementList.Add(new InkElement { Id = "b", X = 100, Y = 5, Width = 50, Height = 10 });
        page.ElementList.Add(new TextElement { Id = "c", X = 0, Y = 300, Width = 20 });

        Assert.Equal(new NoteBounds(0, 5, 150, 295), page.ContentBounds);
        Assert.Equal(150, page.ContentBounds.Right);
        Assert.Equal(300, page.ContentBounds.Bottom);
    }
}
