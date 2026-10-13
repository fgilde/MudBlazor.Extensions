using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Core;
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

        Assert.Null(cut.Find(".mud-ex-cron-edit-time").GetAttribute("style"));
        Assert.DoesNotContain("max-width", cut.Find(".mud-ex-cron-edit-time .mud-picker").GetAttribute("style") ?? string.Empty);
    }

    [Fact]
    public async Task AnEmptyValueIsNoScheduleAndShowsNoError()
    {
        await using var context = CreateContext();
        var cut = context.Render<MudExCronEdit>(p => p.Add(c => c.Value, (string)null));

        Assert.Empty(cut.FindAll(".mud-input-error"));
        Assert.DoesNotContain("Custom schedule", cut.Markup);
    }

    [Fact]
    public async Task ClearingTheTextFieldEmitsNullAndShowsNoError()
    {
        await using var context = CreateContext();
        var emitted = "unset";
        var cut = context.Render<MudExCronEdit>(p => p
            .Add(c => c.Value, "0 6 * * *")
            .Add(c => c.ValueChanged, v => emitted = v));

        await cut.InvokeAsync(() => cut.FindAll("input").Last().Change("  "));

        Assert.Null(emitted);
        Assert.Empty(cut.FindAll(".mud-input-error"));
    }

    [Fact]
    public async Task TimeIsUtcFalseTakesUtcOutOfTheLabelAndTheDescription()
    {
        await using var context = CreateContext();
        var utc = context.Render<MudExCronEdit>(p => p.Add(c => c.Value, "0 6 * * *"));
        var local = context.Render<MudExCronEdit>(p => p.Add(c => c.Value, "0 6 * * *").Add(c => c.TimeIsUtc, false));

        Assert.Contains("UTC", utc.Markup);
        Assert.DoesNotContain("UTC", local.Markup);
        Assert.Contains("06:00", local.Markup);
    }

    [Fact]
    public async Task SelectingAWeekdayEmitsTheExpressionAndTheLastWeekdayCannotBeDeselected()
    {
        await using var context = CreateContext();
        var value = "0 6 * * 1";
        var emitted = new List<string>();
        var cut = context.Render<MudExCronEdit>(p => p
            .Add(c => c.Value, value)
            .Add(c => c.ValueChanged, v => { value = v; emitted.Add(v); }));
        var tuesday = System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.GetAbbreviatedDayName(DayOfWeek.Tuesday);
        var monday = System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.GetAbbreviatedDayName(DayOfWeek.Monday);
        IElement Chip(string text) => cut.FindAll(".mud-chip").Single(c => c.TextContent.Trim() == text);

        await cut.InvokeAsync(() => Chip(tuesday).Click());
        Assert.Equal(["0 6 * * 1,2"], emitted);

        await cut.InvokeAsync(() => Chip(monday).Click());
        Assert.Equal(["0 6 * * 1,2", "0 6 * * 2"], emitted);

        await cut.InvokeAsync(() => Chip(tuesday).Click());
        Assert.Equal(2, emitted.Count);
        Assert.Equal("0 6 * * 2", ExpressionField(cut));
        cut.WaitForAssertion(() => Assert.Equal(tuesday, Assert.Single(cut.FindAll(".mud-chip-selected")).TextContent.Trim()));
    }

    [Fact]
    public async Task ChangingTheFrequencyEmitsTheMatchingExpressionAndCustomEmitsNothing()
    {
        await using var context = CreateContext();
        var emitted = new List<string>();
        var cut = context.Render<MudExCronEdit>(p => p
            .Add(c => c.Value, "30 6 * * *")
            .Add(c => c.ValueChanged, v => emitted.Add(v)));
        var select = cut.FindComponent<MudSelect<MudExCronFrequency>>();

        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(MudExCronFrequency.Hourly));
        Assert.Equal(["30 * * * *"], emitted);

        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(MudExCronFrequency.Custom));
        Assert.Equal(["30 * * * *"], emitted);
        Assert.Equal("30 * * * *", ExpressionField(cut));
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
