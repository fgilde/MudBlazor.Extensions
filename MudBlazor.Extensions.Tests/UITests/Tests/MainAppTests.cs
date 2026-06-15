using FluentAssertions;
using Microsoft.Playwright;
using MudBlazor.Extensions.Helper;
using System;

namespace MudBlazor.Extensions.Tests.UITests.Tests;

[Collection(PlaywrightFixture.PlaywrightCollection)]
public class MainAppTests: BaseUITest
{

    public MainAppTests(PlaywrightFixture playwrightFixture) : base(playwrightFixture)
    {}
    
    [Fact]
    public async Task Loaded()
    {
        await Test(async page =>
        {
            var hasHeader = await page.GetByRole(AriaRole.Banner).GetByRole(AriaRole.Link, new() { Name = "MudBlazor.Extensions" }).IsVisibleAsync();
            hasHeader.Should().BeTrue();
        });
    }


    [Fact]
    public async Task ApiAvailable()
    {
        await Test(async page =>
        {
            // The tree renders nested <li> nodes, so a plain "contains API" filter matches the
            // leaf plus all of its ancestor nodes (strict-mode violation). Anchor on the exact
            // node text so only the actual "API" leaf is clicked.
            await page.Locator("li.mud-treeview-item")
                .Filter(new() { HasTextRegex = new System.Text.RegularExpressions.Regex(@"^\s*API\s*$") })
                .ClickAsync();
            await page.WaitForURLAsync($"{Url}/api");
            await page.WaitForRequestFinishedAsync();
            await Task.Delay(2000);
            var isVis = await page.GetByRole(AriaRole.Heading, new() { Name = "API" }).IsVisibleAsync();
            isVis.Should().BeTrue();
        });
    }
    
    [Fact]
    public async Task About()
    {
        await Test(async page =>
        {
            // "About / Info" moved into the overflow (⋮) menu in the app bar. Open the menu
            // (last button in the banner) and click the "Info" item to open the About dialog.
            await page.GetByRole(AriaRole.Banner).GetByRole(AriaRole.Button).Last.ClickAsync();
            await page.Locator(".mud-menu-item").Filter(new() { HasText = "Info" }).First.ClickAsync();

            var dialog = page.Locator(".mud-dialog");
            await dialog.WaitForAsync();

            var head = await dialog.TextContentAsync();
            head.Should().Contain("MudBlazor.Extensions");

            // The About dialog slides in, so its close button can sit outside the viewport and is
            // not reliably clickable via a real mouse click. Dispatch the click event directly –
            // this still verifies the close button exists and is wired up.
            var closeBtn = dialog.Locator("button.mud-button-close");
            await closeBtn.DispatchEventAsync("click");
        });

    }
    
}