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
            await page.GetByRole(AriaRole.Link, new() { Name = "API" }).ClickAsync();
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
            var version = MudExResource.MudExVersion().ToString();
            await page.GetByRole(AriaRole.Banner).GetByRole(AriaRole.Button).Nth(2).ClickAsync();

            var head = await page.GetByRole(AriaRole.Heading, new() { Name = $"MudBlazor.Extensions {version}" }).TextContentAsync();
            head.Should().Contain($"MudBlazor.Extensions {version}");

            await page.GetByRole(AriaRole.Button, new() { Name = "Close", Exact = true }).ClickAsync();
        });

    }
    
}