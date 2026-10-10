using FluentAssertions;
using Microsoft.Playwright;

namespace MudBlazor.Extensions.Tests.UITests.Tests;

/// <summary>
/// Formats the browser cannot handle natively are decoded and encoded by MudExImageCodecs.js
/// (TIFF and GIF through lazily loaded libraries), so they can only be verified in a real browser.
/// </summary>
[Collection(PlaywrightFixture.PlaywrightCollection)]
public class ImageViewerTests : BaseUITest
{
    protected override Browser? TargetBrowser => Browser.Chromium;

    public ImageViewerTests(PlaywrightFixture playwrightFixture) : base(playwrightFixture)
    { }

    [Theory]
    [InlineData("sample.tga")]
    [InlineData("sample.qoi")]
    [InlineData("sample.pbm")]
    [InlineData("sample.pgm")]
    [InlineData("sample.ppm")]
    [InlineData("Header.tiff")]
    public async Task DecodesRasterFormatsInTheBrowser(string fileName)
    {
        await Test($"{Url}/file-display", async page =>
        {
            await page.Locator(".mud-ex-fd-list .mud-list-item").Filter(new() { HasText = fileName }).First.ClickAsync();
            var canvas = page.Locator(".mud-ex-fd-viewer canvas").First;
            var error = page.Locator(".mud-ex-fd-viewer .mud-alert-filled-error").First;
            await canvas.Or(error).WaitForAsync(new() { Timeout = 60000 });
            (await error.IsVisibleAsync()).Should().BeFalse(await error.IsVisibleAsync() ? await error.TextContentAsync() : null);
        });
    }

    [Fact]
    public async Task ExportsEveryFormat()
    {
        await Test($"{Url}/file-display", async page =>
        {
            // opening any image loads MudExImageViewer.js
            await page.Locator(".mud-ex-fd-list .mud-list-item").Filter(new() { HasText = "sample.qoi" }).First.ClickAsync();
            await page.Locator(".mud-ex-fd-viewer canvas").First.WaitForAsync(new() { Timeout = 60000 });
            var results = await page.EvaluateAsync<string[]>("""
                async () => {
                    const viewer = Object.create(MudExImageViewer.prototype);
                    const results = [];
                    for (const format of ['png', 'jpeg', 'webp', 'bmp', 'gif', 'tiff', 'tga', 'qoi', 'pbm']) {
                        const url = await viewer.exportImage('/sample-data/logo.png', format);
                        const bytes = new Uint8Array(await (await fetch(url)).arrayBuffer());
                        results.push(format + ':' + bytes.length + ':' + Array.from(bytes.slice(0, 4)).join(','));
                    }
                    return results;
                }
                """);

            results.Should().HaveCount(9);
            results.Should().Contain(r => r.StartsWith("png:") && r.EndsWith(":137,80,78,71"));
            results.Should().Contain(r => r.StartsWith("jpeg:") && r.EndsWith(":255,216,255,224"));
            results.Should().Contain(r => r.StartsWith("webp:") && r.EndsWith(":82,73,70,70"));
            results.Should().Contain(r => r.StartsWith("bmp:") && r.Contains(":66,77,"));
            results.Should().Contain(r => r.StartsWith("gif:") && r.EndsWith(":71,73,70,56"));
            results.Should().Contain(r => r.StartsWith("tiff:") && (r.EndsWith(":73,73,42,0") || r.EndsWith(":77,77,0,42")));
            results.Should().Contain(r => r.StartsWith("tga:") && r.EndsWith(":0,0,2,0"));
            results.Should().Contain(r => r.StartsWith("qoi:") && r.EndsWith(":113,111,105,102"));
            results.Should().Contain(r => r.StartsWith("pbm:") && r.Contains(":80,54,10,"));
        });
    }
}
