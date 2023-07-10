using MainSample.WebAssembly;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MudBlazor.Extensions.Tests.UITests.Tests;

/// <summary>
/// The test class that is using the PlaywrightFixture
/// </summary>
[Collection(PlaywrightFixture.PlaywrightCollection)]
public class BaseUITest : IClassFixture<WebApplicationFactory<AssemblyClassLocator>>
{
    private readonly PlaywrightFixture playwrightFixture;
    private readonly WebApplicationFactory<AssemblyClassLocator> factory;
    protected const string url = "https://localhost:5001";

    /// <summary>
    /// Setup test class injecting a playwrightFixture instance.
    /// </summary>
    /// <param name="playwrightFixture">The playwrightFixture
    /// instance.</param>
    public BaseUITest(PlaywrightFixture playwrightFixture, WebApplicationFactory<AssemblyClassLocator> factory)
    {
        this.playwrightFixture = playwrightFixture;
        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseUrls(url)
            .ConfigureServices(services =>
            {
            })
            .ConfigureAppConfiguration((app, conf) =>
            {
            });
        });
    }

    [Fact]
    public async Task MyFirstTest()
    {
     //   var url = "https://www.mudex.org";
        // Open a page and run test logic.
        await this.playwrightFixture.GotoPageAsync(
          url,
          async (page) =>
          {
              // Apply the test logic on the given page.

              // Click text=Home
              await page.Locator("text=Home").ClickAsync();
              await page.WaitForURLAsync($"{url}/");
              // Click text=Hello, world!
              await page.Locator("text=Hello, world!").IsVisibleAsync();

              // Click text=Counter
              await page.Locator("text=Counter").ClickAsync();
              await page.WaitForURLAsync($"{url}/counter");
              // Click h1:has-text("Counter")
              await page.Locator("h1:has-text(\"Counter\")").IsVisibleAsync();
              // Click text=Click me
              await page.Locator("text=Click me").ClickAsync();
              // Click text=Current count: 1
              await page.Locator("text=Current count: 1").IsVisibleAsync();
              // Click text=Click me
              await page.Locator("text=Click me").ClickAsync();
              // Click text=Current count: 2
              await page.Locator("text=Current count: 2").IsVisibleAsync();

          },
          Browser.Chromium);
    }
}