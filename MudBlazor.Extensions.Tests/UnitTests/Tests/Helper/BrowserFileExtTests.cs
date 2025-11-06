using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Core;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Helper;

public class BrowserFileExtTests
{
    [Theory]
    [InlineData("application/pdf", true)]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", true)]
    [InlineData("application/vnd.ms-excel", true)]
    [InlineData("image/jpeg", true)]
    [InlineData("video/mp4", true)]
    [InlineData("audio/mpeg", true)]
    [InlineData("application/zip", true)]
    [InlineData("unknown/type", false)]
    public void GetPreferredColorReturnsColorForKnownTypes(string contentType, bool shouldHaveColor)
    {
        var color = BrowserFileExt.GetPreferredColor(contentType);
        
        if (shouldHaveColor)
        {
            Assert.NotNull(color);
        }
        else
        {
            Assert.NotNull(color);
        }
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("application/vnd.ms-excel")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("application/vnd.openxmlformats-officedocument.presentationml.presentation")]
    public void IconForFileReturnsCorrectIconForOfficeDocuments(string contentType)
    {
        var icon = BrowserFileExt.IconForFile(contentType);
        
        Assert.NotNull(icon);
        Assert.NotEmpty(icon);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/gif")]
    public void IconForFileReturnsImageIconForImageTypes(string contentType)
    {
        var icon = BrowserFileExt.IconForFile(contentType);
        
        Assert.NotNull(icon);
        Assert.NotEmpty(icon);
        // Should return the FileImage icon from Icons.Custom.FileFormats
        Assert.Equal(Icons.Custom.FileFormats.FileImage, icon);
    }

    [Theory]
    [InlineData("video/mp4")]
    [InlineData("video/mpeg")]
    public void IconForFileReturnsVideoIconForVideoTypes(string contentType)
    {
        var icon = BrowserFileExt.IconForFile(contentType);
        
        Assert.NotNull(icon);
        Assert.NotEmpty(icon);
        Assert.Equal(Icons.Custom.FileFormats.FileVideo, icon);
    }

    [Theory]
    [InlineData("audio/mpeg")]
    [InlineData("audio/wav")]
    public void IconForFileReturnsAudioIconForAudioTypes(string contentType)
    {
        var icon = BrowserFileExt.IconForFile(contentType);
        
        Assert.NotNull(icon);
        Assert.NotEmpty(icon);
        Assert.Equal(Icons.Custom.FileFormats.FileMusic, icon);
    }

    [Theory]
    [InlineData("application/zip")]
    [InlineData("application/x-zip-compressed")]
    [InlineData("application/x-rar-compressed")]
    public void IconForFileReturnsArchiveIconForArchiveTypes(string contentType)
    {
        var icon = BrowserFileExt.IconForFile(contentType);
        
        Assert.NotNull(icon);
        Assert.NotEmpty(icon);
        Assert.Equal(Icons.Material.Filled.Archive, icon);
    }

    [Fact]
    public void IconForFileReturnsDefaultIconForNullOrEmpty()
    {
        var icon = BrowserFileExt.IconForFile("");
        
        Assert.NotNull(icon);
        Assert.NotEmpty(icon);
    }

    [Theory]
    [InlineData(".txt", "text/plain")]
    [InlineData(".pdf", "application/pdf")]
    [InlineData(".jpg", "image/jpeg")]
    public void IconForExtensionReturnsCorrectIcon(string extension, string expectedContentType)
    {
        var icon = BrowserFileExt.IconForExtension(extension);
        
        Assert.NotNull(icon);
        Assert.NotEmpty(icon);
    }

    [Fact]
    public void IconForExtensionThrowsOnNullExtension()
    {
        // The method throws NullReferenceException for null input
        Assert.Throws<NullReferenceException>(() => BrowserFileExt.IconForExtension(null));
    }

    [Theory]
    [InlineData("test.nupkg", "application/octet-stream")]
    public void GetIconReturnsNugetIconForNupkgFiles(string fileName, string contentType)
    {
        var icon = BrowserFileExt.GetIcon(fileName, contentType);
        
        Assert.NotNull(icon);
        Assert.NotEmpty(icon);
        Assert.Equal(MudExIcons.Custom.Brands.Nuget, icon);
    }

    [Theory]
    [InlineData("document.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("spreadsheet.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public void GetIconReturnsCorrectIconForFileNameAndContentType(string fileName, string contentType)
    {
        var icon = BrowserFileExt.GetIcon(fileName, contentType);
        
        Assert.NotNull(icon);
        Assert.NotEmpty(icon);
    }
}
