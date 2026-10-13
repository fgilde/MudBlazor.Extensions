using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class MudExCronEditTests
{
    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    private static string ExpressionField(IRenderedComponent<MudExCronEdit> cut) => cut.FindAll("input").Last().GetAttribute("value");

    [Fact]
    public async Task ShowsTheBoundExpressionAndSaysInWordsWhenItRuns()
    {
        await using var context = CreateContext();
        var cut = context.Render<MudExCronEdit>(p => p.Add(c => c.Value, "0 6 * * 1,3"));

        Assert.Equal("0 6 * * 1,3", ExpressionField(cut));
        Assert.Contains("06:00 UTC", cut.Markup);
        Assert.Equal(2, cut.FindAll(".mud-chip-selected").Count);
    }

    [Fact]
    public async Task KeepsAnExpressionTheFrequencyUiCannotShowAsCustomAndUnchanged()
    {
        await using var context = CreateContext();
        string emitted = null;
        var cut = context.Render<MudExCronEdit>(p => p
            .Add(c => c.Value, "*/7 3-5 * * 1")
            .Add(c => c.ValueChanged, v => emitted = v));

        Assert.Equal("*/7 3-5 * * 1", ExpressionField(cut));
        Assert.Contains("Custom", cut.Find(".mud-select input").GetAttribute("value"));
        Assert.Empty(cut.FindAll(".mud-chip"));
        Assert.Null(emitted);
    }

    [Fact]
    public async Task AnInvalidTypedExpressionShowsAnErrorAndDoesNotOverwriteTheValue()
    {
        await using var context = CreateContext();
        var value = "0 6 * * *";
        var changes = 0;
        var cut = context.Render<MudExCronEdit>(p => p
            .Add(c => c.Value, value)
            .Add(c => c.ValueChanged, v => { value = v; changes++; }));

        await cut.InvokeAsync(() => cut.FindAll("input").Last().Change("every day"));

        Assert.Equal(0, changes);
        Assert.Equal("0 6 * * *", value);
        Assert.Equal("every day", ExpressionField(cut));
        Assert.NotEmpty(cut.FindAll(".mud-input-error"));
    }

    [Fact]
    public async Task AValidTypedExpressionIsEmitted()
    {
        await using var context = CreateContext();
        string emitted = null;
        var cut = context.Render<MudExCronEdit>(p => p
            .Add(c => c.Value, "0 6 * * *")
            .Add(c => c.ValueChanged, v => emitted = v));

        await cut.InvokeAsync(() => cut.FindAll("input").Last().Change("*/5 * * * *"));

        Assert.Equal("*/5 * * * *", emitted);
    }

    [Fact]
    public async Task TheTimePickerGetsNoStyleThatWouldBePassedOnToItsPopover()
    {
        await using var context = CreateContext();
        var cut = context.Render<MudExCronEdit>(p => p.Add(c => c.Value, "0 6 * * *"));

        Assert.Contains("max-width", cut.Find(".mud-ex-cron-time").GetAttribute("style"));
        Assert.DoesNotContain("max-width", cut.Find(".mud-ex-cron-time .mud-picker").GetAttribute("style") ?? string.Empty);
    }

    [Fact]
    public async Task DisabledDisablesTheInputs()
    {
        await using var context = CreateContext();
        var cut = context.Render<MudExCronEdit>(p => p.Add(c => c.Value, "0 6 * * *").Add(c => c.Disabled, true));

        Assert.All(cut.FindAll("input"), i => Assert.True(i.HasAttribute("disabled")));
    }

    public class Job
    {
        public string Schedule { get; set; } = "30 * * * *";
    }

    [Fact]
    public async Task RenderWithEditsAStringPropertyWithTheEditor()
    {
        await using var context = CreateContext();
        var cut = context.Render<MudExObjectEdit<Job>>(p => p
            .Add(c => c.Value, new Job())
            .Add(c => c.MetaConfiguration, meta => meta.Property(m => m.Schedule).RenderWith<MudExCronEdit, string>(e => e.Value)));

        cut.WaitForAssertion(() => Assert.Contains("Every hour at minute 30", cut.Markup));
    }
}