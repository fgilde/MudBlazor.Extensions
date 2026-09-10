using MudBlazor;
using MudBlazor.Extensions.Core.FileManager;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.FileManager;

public class FileStructureNodeTests
{
    [Fact]
    public void File_DerivesContentTypeIconAndColorFromItsName()
    {
        var node = new MudExFileStructureNode { Name = "report.pdf", FullPath = "docs/report.pdf" };

        Assert.Equal("application/pdf", node.ContentType);
        Assert.Equal(Icons.Custom.FileFormats.FilePdf, node.Icon);
        Assert.False(node.IsDirectory);
    }

    [Fact]
    public void ExplicitContentType_WinsOverTheDerivedOne()
    {
        var node = new MudExFileStructureNode { Name = "data.bin", ContentType = "image/png" };

        Assert.Equal("image/png", node.ContentType);
        Assert.Equal(Icons.Custom.FileFormats.FileImage, node.Icon);
    }

    [Fact]
    public void Directory_IconFollowsTheExpandedState()
    {
        // A file may cache its icon, a directory must not: expanding it has to change the glyph.
        var node = new MudExFileStructureNode { Name = "docs", IsDirectory = true };

        Assert.Equal(Icons.Material.Filled.Folder, node.Icon);

        node.IsExpanded = true;
        Assert.Equal(Icons.Material.Filled.FolderOpen, node.Icon);

        node.IsExpanded = false;
        Assert.Equal(Icons.Material.Filled.Folder, node.Icon);
    }

    [Fact]
    public void Directory_HasNoContentType()
    {
        var node = new MudExFileStructureNode { Name = "docs", IsDirectory = true };

        Assert.Null(node.ContentType);
    }

    [Fact]
    public void RenamingAFile_DropsTheDerivedContentTypeIconAndColor()
    {
        // Icon, content type and color are cached the first time they are read. Without invalidating them,
        // a rename from .txt to .pdf keeps showing the text icon and reporting text/plain forever.
        var node = new MudExFileStructureNode { Name = "a.txt", FullPath = "docs/a.txt" };

        Assert.Equal("text/plain", node.ContentType);
        Assert.Equal(Icons.Custom.FileFormats.FileDocument, node.Icon);
        var textColor = node.Color.ToString();

        node.Name = "a.pdf";
        node.FullPath = "docs/a.pdf";

        Assert.Equal("application/pdf", node.ContentType);
        Assert.Equal(Icons.Custom.FileFormats.FilePdf, node.Icon);
        Assert.NotEqual(textColor, node.Color.ToString());
    }

    [Fact]
    public void ToString_ReturnsTheName()
    {
        // MudExTreeView uses ToString() as the default label, so this is load bearing
        Assert.Equal("report.pdf", new MudExFileStructureNode { Name = "report.pdf" }.ToString());
    }

    [Fact]
    public void Capabilities_AllContainsEveryIndividualFlag()
    {
        foreach (var flag in Enum.GetValues<MudExFileManagerCapabilities>())
        {
            if (flag == MudExFileManagerCapabilities.None || flag == MudExFileManagerCapabilities.All)
                continue;
            Assert.True(MudExFileManagerCapabilities.All.HasFlag(flag), $"All is missing {flag}");
        }
    }
}
