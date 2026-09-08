using System.IO.Compression;
using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>
/// Covers <see cref="MudExUploadEdit{T}.RestrictArchiveContent"/>: whether the extension and mime type
/// restrictions also apply to the files inside an archive that is kept as it is.
/// </summary>
public class UploadEditArchiveRestrictionTests
{
    private static byte[] Zip(params string[] entryNames)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            foreach (var name in entryNames)
            {
                using var writer = new StreamWriter(archive.CreateEntry(name).Open());
                writer.Write("content of " + name);
            }
        }
        return stream.ToArray();
    }

    private static UploadableFile File(string name, byte[] data) => new() { FileName = name, Data = data };

    private sealed record Harness(
        IRenderedComponent<MudExUploadEdit<UploadableFile>> Component,
        Func<string> LastError)
    {
        public MudExUploadEdit<UploadableFile> Instance => Component.Instance;
        public int Accepted => Instance.UploadRequests?.Count ?? 0;
    }

    private static Harness Render(TestContext context, Action<ComponentParameterCollectionBuilder<MudExUploadEdit<UploadableFile>>> configure)
    {
        string lastError = null;

        var cut = context.Render<MudExUploadEdit<UploadableFile>>(parameters =>
        {
            parameters.Add(c => c.AllowMultiple, true);
            parameters.Add(c => c.OnError, EventCallback.Factory.Create<string>(new object(), message => lastError = message));
            configure(parameters);
        });

        // Extensions and mime types are expanded on a background task after the first render, and "*" is the
        // placeholder until that finished. Adding a file before it lands would evaluate against nothing.
        cut.WaitForAssertion(
            () => Assert.NotEqual("*", cut.Find("input[type=file]").GetAttribute("accept")),
            TimeSpan.FromSeconds(10));

        return new Harness(cut, () => lastError);
    }

    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    [Fact]
    public async Task Default_KeepsArchiveContentUnchecked()
    {
        await using var context = CreateContext();
        var harness = Render(context, p =>
        {
            p.Add(c => c.Extensions, new[] { ".zip", ".txt", ".doc" });
            p.Add(c => c.ExtensionRestrictionType, RestrictionType.WhiteList);
        });

        await harness.Instance.Add(File("bundle.zip", Zip("notes.txt", "evil.exe")));

        // RestrictArchiveContent defaults to false, so an allowed archive may contain anything
        Assert.Equal(1, harness.Accepted);
        Assert.Null(harness.LastError());
    }

    [Fact]
    public async Task WhiteList_RejectsArchiveWithForbiddenEntry()
    {
        await using var context = CreateContext();
        var harness = Render(context, p =>
        {
            p.Add(c => c.Extensions, new[] { ".zip", ".txt", ".doc" });
            p.Add(c => c.ExtensionRestrictionType, RestrictionType.WhiteList);
            p.Add(c => c.RestrictArchiveContent, true);
        });

        await harness.Instance.Add(File("bundle.zip", Zip("notes.txt", "evil.exe")));

        Assert.Equal(0, harness.Accepted);
        Assert.Contains("bundle.zip", harness.LastError());
        Assert.Contains("evil.exe", harness.LastError());
        Assert.DoesNotContain("notes.txt", harness.LastError());
    }

    [Fact]
    public async Task WhiteList_AcceptsArchiveWhenEveryEntryIsAllowed()
    {
        await using var context = CreateContext();
        var harness = Render(context, p =>
        {
            p.Add(c => c.Extensions, new[] { ".zip", ".txt", ".doc" });
            p.Add(c => c.ExtensionRestrictionType, RestrictionType.WhiteList);
            p.Add(c => c.RestrictArchiveContent, true);
        });

        await harness.Instance.Add(File("bundle.zip", Zip("notes.txt", "letter.doc", "sub/more.txt")));

        Assert.Equal(1, harness.Accepted);
        Assert.Null(harness.LastError());
    }

    [Fact]
    public async Task MimeWhiteList_AlsoAppliesToArchiveContent()
    {
        await using var context = CreateContext();
        var harness = Render(context, p =>
        {
            p.Add(c => c.MimeTypes, new[] { "application/zip", "text/plain" });
            p.Add(c => c.MimeRestrictionType, RestrictionType.WhiteList);
            p.Add(c => c.RestrictArchiveContent, true);
        });

        // The entry mime type is derived from its name, so a png inside is a mime violation
        await harness.Instance.Add(File("bundle.zip", Zip("notes.txt", "picture.png")));

        Assert.Equal(0, harness.Accepted);
        Assert.Contains("picture.png", harness.LastError());
    }

    [Fact]
    public async Task MimeBlackList_AlsoAppliesToArchiveContent()
    {
        await using var context = CreateContext();
        var harness = Render(context, p =>
        {
            // The blacklist direction is asserted on the mime dimension: an extension blacklist currently
            // subtracts ".exe" from a set that holds "exe" without the dot, so it removes nothing.
            p.Add(c => c.MimeTypes, new[] { "image/png" });
            p.Add(c => c.MimeRestrictionType, RestrictionType.BlackList);
            p.Add(c => c.RestrictArchiveContent, true);
        });

        await harness.Instance.Add(File("blocked.zip", Zip("notes.txt", "picture.png")));
        Assert.Equal(0, harness.Accepted);
        Assert.Contains("picture.png", harness.LastError());

        await harness.Instance.Add(File("clean.zip", Zip("notes.txt", "letter.doc")));
        Assert.Equal(1, harness.Accepted);
    }

    [Fact]
    public async Task NonArchive_IsNotAffected()
    {
        await using var context = CreateContext();
        var harness = Render(context, p =>
        {
            p.Add(c => c.Extensions, new[] { ".zip", ".txt" });
            p.Add(c => c.ExtensionRestrictionType, RestrictionType.WhiteList);
            p.Add(c => c.RestrictArchiveContent, true);
        });

        await harness.Instance.Add(File("notes.txt", "plain text"u8.ToArray()));

        Assert.Equal(1, harness.Accepted);
        Assert.Null(harness.LastError());
    }
}
