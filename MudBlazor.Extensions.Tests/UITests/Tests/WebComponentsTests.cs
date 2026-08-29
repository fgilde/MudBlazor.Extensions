using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Xunit;

namespace MudBlazor.Extensions.Tests.UITests.Tests;

/// <summary>
/// Proves that the components really work as browser custom elements - loaded from the same origin
/// like on www.mudex.org and from a foreign origin like any other site embedding the cdn script.
///
/// Requires the bundle to be published first:
///   dotnet publish Samples/MudEx.WebComponents -c Debug
/// </summary>
[Collection(PlaywrightFixture.PlaywrightCollection)]
public class WebComponentsTests : IAsyncLifetime
{
    private const int SitePort = 5111;
    private const int BundlePort = 5112;
    private const int ForeignPort = 5113;

    private readonly PlaywrightFixture _playwrightFixture;
    private readonly string _siteRoot;
    private readonly string _bundleRoot;
    private readonly string _foreignRoot;

    private WebApplication? _site;
    private WebApplication? _bundle;
    private WebApplication? _foreign;

    public WebComponentsTests(PlaywrightFixture playwrightFixture)
    {
        _playwrightFixture = playwrightFixture;
        var repositoryRoot = FindRepositoryRoot();
        _siteRoot = Path.Combine(repositoryRoot, "Samples", "MainSample.WebAssembly", "wwwroot");
        _bundleRoot = Path.Combine(_siteRoot, "wc");
        _foreignRoot = Path.Combine(Path.GetTempPath(), "mudex-webcomponents-foreign-origin");
    }

    public async Task InitializeAsync()
    {
        Directory.Exists(_bundleRoot).Should().BeTrue(
            $"the web component bundle must be published first: dotnet publish Samples/MudEx.WebComponents -c Debug (expected at {_bundleRoot})");

        WriteForeignOriginPage();

        _site = await StartStaticServerAsync(_siteRoot, SitePort, allowCrossOrigin: false);
        _bundle = await StartStaticServerAsync(_bundleRoot, BundlePort, allowCrossOrigin: true);
        _foreign = await StartStaticServerAsync(_foreignRoot, ForeignPort, allowCrossOrigin: false);
    }

    public async Task DisposeAsync()
    {
        foreach (var app in new[] { _site, _bundle, _foreign }.Where(a => a is not null))
            await app!.DisposeAsync();
    }

    [Fact]
    public async Task DemoPageRendersTheFileDisplayAndReactsToAttributeChanges()
    {
        await Run($"http://localhost:{SitePort}/webcomponents.html", async page =>
        {
            var errors = CaptureConsoleErrors(page);

            await page.WaitForSelectorAsync("#boot-state:has-text('components registered')",
                new() { Timeout = 120000 });

            var display = page.Locator("mudex-file-display");
            await display.Locator(".mud-ex-file-display-container").WaitForAsync(new() { Timeout = 60000 });
            (await display.Locator(".mud-ex-file-display-toolbar").CountAsync()).Should().Be(1);
            // the toolbar menu only renders when file-name is set - it silently disappears otherwise
            (await display.Locator(".mud-ex-file-display-toolbar button").CountAsync()).Should().BeGreaterThan(0);

            // switching the attribute must pick another renderer and show the real content
            await page.ClickAsync("#btn-markdown");
            await page.WaitForSelectorAsync("mudex-file-display:has-text('MudExFileDisplay')",
                new() { Timeout = 60000 });


            (await page.EvaluateAsync<int>("window.MudEx.tags.length")).Should().BeGreaterThan(50);

            errors.Should().BeEmpty();
        });
    }

    [Fact]
    public async Task BundleWorksWhenEmbeddedFromAForeignOrigin()
    {
        await Run($"http://localhost:{ForeignPort}/index.html", async page =>
        {
            var errors = CaptureConsoleErrors(page);

            await page.WaitForSelectorAsync("mudex-file-display .mud-ex-file-display-container",
                new() { Timeout = 120000 });

            var loadedFrom = await page.EvaluateAsync<string>("window.MudEx.base");
            loadedFrom.Should().Contain($"localhost:{BundlePort}");

            errors.Should().BeEmpty();
        });
    }

    /// <summary>
    /// The regressions that actually break the bundle: a component script resolved against the wrong
    /// origin, or the runtime not starting at all. Unrelated noise of the demo page is ignored.
    /// </summary>
    private static ConcurrentBag<string> CaptureConsoleErrors(IPage page)
    {
        var errors = new ConcurrentBag<string>();
        page.Console += (_, message) =>
        {
            if (message.Type != "error")
                return;
            if (message.Text.Contains("lib.module.js")
                || message.Text.Contains("Failed to start platform")
                || message.Text.Contains("MudEx is not defined")
                || message.Text.Contains("ERR_CONTENT_DECODING_FAILED"))
                errors.Add(message.Text);
        };
        return errors;
    }

    private Task Run(string url, Func<IPage, Task> test)
        => _playwrightFixture.GotoPageAsync(url, test, Browser.Chromium);

    private void WriteForeignOriginPage()
    {
        Directory.CreateDirectory(_foreignRoot);
        File.WriteAllText(Path.Combine(_foreignRoot, "index.html"),
            $$"""
              <!DOCTYPE html>
              <html lang="en">
              <head><meta charset="utf-8" /><title>foreign origin</title></head>
              <body>
                  <h1>A page that only embeds the script</h1>
                  <mudex-file-display id="display"
                                      url="http://localhost:{{BundlePort}}/sample-data/sample.pdf"
                                      show-file-name="true"
                                      style="display:block;height:600px"></mudex-file-display>
                  <script src="http://localhost:{{BundlePort}}/mudex.js"></script>
              </body>
              </html>
              """);
    }

    private static async Task<WebApplication> StartStaticServerAsync(string root, int port, bool allowCrossOrigin)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls($"http://localhost:{port}");

        var app = builder.Build();
        if (allowCrossOrigin)
        {
            app.Use(async (context, next) =>
            {
                context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                await next();
            });
        }

        var contentTypes = new FileExtensionContentTypeProvider();
        contentTypes.Mappings[".wasm"] = "application/wasm";
        contentTypes.Mappings[".dat"] = "application/octet-stream";
        contentTypes.Mappings[".blat"] = "application/octet-stream";
        contentTypes.Mappings[".dll"] = "application/octet-stream";
        contentTypes.Mappings[".pdb"] = "application/octet-stream";

        app.UseFileServer(new FileServerOptions
        {
            FileProvider = new PhysicalFileProvider(root),
            RequestPath = string.Empty,
            EnableDefaultFiles = true,
            StaticFileOptions =
            {
                ContentTypeProvider = contentTypes,
                ServeUnknownFileTypes = true,
                DefaultContentType = "application/octet-stream"
            }
        });

        await app.StartAsync();
        return app;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MudBlazor.Extensions.slnx")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the tests must run inside the repository");
        return directory!.FullName;
    }
}
