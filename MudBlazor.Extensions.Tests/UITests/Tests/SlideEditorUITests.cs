using FluentAssertions;
using Microsoft.Playwright;

namespace MudBlazor.Extensions.Tests.UITests.Tests;

/// <summary>
/// Keyboard and pointer behaviour of the slide editor that runs in its JS modules: double-click editing, Ctrl+S, leaving the canvas with Tab
/// and typing right after selecting a shape.
/// </summary>
[Collection(PlaywrightFixture.PlaywrightCollection)]
public class SlideEditorUITests : BaseUITest
{
    protected override Browser? TargetBrowser => Browser.Chromium;

    private static readonly SemaphoreSlim WarmUp = new(1, 1);
    private static bool _warm;

    public SlideEditorUITests(PlaywrightFixture playwrightFixture) : base(playwrightFixture)
    { }

    private static async Task<bool> WarmUpAsync(IPage page)
    {
        await WarmUp.WaitAsync();
        try
        {
            if (_warm)
                return false;
            for (var attempt = 1; !_warm && attempt <= 5; attempt++)
            {
                var probe = await page.Context.NewPageAsync();
                try
                {
                    await probe.GotoAsync(page.Url, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120000 });
                    await probe.Locator("button").Filter(new() { HasText = "Show demo" }).First.ClickAsync(new() { Timeout = 120000 });
                    await probe.Locator(".mud-ex-slide-canvas[data-readonly='false'] [data-shape-id='3']").First.WaitForAsync(new() { Timeout = 60000 });
                    _warm = true;
                }
                catch (TimeoutException)
                {
                }
                finally
                {
                    await probe.CloseAsync();
                }
            }

            return true;
        }
        finally
        {
            WarmUp.Release();
        }
    }

    private static async Task<ILocator> OpenAsync(IPage page)
    {
        if (await WarmUpAsync(page))
            await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120000 });
        await page.Locator("button").Filter(new() { HasText = "Show demo" }).First.ClickAsync(new() { Timeout = 120000 });
        var canvas = page.Locator(".mud-ex-slide-canvas[data-readonly='false']").First;
        await canvas.Locator("[data-shape-id='3']").WaitForAsync(new() { Timeout = 120000 });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return canvas;
    }

    private static async Task SelectAsync(ILocator canvas, string id)
    {
        await canvas.Locator($"[data-shape-id='{id}']").ClickAsync();
        await canvas.Locator($".mud-ex-slide-selection[data-id='{id}']").WaitForAsync();
    }

    [Fact]
    public async Task DoubleClick_StartsTextEditing()
    {
        await Test($"{Url}/slides", async page =>
        {
            var canvas = await OpenAsync(page);

            await canvas.Locator("[data-shape-id='3']").DblClickAsync();

            await canvas.Locator(".mud-ex-slide-text-editable").WaitForAsync(new() { Timeout = 10000 });
        });
    }

    [Fact]
    public async Task CtrlS_DoesNotOpenTheBrowserSaveDialog()
    {
        await Test($"{Url}/slides", async page =>
        {
            var canvas = await OpenAsync(page);

            var prevented = await canvas.EvaluateAsync<bool>("""
                c => {
                    const e = new KeyboardEvent('keydown', { key: 's', ctrlKey: true, bubbles: true, cancelable: true });
                    c.dispatchEvent(e);
                    return e.defaultPrevented;
                }
                """);

            prevented.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Tab_LeavesTheCanvasAfterTheLastShape()
    {
        await Test($"{Url}/slides", async page =>
        {
            var canvas = await OpenAsync(page);
            var ids = await canvas.Locator("[data-shape-id]:not([data-group-id])").EvaluateAllAsync<string[]>("e => e.map(x => x.dataset.shapeId)");
            await canvas.FocusAsync();

            foreach (var id in ids)
            {
                await page.Keyboard.PressAsync("Tab");
                await page.WaitForFunctionAsync("([c, id]) => c.dataset.selection === id", new object[] { await canvas.ElementHandleAsync(), id });
            }

            await page.Keyboard.PressAsync("Tab");

            (await canvas.EvaluateAsync<bool>("c => document.activeElement === c")).Should().BeFalse();
            await page.WaitForFunctionAsync("c => c.dataset.selection === ''", await canvas.ElementHandleAsync());
        });
    }

    [Fact]
    public async Task Typing_RightAfterSelecting_KeepsEveryKey()
    {
        await Test($"{Url}/slides", async page =>
        {
            var canvas = await OpenAsync(page);
            await SelectAsync(canvas, "3");

            await page.Keyboard.TypeAsync("Hello World");

            var editable = canvas.Locator(".mud-ex-slide-text-editable");
            await editable.WaitForAsync(new() { Timeout = 10000 });
            await page.WaitForFunctionAsync("e => e.textContent.replace(/\\u200b/g, '') === 'Hello World'", await editable.ElementHandleAsync(),
                new() { Timeout = 10000 });
            await page.Keyboard.PressAsync("Escape");
            await canvas.Locator("[data-shape-id='3'] .mud-ex-slide-text").Filter(new() { HasText = "Hello World" }).WaitForAsync(new() { Timeout = 10000 });
        });
    }
}
