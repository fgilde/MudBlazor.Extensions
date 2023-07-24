# MudBlazor.Extensions.Tests

This is the documentation for the `MudBlazor.Extensions.Tests` project.

The tests are split into two categories:
- **UITests**: located in the `UITests` folder, these tests are focused on user interface behaviors.
- **UnitTests**: located in the `UnitTests` folder, these tests are focused on individual functional units of the code.

## UITests

The UITests make use of the Playwright tool for end-to-end browser testing. Each test should inherit from the `BaseUITest` to gain access to the `PlaywrightFixture`.

### Test Structure

A sample test is structured as follows:

```csharp
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
}
```

## UnitTests

The UnitTests should be added in the `UnitTests` folder. Each unit test is focused on testing the individual functional units of the code.

## xUnit

We use [xUnit](https://xunit.net/) as the test framework for both UnitTests and UITests.

## Record a Test

To record a test, navigate via the command line to the `MudBlazor.Extensions.Tests` folder and run the following command:

```pwsh
pwsh bin/Debug/net7.0/playwright.ps1 codegen https://www.mudex.org
```