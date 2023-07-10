using MainSample.ServerSide;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Playwright;

namespace MudBlazor.Extensions.Tests.UITests.Tests;

/// <summary>
/// The test class that is using the PlaywrightFixture
/// </summary>
[Collection(PlaywrightFixture.PlaywrightCollection)]
public class BaseUITest
{
    // null means all
    protected virtual Browser? TargetBrowser => null;

    protected readonly PlaywrightFixture playwrightFixture;
    protected WebTestingHostFactory<ServerApplicationClassLocator> factory;
    protected const string Url = "http://localhost:5000";


    public BaseUITest(PlaywrightFixture playwrightFixture)
    {
        this.playwrightFixture = playwrightFixture;
        StartApp();
    }

    protected void StartApp()
    {
        factory = new WebTestingHostFactory<ServerApplicationClassLocator>();
        factory.WithWebHostBuilder(builder =>
        {
            // Setup the url to use.
            builder.UseUrls(Url);
            // Replace or add services if needed.
            builder.ConfigureServices(services =>
            {
                // services.AddTransient<....>();
            });
            // Replace or add configuration if needed.
            builder.ConfigureAppConfiguration((app, conf) =>
            {
                // conf.AddJsonFile("appsettings.Test.json");
            });
        })
        // Create the host using the CreateDefaultClient method.
        .CreateDefaultClient();
    }

    protected async Task Test(Func<IPage, Task> testHandler) => await ExecuteTestAsync(Url, testHandler);
    protected async Task Test(Func<IPage, Task> testHandler, Browser? targetBrowser) => await ExecuteTestAsync(Url, testHandler, targetBrowser);
    protected async Task Test(string url, Func<IPage, Task> testHandler) => await ExecuteTestAsync(url, testHandler);
    protected async Task Test(string url, Func<IPage, Task> testHandler, Browser? targetBrowser) => await ExecuteTestAsync(url, testHandler, targetBrowser);

    private Task ExecuteTestAsync(string url, Func<IPage, Task> testHandler, Browser? targetBrowser = null)
    {
        targetBrowser ??= TargetBrowser;
        return targetBrowser.HasValue 
            // If TargetBrowser has a value, only run for that browser.
            ? playwrightFixture.GotoPageAsync(url, testHandler, targetBrowser.Value) 
            : Task.WhenAll(Enum.GetValues(typeof(Browser))
                .Cast<Browser>()
                .Select(browser => playwrightFixture.GotoPageAsync(url, testHandler, browser)).ToList());
    }

}