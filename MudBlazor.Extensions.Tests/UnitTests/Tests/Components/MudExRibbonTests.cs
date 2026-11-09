using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class MudExRibbonTests : IAsyncLifetime
{
    private readonly List<BunitContext> _contexts = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var context in _contexts)
            await context.DisposeAsync();
    }

    private BunitContext NewContext()
    {
        var context = new BunitContext();
        _contexts.Add(context);
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    private IRenderedComponent<MudExRibbon> Render(Action<ComponentParameterCollectionBuilder<MudExRibbon>> configure)
        => NewContext().Render(configure);

    [Fact]
    public void Classic_RendersTabsGroupsAndItemsInDeclarationOrder()
    {
        var cut = Render(p => p
            .AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Home")
                .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Clipboard")
                    .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Paste").Add(x => x.Size, MudExRibbonItemSize.Large))
                    .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Cut").Add(x => x.Size, MudExRibbonItemSize.Small))
                    .AddChildContent<MudExRibbonSeparator>()
                    .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Copy")))
                .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Font")
                    .AddChildContent<MudExRibbonRow>(r => r
                        .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Bold").Add(x => x.Size, MudExRibbonItemSize.Small))
                        .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Italic").Add(x => x.Size, MudExRibbonItemSize.Small)))
                    .AddChildContent<MudExRibbonContent>(c => c.AddChildContent("<span class=\"custom\">Size</span>"))))
            .AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Insert")
                .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Tables")
                    .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Table")))));

        cut.WaitForAssertion(() =>
            Assert.Equal(["Home", "Insert"], cut.FindAll("[role=tab]").Select(e => e.TextContent.Trim())));
        Assert.Equal(["Clipboard", "Font"], cut.FindAll("[role=toolbar]").Select(e => e.GetAttribute("aria-label")));
        Assert.Equal(["Paste", "Cut", "Copy", "Bold", "Italic"],
            cut.FindAll(".mud-ex-ribbon-button").Select(e => e.GetAttribute("aria-label")));
        Assert.Single(cut.FindAll(".mud-ex-ribbon-separator"));
        Assert.Single(cut.FindAll(".mud-ex-ribbon-row .mud-ex-ribbon-button.mud-ex-ribbon-small"), e => e.GetAttribute("aria-label") == "Bold");
        Assert.Single(cut.FindAll(".custom"));
        Assert.DoesNotContain(cut.FindAll(".mud-ex-ribbon-button"), e => e.GetAttribute("aria-label") == "Table");

        var font = cut.Instance.Tabs[0].Groups[1];
        Assert.Equal([typeof(MudExRibbonRow), typeof(MudExRibbonContent)], font.Items.Select(i => i.GetType()));

        cut.FindAll("[role=tab]")[1].Click();
        cut.WaitForAssertion(() => Assert.Equal(["Tables"], cut.FindAll("[role=toolbar]").Select(e => e.GetAttribute("aria-label"))));
        Assert.Equal(1, cut.Instance.ActiveTabIndex);
    }

    [Fact]
    public void Button_Click_InvokesOnClick()
    {
        var clicks = 0;
        var cut = Render(p => p.AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Home")
            .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "G")
                .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Save").Add(x => x.OnClick, () => clicks++)))));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-button")));
        cut.Find(".mud-ex-ribbon-button").Click();

        cut.WaitForAssertion(() => Assert.Equal(1, clicks));
    }

    [Fact]
    public async Task DisabledButton_DoesNotInvoke()
    {
        var clicks = 0;
        var cut = Render(p => p.AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Home")
            .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "G")
                .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Save").Add(x => x.Disabled, true).Add(x => x.OnClick, () => clicks++)))));

        cut.WaitForAssertion(() => Assert.True(cut.Find(".mud-ex-ribbon-button").HasAttribute("disabled")));
        await cut.InvokeAsync(() => cut.FindComponent<MudExRibbonButton>().Instance.ExecuteAsync());

        Assert.Equal(0, clicks);
    }

    [Fact]
    public void ToggleButton_ReflectsChecked_AndRaisesCheckedChanged()
    {
        bool? raised = null;
        var cut = Render(p => p.AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Home")
            .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Font")
                .AddChildContent<MudExRibbonToggleButton>(b => b.Add(x => x.Text, "Bold").Add(x => x.Checked, true))
                .AddChildContent<MudExRibbonToggleButton>(b => b.Add(x => x.Text, "Italic").Add(x => x.CheckedChanged, v => raised = v)))));

        cut.WaitForAssertion(() => Assert.Equal(["true", "false"], cut.FindAll(".mud-ex-ribbon-button").Select(e => e.GetAttribute("aria-pressed"))));

        cut.FindAll(".mud-ex-ribbon-button")[1].Click();

        cut.WaitForAssertion(() => Assert.True(raised));
        cut.WaitForAssertion(() => Assert.Equal("true", cut.FindAll(".mud-ex-ribbon-button")[1].GetAttribute("aria-pressed")));
    }

    [Fact]
    public void InvisibleTab_IsNotRendered()
    {
        var cut = Render(p => p
            .Add(x => x.ActiveTabIndex, 0)
            .AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Hidden").Add(x => x.Visible, false)
                .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Secret")))
            .AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Home")
                .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Clipboard"))));

        cut.WaitForAssertion(() => Assert.Equal(["Home"], cut.FindAll("[role=tab]").Select(e => e.TextContent.Trim())));
        Assert.Equal(["Clipboard"], cut.FindAll("[role=toolbar]").Select(e => e.GetAttribute("aria-label")));
        Assert.Equal(2, cut.Instance.Tabs.Count);
    }

    [Fact]
    public void Tooltip_ContainsShortcutInBrackets()
    {
        var context = NewContext();
        var popovers = context.Render<MudPopoverProvider>();
        var cut = context.Render<MudExRibbon>(p => p.AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Home")
            .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Font")
                .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Bold").Add(x => x.Shortcut, "Ctrl+B")
                    .Add(x => x.Description, "Make the selected text bold.")))));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-tooltip-root")));
        cut.Find(".mud-tooltip-root").PointerEnter();

        popovers.WaitForAssertion(() =>
        {
            Assert.Contains("Bold (Ctrl+B)", popovers.Markup);
            Assert.Contains("Make the selected text bold.", popovers.Markup);
        });
    }

    [Fact]
    public void HeaderColor_UsesPaletteVariables_ForEnumColor()
    {
        var cut = Render(p => p.Add(x => x.Color, Color.Success));

        var style = cut.Find(".mud-ex-ribbon-header").GetAttribute("style");
        Assert.Contains("var(--mud-palette-success)", style);
        Assert.Contains("var(--mud-palette-success-text)", style);
    }

    [Fact]
    public void HeaderColor_DarkCustomColor_UsesWhiteForeground()
    {
        var cut = Render(p => p.Add(x => x.Color, (MudExColor)"#217346"));

        var style = cut.Find(".mud-ex-ribbon-header").GetAttribute("style");
        Assert.Contains("#217346", style);
        Assert.Contains("color:#fff", style);
        Assert.Equal("rgba(0,0,0,.87)", MudExRibbonColors.ForegroundFor("#ffeb3b"));
    }

    [Fact]
    public async Task ItemParameterChangeOnEveryParentRender_DoesNotLoop()
    {
        var cut = NewContext().Render<RibbonHost>();
        var ribbon = cut.FindComponent<CountingRibbon>().Instance;
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-button")));
        var before = ribbon.Renders;

        for (var i = 0; i < 5; i++)
            await cut.InvokeAsync(cut.Instance.Rerender);

        cut.WaitForAssertion(() => Assert.Equal(6, cut.Instance.Renders));
        Assert.InRange(ribbon.Renders - before, 1, 6);
        cut.Find(".mud-ex-ribbon-button").Click();
        cut.WaitForAssertion(() => Assert.Equal(1, cut.Instance.Clicks));
    }

    private sealed class CountingRibbon : MudExRibbon
    {
        public int Renders { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Renders++;
            base.BuildRenderTree(builder);
        }
    }

    private sealed class RibbonHost : ComponentBase
    {
        public int Renders { get; private set; }
        public int Clicks { get; private set; }

        public void Rerender() => StateHasChanged();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Renders++;
            builder.OpenComponent<CountingRibbon>(0);
            builder.AddAttribute(1, nameof(MudExRibbon.ChildContent), (RenderFragment)(tab =>
            {
                tab.OpenComponent<MudExRibbonTab>(0);
                tab.AddAttribute(1, nameof(MudExRibbonTab.Text), "Home");
                tab.AddAttribute(2, nameof(MudExRibbonTab.ChildContent), (RenderFragment)(group =>
                {
                    group.OpenComponent<MudExRibbonGroup>(0);
                    group.AddAttribute(1, nameof(MudExRibbonGroup.Text), "Font");
                    group.AddAttribute(2, nameof(MudExRibbonGroup.ChildContent), (RenderFragment)(item =>
                    {
                        var step = 1;
                        item.OpenComponent<MudExRibbonButton>(0);
                        item.AddAttribute(1, nameof(MudExRibbonButton.Text), string.Concat("Bo", "ld"));
                        item.AddAttribute(2, nameof(MudExRibbonButton.OnClick), EventCallback.Factory.Create(this, () => Clicks += step));
                        item.CloseComponent();
                    }));
                    group.CloseComponent();
                }));
                tab.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
