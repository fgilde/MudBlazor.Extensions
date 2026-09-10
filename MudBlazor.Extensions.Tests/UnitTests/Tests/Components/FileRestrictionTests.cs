using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.FileManager;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Services;
using Nextended.Blazor.Models;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>
/// Covers <see cref="MudExFileRestrictions"/> and that <see cref="MudExFileManager"/> applies it on the path
/// a file takes when it is dropped straight onto a folder - not only in the upload dialog.
/// </summary>
/// <remarks>
/// Black lists used to be useless: the check derived an allowed set from the mime types instead of comparing
/// against the configured list, and the derived extensions carry no leading dot, so nothing ever matched.
/// The black list cases below are what fails again if that comes back.
/// </remarks>
public class FileRestrictionTests
{
    private sealed class FakeBrowserFile : IBrowserFile
    {
        public FakeBrowserFile(string name, string contentType, long size)
        {
            Name = name;
            ContentType = contentType;
            Size = size;
        }

        public string Name { get; }
        public DateTimeOffset LastModified { get; } = DateTimeOffset.Now;
        public long Size { get; }
        public string ContentType { get; }

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
            => new MemoryStream(new byte[Size]);
    }

    [Fact]
    public void NothingConfiguredAllowsEverything()
    {
        var restrictions = new MudExFileRestrictions();

        Assert.True(restrictions.IsEmpty);
        Assert.Null(restrictions.Validate("anything.exe", "application/octet-stream", 1024 * 1024));
    }

    [Fact]
    public void AnExtensionBlackListRefusesWhatItNames()
    {
        var restrictions = new MudExFileRestrictions
        {
            Extensions = new[] { "exe", ".bat" },
            ExtensionRestrictionType = RestrictionType.BlackList
        };

        Assert.False(restrictions.IsExtensionAllowed(".exe"));
        Assert.False(restrictions.IsExtensionAllowed("bat"));
        Assert.True(restrictions.IsExtensionAllowed(".txt"));
        Assert.NotNull(restrictions.Validate("setup.exe", "application/octet-stream", 10));
    }

    [Fact]
    public void AnExtensionWhiteListRefusesEverythingElse()
    {
        var restrictions = new MudExFileRestrictions
        {
            Extensions = new[] { ".png", ".jpg" }
        };

        Assert.True(restrictions.IsExtensionAllowed(".PNG"));
        Assert.False(restrictions.IsExtensionAllowed(".gif"));
    }

    [Fact]
    public void AMimeTypeBlackListRefusesWhatItNames()
    {
        var restrictions = new MudExFileRestrictions
        {
            MimeTypes = new[] { "image/*" },
            MimeRestrictionType = RestrictionType.BlackList
        };

        Assert.False(restrictions.IsMimeTypeAllowed("image/png"));
        Assert.True(restrictions.IsMimeTypeAllowed("text/plain"));
    }

    [Fact]
    public void AMimeTypeWhiteListRefusesEverythingElse()
    {
        var restrictions = new MudExFileRestrictions
        {
            MimeTypes = new[] { "image/*" }
        };

        Assert.True(restrictions.IsMimeTypeAllowed("image/jpeg"));
        Assert.False(restrictions.IsMimeTypeAllowed("application/pdf"));
    }

    [Fact]
    public void TheSizeLimitCounts()
    {
        var restrictions = new MudExFileRestrictions { MaxFileSize = 100 };

        Assert.True(restrictions.IsSizeAllowed(100));
        Assert.False(restrictions.IsSizeAllowed(101));
        Assert.Contains("maximum size", restrictions.Validate("a.txt", "text/plain", 101));
    }

    [Fact]
    public void TheUploadEditUsesTheSameRules()
    {
        var upload = new MudExUploadEdit<UploadableFile>
        {
            Extensions = new[] { "exe" },
            ExtensionRestrictionType = RestrictionType.BlackList,
            MaxFileSize = 50
        };

        var restrictions = upload.Restrictions;

        Assert.False(restrictions.IsExtensionAllowed(".exe"));
        Assert.True(restrictions.IsExtensionAllowed(".txt"));
        Assert.False(restrictions.IsSizeAllowed(51));
    }

    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    private static IRenderedComponent<MudExFileManager> RenderManager(
        TestContext context, MudExInMemoryFileStructureManager structure, MudExFileRestrictions restrictions)
        => context.Render<MudExFileManager>(parameters => parameters
            .Add(c => c.Manager, structure)
            .Add(c => c.Restrictions, restrictions));

    private static async Task<List<string>> NamesIn(
        MudExInMemoryFileStructureManager structure, MudExFileStructureNode target)
        => (await structure.GetChildrenAsync(target)).Select(n => n.Name).ToList();

    private static MudExInMemoryFileStructureManager CreateStructure()
    {
        var structure = new MudExInMemoryFileStructureManager();
        structure.AddFile("documents/report.md", 12);
        return structure;
    }

    [Fact]
    public async Task ARefusedFileIsNotWrittenAndSaysWhy()
    {
        await using var context = CreateContext();
        var structure = CreateStructure();
        var cut = RenderManager(context, structure, new MudExFileRestrictions
        {
            Extensions = new[] { "exe" },
            ExtensionRestrictionType = RestrictionType.BlackList
        });

        var target = cut.Instance.RootItems.First(n => n.Name == "documents");
        await cut.InvokeAsync(() => cut.Instance.UploadFilesAsync(
            target, new IBrowserFile[] { new FakeBrowserFile("setup.exe", "application/octet-stream", 4) }));

        Assert.DoesNotContain("setup.exe", await NamesIn(structure, target));
        Assert.Contains("setup.exe", cut.Markup);
    }

    [Fact]
    public async Task TheAcceptedFilesOfAMixedDropStillGoThrough()
    {
        await using var context = CreateContext();
        var structure = CreateStructure();
        var cut = RenderManager(context, structure, new MudExFileRestrictions
        {
            Extensions = new[] { "exe" },
            ExtensionRestrictionType = RestrictionType.BlackList
        });

        var target = cut.Instance.RootItems.First(n => n.Name == "documents");
        await cut.InvokeAsync(() => cut.Instance.UploadFilesAsync(target, new IBrowserFile[]
        {
            new FakeBrowserFile("setup.exe", "application/octet-stream", 4),
            new FakeBrowserFile("notes.txt", "text/plain", 4)
        }));

        var names = await NamesIn(structure, target);
        Assert.Contains("notes.txt", names);
        Assert.DoesNotContain("setup.exe", names);
    }

    [Fact]
    public async Task WithoutRestrictionsEverythingIsWritten()
    {
        await using var context = CreateContext();
        var structure = CreateStructure();
        var cut = RenderManager(context, structure, null);

        var target = cut.Instance.RootItems.First(n => n.Name == "documents");
        await cut.InvokeAsync(() => cut.Instance.UploadFilesAsync(
            target, new IBrowserFile[] { new FakeBrowserFile("setup.exe", "application/octet-stream", 4) }));

        Assert.Contains("setup.exe", await NamesIn(structure, target));
    }
}
