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
        var panel = cut.Find("[role=tabpanel]");
        var insert = cut.FindAll("[role=tab]")[1];
        Assert.Equal($"{cut.Instance.ElementId}-tab-1", insert.Id);
        Assert.Equal(panel.Id, insert.GetAttribute("aria-controls"));
        Assert.Equal(insert.Id, panel.GetAttribute("aria-labelledby"));
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

    [Fact]
    public async Task ItemWithUnstableStateKey_DoesNotLoop()
    {
        var cut = NewContext().Render<RibbonHost>(p => p.Add(x => x.UnstableText, true));
        var ribbon = cut.FindComponent<CountingRibbon>().Instance;
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-button")));
        var before = ribbon.Renders;

        for (var i = 0; i < 5; i++)
            await cut.InvokeAsync(cut.Instance.Rerender);

        cut.WaitForAssertion(() => Assert.Equal(6, cut.Instance.Renders));
        Assert.Equal(5, ribbon.Renders - before);
        Assert.Equal(cut.FindComponent<MudExRibbonButton>().Instance.Text, cut.Find(".mud-ex-ribbon-button").GetAttribute("aria-label"));
    }

    private (IRenderedComponent<MudExRibbon> Ribbon, IRenderedComponent<MudPopoverProvider> Popovers, BunitContext Context) RenderGroup(
        Action<ComponentParameterCollectionBuilder<MudExRibbonGroup>> configure)
    {
        var context = NewContext();
        var popovers = context.Render<MudPopoverProvider>();
        var cut = context.Render<MudExRibbon>(p => p.AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Home")
            .AddChildContent<MudExRibbonGroup>(g =>
            {
                g.Add(x => x.Text, "G");
                configure(g);
            })));
        return (cut, popovers, context);
    }

    [Fact]
    public void SplitButton_FaceRunsOnClick_ArrowOpensMenu()
    {
        var clicks = 0;
        var (cut, popovers, _) = RenderGroup(g => g
            .AddChildContent<MudExRibbonMenuButton>(m => m.Add(x => x.Text, "Paste").Add(x => x.Split, true).Add(x => x.OnClick, () => clicks++)
                .AddChildContent<MudExRibbonMenuItem>(i => i.Add(x => x.Text, "Paste Special")))
            .AddChildContent<MudExRibbonMenuButton>(m => m.Add(x => x.Text, "Styles")
                .AddChildContent<MudExRibbonMenuItem>(i => i.Add(x => x.Text, "Heading"))));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-split-face")));
        cut.Find(".mud-ex-ribbon-split-face").Click();

        cut.WaitForAssertion(() => Assert.Equal(1, clicks));
        Assert.DoesNotContain("Paste Special", popovers.Markup);

        cut.Find(".mud-ex-ribbon-split-arrow").Click();
        popovers.WaitForAssertion(() => Assert.Contains("Paste Special", popovers.Markup));
        Assert.Equal(1, clicks);

        cut.FindAll(".mud-ex-ribbon-menu .mud-ex-ribbon-button").Last().Click();
        popovers.WaitForAssertion(() => Assert.Contains("Heading", popovers.Markup));
    }

    [Fact]
    public void MenuItem_InvokesOnClick_AndClosesMenu()
    {
        var clicks = 0;
        var (cut, popovers, _) = RenderGroup(g => g
            .AddChildContent<MudExRibbonMenuButton>(m => m.Add(x => x.Text, "Insert")
                .AddChildContent<MudExRibbonMenuItem>(i => i.Add(x => x.Text, "Table").Add(x => x.OnClick, () => clicks++))
                .AddChildContent<MudExRibbonMenuItem>(i => i.Add(x => x.Text, "Shapes")
                    .AddChildContent<MudExRibbonMenuItem>(c => c.Add(x => x.Text, "Circle")))));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-menu .mud-ex-ribbon-button")));
        var menu = Assert.IsType<MudExRibbonMenuButton>(Assert.Single(cut.Instance.Tabs[0].Groups[0].Items));
        Assert.Equal(["Table", "Shapes"], menu.Items.Select(i => i.Text));
        Assert.Equal(["Circle"], ((MudExRibbonMenuItem)menu.Items[1]).Items.Select(i => i.Text));

        cut.Find(".mud-ex-ribbon-menu .mud-ex-ribbon-button").Click();
        popovers.WaitForAssertion(() => Assert.Contains(popovers.FindAll("[role=menuitem]"), e => e.TextContent.Contains("Table")));
        Assert.Equal("true", cut.Find(".mud-ex-ribbon-menu [aria-haspopup=menu]").GetAttribute("aria-expanded"));

        popovers.FindAll("[role=menuitem]").First(e => e.TextContent.Contains("Table")).Click();

        cut.WaitForAssertion(() => Assert.Equal(1, clicks));
        cut.WaitForAssertion(() => Assert.Equal("false", cut.Find(".mud-ex-ribbon-menu [aria-haspopup=menu]").GetAttribute("aria-expanded")));
    }

    private static readonly string[] Styles = ["Normal", "No Spacing", "Heading 1", "Heading 2", "Title", "Subtitle"];

    [Fact]
    public void Gallery_SelectingItem_RaisesSelectedItemChanged()
    {
        string? selected = null;
        var (cut, _, _) = RenderGroup(g => g
            .AddChildContent<MudExRibbonGallery<string>>(b => b.Add(x => x.Text, "Styles").Add(x => x.Items, Styles)
                .Add(x => x.SelectedItem, "Normal").Add(x => x.SelectedItemChanged, v => selected = v)));

        cut.WaitForAssertion(() => Assert.Equal(Styles.Take(4), cut.FindAll(".mud-ex-ribbon-gallery-strip .mud-ex-ribbon-gallery-item").Select(e => e.TextContent.Trim())));
        Assert.Equal("true", cut.Find(".mud-ex-ribbon-gallery-strip .mud-ex-ribbon-gallery-item").GetAttribute("aria-selected"));
        Assert.True(cut.Find(".mud-ex-ribbon-gallery-up").HasAttribute("disabled"));

        cut.FindAll(".mud-ex-ribbon-gallery-strip .mud-ex-ribbon-gallery-item")[2].Click();

        cut.WaitForAssertion(() => Assert.Equal("Heading 1", selected));
        cut.WaitForAssertion(() => Assert.Equal("true", cut.FindAll(".mud-ex-ribbon-gallery-strip .mud-ex-ribbon-gallery-item")[2].GetAttribute("aria-selected")));

        cut.Find(".mud-ex-ribbon-gallery-down").Click();
        cut.WaitForAssertion(() => Assert.Equal(Styles.Skip(4), cut.FindAll(".mud-ex-ribbon-gallery-strip .mud-ex-ribbon-gallery-item").Select(e => e.TextContent.Trim())));
        Assert.True(cut.Find(".mud-ex-ribbon-gallery-down").HasAttribute("disabled"));
    }

    [Fact]
    public void Gallery_MoreArrow_ShowsAllItems()
    {
        string? selected = null;
        var (cut, popovers, _) = RenderGroup(g => g
            .AddChildContent<MudExRibbonGallery<string>>(b => b.Add(x => x.Text, "Styles").Add(x => x.Items, Styles)
                .Add(x => x.ItemText, s => s.ToUpperInvariant()).Add(x => x.SelectedItemChanged, v => selected = v)));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-gallery-more")));
        cut.Find(".mud-ex-ribbon-gallery-more").Click();

        popovers.WaitForAssertion(() => Assert.Equal(Styles.Select(s => s.ToUpperInvariant()),
            popovers.FindAll(".mud-ex-ribbon-gallery-item").Select(e => e.TextContent.Trim())));

        popovers.FindAll(".mud-ex-ribbon-gallery-item")[5].Click();

        cut.WaitForAssertion(() => Assert.Equal("Subtitle", selected));
        cut.WaitForAssertion(() => Assert.Equal("false", cut.Find(".mud-ex-ribbon-gallery [aria-haspopup=menu]").GetAttribute("aria-expanded")));
        Assert.Contains(cut.FindAll(".mud-ex-ribbon-gallery-strip .mud-ex-ribbon-gallery-item"), e => e.TextContent.Trim() == "SUBTITLE");
    }

    [Fact]
    public void NumberBox_ClampsToMinMax_AndSteps()
    {
        var raised = new List<double>();
        var (cut, _, _) = RenderGroup(g => g
            .AddChildContent<MudExRibbonNumberBox>(b => b.Add(x => x.Text, "Before:").Add(x => x.Value, 2).Add(x => x.Min, 0).Add(x => x.Max, 10)
                .Add(x => x.Step, 2).Add(x => x.Unit, "pt").Add(x => x.ValueChanged, v => raised.Add(v))));

        cut.WaitForAssertion(() => Assert.Equal("2 pt", cut.Find(".mud-ex-ribbon-number input").GetAttribute("value")));

        cut.Find(".mud-ex-ribbon-number input").Change("12 pt");
        cut.WaitForAssertion(() => Assert.Equal("10 pt", cut.Find(".mud-ex-ribbon-number input").GetAttribute("value")));

        cut.Find(".mud-ex-ribbon-number input").Change("-5");
        cut.WaitForAssertion(() => Assert.Equal("0 pt", cut.Find(".mud-ex-ribbon-number input").GetAttribute("value")));

        cut.Find(".mud-ex-ribbon-number input").Change("3");
        cut.WaitForAssertion(() => Assert.Equal("4 pt", cut.Find(".mud-ex-ribbon-number input").GetAttribute("value")));

        cut.Find(".mud-ex-ribbon-spin-up").Click();
        cut.WaitForAssertion(() => Assert.Equal("6 pt", cut.Find(".mud-ex-ribbon-number input").GetAttribute("value")));

        cut.Find(".mud-ex-ribbon-spin-down").Click();
        cut.WaitForAssertion(() => Assert.Equal("4 pt", cut.Find(".mud-ex-ribbon-number input").GetAttribute("value")));
        cut.Find(".mud-ex-ribbon-spin-down").Click();
        cut.WaitForAssertion(() => Assert.Equal("2 pt", cut.Find(".mud-ex-ribbon-number input").GetAttribute("value")));

        Assert.Equal([10d, 0d, 4d, 6d, 4d, 2d], raised);
    }

    [Fact]
    public void Select_RaisesValueChanged()
    {
        string? font = null, size = null;
        var (cut, _, _) = RenderGroup(g => g
            .AddChildContent<MudExRibbonSelect<string>>(b => b.Add(x => x.Text, "Font").Add(x => x.Items, ["Arial", "Calibri", "Times"])
                .Add(x => x.Value, "Calibri").Add(x => x.ValueChanged, v => font = v))
            .AddChildContent<MudExRibbonSelect<string>>(b => b.Add(x => x.Text, "Size").Add(x => x.Items, ["10", "11", "12"])
                .Add(x => x.Editable, true).Add(x => x.ValueChanged, v => size = v)));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("select.mud-ex-ribbon-select")));
        Assert.True(cut.FindAll("select.mud-ex-ribbon-select option")[1].HasAttribute("selected"));

        cut.Find("select.mud-ex-ribbon-select").Change("2");
        cut.WaitForAssertion(() => Assert.Equal("Times", font));
        cut.WaitForAssertion(() => Assert.True(cut.FindAll("select.mud-ex-ribbon-select option")[2].HasAttribute("selected")));

        Assert.Equal(["10", "11", "12"], cut.FindAll("datalist option").Select(o => o.GetAttribute("value")));
        cut.Find("input.mud-ex-ribbon-select").Change("14");
        cut.WaitForAssertion(() => Assert.Equal("14", size));
    }

    [Fact]
    public async Task ColorPicker_FaceAppliesLastColor_PaletteSelectionRaisesValueChanged()
    {
        var raised = new List<string>();
        var (cut, popovers, context) = RenderGroup(g => g
            .AddChildContent<MudExRibbonColorPicker>(b => b.Add(x => x.Text, "Font Color").Add(x => x.Value, Color.Error)
                .Add(x => x.ValueChanged, v => raised.Add(v.ToString()))));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-split-face")));
        Assert.Contains("var(--mud-palette-error)", cut.Find(".mud-ex-ribbon-color-bar").GetAttribute("style"));

        cut.Find(".mud-ex-ribbon-split-face").Click();
        cut.WaitForAssertion(() => Assert.Equal(["Error"], raised));

        cut.Find(".mud-ex-ribbon-split-arrow").Click();
        popovers.WaitForAssertion(() => Assert.Equal(18, popovers.FindAll(".mud-ex-ribbon-swatch").Count));
        Assert.Equal(["Primary", "Secondary", "Tertiary", "Info", "Success", "Warning", "Error", "Dark",
                "#C00000", "#FF0000", "#FFC000", "#FFFF00", "#92D050", "#00B050", "#00B0F0", "#0070C0", "#002060", "#7030A0"],
            popovers.FindAll(".mud-ex-ribbon-swatch").Select(e => e.GetAttribute("aria-label")));

        popovers.FindAll(".mud-ex-ribbon-swatch").Single(e => e.GetAttribute("aria-label") == "#0070C0").Click();
        cut.WaitForAssertion(() => Assert.Equal(["Error", "#0070C0"], raised));
        cut.WaitForAssertion(() => Assert.Contains("#0070c0", cut.Find(".mud-ex-ribbon-color-bar").GetAttribute("style"), StringComparison.OrdinalIgnoreCase));

        cut.Find(".mud-ex-ribbon-split-face").Click();
        cut.WaitForAssertion(() => Assert.Equal(["Error", "#0070C0", "#0070C0"], raised));

        cut.Find(".mud-ex-ribbon-split-arrow").Click();
        popovers.WaitForAssertion(() => Assert.Single(popovers.FindAll(".mud-ex-ribbon-more-colors")));
        context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        popovers.Find(".mud-ex-ribbon-more-colors").Click();
        popovers.WaitForAssertion(() => Assert.Single(popovers.FindComponents<MudExColorEdit>()));

        var edit = popovers.FindComponent<MudExColorEdit>();
        await popovers.InvokeAsync(() => edit.Instance.ValueChanged.InvokeAsync("#123456"));
        cut.WaitForAssertion(() => Assert.Equal("#123456", raised.Last()));
        Assert.Single(popovers.FindComponents<MudExColorEdit>());
    }

    [Fact]
    public void NewItems_RenderInEveryLayout()
    {
        var (cut, _, context) = RenderGroup(g => g
            .AddChildContent<MudExRibbonMenuButton>(m => m.Add(x => x.Text, "Paste").Add(x => x.Split, true)
                .AddChildContent<MudExRibbonMenuItem>(i => i.Add(x => x.Text, "Paste Special")))
            .AddChildContent<MudExRibbonMenuItem>(m => m.Add(x => x.Text, "Shapes").Add(x => x.Checked, true)
                .AddChildContent<MudExRibbonMenuItem>(i => i.Add(x => x.Text, "Circle")))
            .AddChildContent<MudExRibbonGallery<string>>(b => b.Add(x => x.Text, "Styles").Add(x => x.Items, Styles))
            .AddChildContent<MudExRibbonNumberBox>(b => b.Add(x => x.Text, "Indent"))
            .AddChildContent<MudExRibbonSelect<string>>(b => b.Add(x => x.Text, "Font").Add(x => x.Items, ["Arial"]))
            .AddChildContent<MudExRibbonColorPicker>(b => b.Add(x => x.Text, "Highlight")));

        cut.WaitForAssertion(() => Assert.Equal(6, cut.Instance.Tabs[0].Groups[0].Items.Count));
        var items = cut.Instance.Tabs[0].Groups[0].Items;
        Assert.Equal([true, true, false, false, false, true], items.Select(i => i.IsCommand));

        foreach (var item in items)
        foreach (var layout in Enum.GetValues<MudExRibbonItemLayout>())
        {
            var rendered = context.Render(item.Render(layout));
            Assert.False(string.IsNullOrWhiteSpace(rendered.Markup), $"{item.Text} {layout}");
            if (layout == MudExRibbonItemLayout.MenuEntry)
                Assert.Contains(item.Text, rendered.Markup);
        }
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

        [Parameter] public bool UnstableText { get; set; }

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
                        item.AddAttribute(1, nameof(MudExRibbonButton.Text), UnstableText ? Guid.NewGuid().ToString() : string.Concat("Bo", "ld"));
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
