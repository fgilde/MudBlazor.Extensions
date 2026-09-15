using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core.FileManager;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Helper;

/// <summary>
/// Issue #155: every component's first render pushed the whole stylesheet through JS interop again,
/// which on Blazor Server meant megabytes over SignalR for a single select. The stylesheet may
/// travel once per JS runtime (= once per circuit), no matter how many components render.
/// </summary>
public class CssAutoLoadTests
{
    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    private static readonly List<MudExFileStructureNode> Items = new()
    {
        new() { Name = "a.txt", FullPath = "a.txt", Size = 1, ContentType = "text/plain" }
    };

    [Fact]
    public void Stylesheet_is_sent_once_per_js_runtime()
    {
        var context = CreateContext();

        context.Render<MudExFileGrid>(p => p.Add(c => c.Items, Items));
        context.Render<MudExFileGrid>(p => p.Add(c => c.Items, Items));
        context.Render<MudExFileGrid>(p => p.Add(c => c.Items, Items));

        var cssCalls = context.JSInterop.Invocations.Count(i => i.Identifier == "BlazorJS.addCss");
        Assert.Equal(1, cssCalls);
    }

    [Fact]
    public void Stylesheet_is_sent_again_for_another_js_runtime()
    {
        var first = CreateContext();
        first.Render<MudExFileGrid>(p => p.Add(c => c.Items, Items));

        var second = CreateContext();
        second.Render<MudExFileGrid>(p => p.Add(c => c.Items, Items));

        Assert.Equal(1, second.JSInterop.Invocations.Count(i => i.Identifier == "BlazorJS.addCss"));
    }
}
