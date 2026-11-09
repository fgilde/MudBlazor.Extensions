using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
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
        Action<ComponentParameterCollectionBuilder<MudExRibbonGroup>> configure, TimeProvider? time = null)
    {
        var context = NewContext();
        if (time != null)
            context.Services.AddSingleton(time);
        var popovers = context.Render<MudPopoverProvider>();
        var cut = context.Render<MudExRibbon>(p => p.Add(x => x.ShowModeSwitch, false).AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Home")
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

    [Fact]
    public void MenuActivator_EnterThenClick_OpensMenu()
    {
        var (cut, popovers, _) = RenderGroup(g => g
            .AddChildContent<MudExRibbonMenuButton>(m => m.Add(x => x.Text, "Styles")
                .AddChildContent<MudExRibbonMenuItem>(i => i.Add(x => x.Text, "Heading")))
            .AddChildContent<MudExRibbonMenuButton>(m => m.Add(x => x.Text, "Paste").Add(x => x.Split, true)
                .AddChildContent<MudExRibbonMenuItem>(i => i.Add(x => x.Text, "Paste Special")))
            .AddChildContent<MudExRibbonGallery<string>>(b => b.Add(x => x.Text, "Gallery").Add(x => x.Items, Styles)), new FrozenTime());

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[aria-haspopup=menu]").Count));
        string[] activators = [".mud-ex-ribbon-menu-face", ".mud-ex-ribbon-split-arrow", ".mud-ex-ribbon-gallery-more"];
        for (var i = 0; i < activators.Length; i++)
        {
            var index = i;
            cut.FindAll("[aria-haspopup=menu]")[index].KeyDown(new KeyboardEventArgs { Key = "Enter" });
            cut.WaitForAssertion(() => Assert.Equal("true", cut.FindAll("[aria-haspopup=menu]")[index].GetAttribute("aria-expanded")));
            cut.Find(activators[index]).Click();
            cut.WaitForAssertion(() => Assert.Equal("true", cut.FindAll("[aria-haspopup=menu]")[index].GetAttribute("aria-expanded")));
        }
        popovers.WaitForAssertion(() => Assert.Contains("Heading", popovers.Markup));
    }

    [Fact]
    public void SplitFace_Enter_RunsOnClick_MenuStaysClosed()
    {
        var clicks = 0;
        var (cut, _, _) = RenderGroup(g => g
            .AddChildContent<MudExRibbonMenuButton>(m => m.Add(x => x.Text, "Paste").Add(x => x.Split, true).Add(x => x.OnClick, () => clicks++)
                .AddChildContent<MudExRibbonMenuItem>(i => i.Add(x => x.Text, "Paste Special"))), new FrozenTime());

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-split-face")));
        var keyDown = Record.Exception(() => cut.Find(".mud-ex-ribbon-split-face").KeyDown(new KeyboardEventArgs { Key = "Enter" }));
        Assert.IsType<MissingEventHandlerException>(keyDown);
        cut.Find(".mud-ex-ribbon-split-face").Click();

        cut.WaitForAssertion(() => Assert.Equal(1, clicks));
        Assert.Equal("false", cut.Find("[aria-haspopup=menu]").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void ColorPicker_FaceWithoutColor_DoesNotRaise()
    {
        var raised = 0;
        var (cut, _, _) = RenderGroup(g => g
            .AddChildContent<MudExRibbonColorPicker>(b => b.Add(x => x.Text, "Highlight").Add(x => x.ValueChanged, _ => raised++)));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-split-face")));
        cut.Find(".mud-ex-ribbon-split-face").Click();

        Assert.Equal(0, raised);
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

        cut.Find(".mud-ex-ribbon-split-arrow").Click();
        cut.WaitForAssertion(() => Assert.Equal("false", cut.Find("[aria-haspopup=menu]").GetAttribute("aria-expanded")));
        cut.Find(".mud-ex-ribbon-split-arrow").Click();
        popovers.WaitForAssertion(() => Assert.Single(popovers.FindAll(".mud-ex-ribbon-more-colors")));
        Assert.Empty(popovers.FindComponents<MudExColorEdit>());
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

    private static readonly string[] Commands = ["Paste", "Keep Text Only", "Bold", "Italic", "Cut", "Styles", "Heading", "Table"];

    private (IRenderedComponent<MudExRibbon> Ribbon, IRenderedComponent<MudPopoverProvider> Popovers) RenderModes(
        Action<ComponentParameterCollectionBuilder<MudExRibbon>> configure, Action? onPaste = null, Action<bool>? onBold = null)
    {
        var context = NewContext();
        context.Services.AddSingleton<TimeProvider>(new FrozenTime());
        var popovers = context.Render<MudPopoverProvider>();
        var cut = context.Render<MudExRibbon>(p =>
        {
            configure(p);
            p.AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Home")
                    .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Clipboard")
                        .AddChildContent<MudExRibbonMenuButton>(m => m.Add(x => x.Text, "Paste").Add(x => x.Split, true).Add(x => x.Size, MudExRibbonItemSize.Large)
                            .Add(x => x.Icon, Icons.Material.Filled.ContentPaste).Add(x => x.OnClick, () => onPaste?.Invoke())
                            .AddChildContent<MudExRibbonMenuItem>(i => i.Add(x => x.Text, "Keep Text Only")))
                        .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Cut").Add(x => x.Size, MudExRibbonItemSize.Small).Add(x => x.Icon, Icons.Material.Filled.ContentCut)))
                    .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Font")
                        .AddChildContent<MudExRibbonRow>(r => r
                            .AddChildContent<MudExRibbonToggleButton>(b => b.Add(x => x.Text, "Bold").Add(x => x.Size, MudExRibbonItemSize.Small)
                                .Add(x => x.Icon, Icons.Material.Filled.FormatBold).Add(x => x.CheckedChanged, v => onBold?.Invoke(v)))
                            .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Italic").Add(x => x.Size, MudExRibbonItemSize.Small).Add(x => x.Icon, Icons.Material.Filled.FormatItalic)))
                        .AddChildContent<MudExRibbonMenuButton>(m => m.Add(x => x.Text, "Styles")
                            .AddChildContent<MudExRibbonMenuItem>(i => i.Add(x => x.Text, "Heading")))))
                .AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Insert")
                    .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Tables")
                        .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Table").Add(x => x.Icon, Icons.Material.Filled.TableChart))));
        });
        cut.WaitForAssertion(() => Assert.Equal(2, cut.Instance.Tabs.Count));
        return (cut, popovers);
    }

    private static IEnumerable<string> Labels(IRenderedComponent<MudExRibbon> cut, IRenderedComponent<MudPopoverProvider> popovers)
        => cut.FindAll("*").Concat(popovers.FindAll("*"))
            .SelectMany(e => e.ChildNodes.OfType<AngleSharp.Dom.IText>().Select(t => t.Text.Trim()).Append(e.GetAttribute("aria-label") ?? string.Empty))
            .Where(s => s.Length > 0);

    private static void OpenAllMenus(IRenderedComponent<MudExRibbon> cut, IRenderedComponent<MudPopoverProvider> popovers, HashSet<string> found)
    {
        var opened = new HashSet<string>();
        found.UnionWith(Labels(cut, popovers));
        while (cut.FindAll("[aria-haspopup=menu]").Concat(popovers.FindAll("[aria-haspopup=menu]"))
                   .FirstOrDefault(e => e.Closest(".mud-ex-ribbon-mode-switch") == null && opened.Add(e.Closest(".mud-menu")!.Id!)) is { } activator)
        {
            try
            {
                activator.Click();
            }
            catch (MissingEventHandlerException)
            {
                activator.QuerySelectorAll("button").Last().Click();
            }
            found.UnionWith(Labels(cut, popovers));
        }
    }

    [Theory]
    [InlineData(MudExRibbonMode.Classic)]
    [InlineData(MudExRibbonMode.Simplified)]
    [InlineData(MudExRibbonMode.Menu)]
    [InlineData(MudExRibbonMode.Toolbar)]
    public void EveryMode_ExposesTheSameCommands(MudExRibbonMode mode)
    {
        var (cut, popovers) = RenderModes(p => p.Add(x => x.Mode, mode));
        var found = new HashSet<string>();

        for (var tab = 0; tab < 2; tab++)
        {
            var index = tab;
            cut.Render(p => p.Add(x => x.ActiveTabIndex, index));
            cut.WaitForAssertion(() => Assert.Single(cut.FindAll($".mud-ex-ribbon-{mode.ToString().ToLowerInvariant()}-mode")));
            OpenAllMenus(cut, popovers, found);
        }

        Assert.Empty(Commands.Except(found));
    }

    [Fact]
    public void Simplified_RendersOneRowOfMediumAndSmallItems()
    {
        var (cut, _) = RenderModes(p => p.Add(x => x.Mode, MudExRibbonMode.Simplified));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-simplified-body")));
        Assert.Empty(cut.FindAll(".mud-ex-ribbon-large"));
        Assert.Single(cut.FindAll(".mud-ex-ribbon-split-medium"));
        Assert.Contains(cut.FindAll(".mud-ex-ribbon-small"), e => e.GetAttribute("aria-label") == "Cut");
        Assert.Single(cut.FindAll(".mud-ex-ribbon-simplified-body > .mud-ex-ribbon-separator"));
        Assert.Empty(cut.FindAll(".mud-ex-ribbon-group-title"));
    }

    [Fact]
    public void Toolbar_TabDropdownSelectsTab()
    {
        var (cut, popovers) = RenderModes(p => p.Add(x => x.Mode, MudExRibbonMode.Toolbar));

        cut.WaitForAssertion(() => Assert.Contains(cut.FindAll(".mud-ex-ribbon-toolbaricon"), e => e.GetAttribute("aria-label") == "Cut"));
        Assert.Contains("Home", cut.Find(".mud-ex-ribbon-tab-select").TextContent);

        cut.Find(".mud-ex-ribbon-tab-select button").Click();
        popovers.WaitForAssertion(() => Assert.Contains(popovers.FindAll("[role=menuitem]"), e => e.TextContent.Contains("Insert")));
        popovers.FindAll("[role=menuitem]").First(e => e.TextContent.Contains("Insert")).Click();

        cut.WaitForAssertion(() => Assert.Contains(cut.FindAll(".mud-ex-ribbon-toolbaricon"), e => e.GetAttribute("aria-label") == "Table"));
        Assert.Equal(1, cut.Instance.ActiveTabIndex);
    }

    [Fact]
    public void ModeSwitch_RaisesModeChanged()
    {
        var raised = new List<MudExRibbonMode>();
        var (cut, popovers) = RenderModes(p => p.Add(x => x.ModeChanged, m => raised.Add(m)));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-mode-switch button")));
        cut.Find(".mud-ex-ribbon-mode-switch button").Click();
        popovers.WaitForAssertion(() => Assert.Equal(["Classic ribbon", "Simplified ribbon", "Menu bar", "Toolbar"],
            popovers.FindAll(".mud-ex-ribbon-mode-option").Select(e => e.TextContent.Trim())));
        Assert.All(popovers.FindAll(".mud-ex-ribbon-mode-option"), e => Assert.Equal("menuitemradio", e.GetAttribute("role")));
        Assert.Equal(["true", "false", "false", "false"], popovers.FindAll(".mud-ex-ribbon-mode-option").Select(e => e.GetAttribute("aria-checked")));

        popovers.FindAll(".mud-ex-ribbon-mode-option")[2].Click();

        cut.WaitForAssertion(() => Assert.Equal([MudExRibbonMode.Menu], raised));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-menu-mode")));
        Assert.Equal(MudExRibbonMode.Menu, cut.Instance.Mode);
        Assert.Single(cut.FindAll(".mud-ex-ribbon-mode-switch"));

        cut.Render(p => p.Add(x => x.ShowModeSwitch, false));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-ribbon-mode-switch")));
    }

    [Fact]
    public void Collapsed_HidesGroups_TabClickShowsOverlay()
    {
        var pastes = 0;
        var (cut, popovers) = RenderModes(p => p.Add(x => x.Collapsed, true), () => pastes++);

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[role=tab]").Count));
        Assert.Empty(cut.FindAll("[role=toolbar]"));
        Assert.Empty(popovers.FindAll("[role=toolbar]"));

        cut.FindAll("[role=tab]")[0].Click();
        popovers.WaitForAssertion(() => Assert.Equal(["Clipboard", "Font"], popovers.FindAll("[role=toolbar]").Select(e => e.GetAttribute("aria-label"))));

        popovers.Find(".mud-ex-ribbon-split-face").Click();
        cut.WaitForAssertion(() => Assert.Equal(1, pastes));
        popovers.WaitForAssertion(() => Assert.Empty(popovers.FindAll("[role=toolbar]")));

        cut.FindAll("[role=tab]")[1].Click();
        popovers.WaitForAssertion(() => Assert.Equal(["Tables"], popovers.FindAll("[role=toolbar]").Select(e => e.GetAttribute("aria-label"))));
        popovers.Find(".mud-overlay.mud-ex-ribbon-overlay-backdrop").Click();
        popovers.WaitForAssertion(() => Assert.Empty(popovers.FindAll("[role=toolbar]")));
        Assert.True(cut.Instance.Collapsed);

        cut.Render(p => p.Add(x => x.Mode, MudExRibbonMode.Toolbar));
        cut.WaitForAssertion(() => Assert.Contains(cut.FindAll(".mud-ex-ribbon-toolbaricon"), e => e.GetAttribute("aria-label") == "Table"));
    }

    [Fact]
    public void Collapsed_EscapeClosesOverlay()
    {
        var launches = 0;
        var context = NewContext();
        var popovers = context.Render<MudPopoverProvider>();
        var cut = context.Render<MudExRibbon>(p => p.Add(x => x.Collapsed, true).AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Home")
            .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Font").Add(x => x.OnDialogLauncherClick, () => launches++)
                .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Bold")))));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[role=tab]")));
        Assert.Equal("false", cut.Find("[role=tab]").GetAttribute("aria-expanded"));
        Assert.False(cut.Find("[role=tab]").HasAttribute("aria-controls"));

        cut.Find("[role=tab]").Click();
        popovers.WaitForAssertion(() => Assert.Single(popovers.FindAll("[role=toolbar]")));
        Assert.Equal("true", cut.Find("[role=tab]").GetAttribute("aria-expanded"));
        Assert.Equal(popovers.Find("[role=tabpanel]").Id, cut.Find("[role=tab]").GetAttribute("aria-controls"));
        Assert.Contains("mud-ex-ribbon-overlay-backdrop", popovers.Find(".mud-overlay").ClassList);
        Assert.Contains("mud-ex-ribbon-header-raised", cut.Find(".mud-ex-ribbon-header").ClassList);

        popovers.Find(".mud-ex-ribbon-overlay-content").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        popovers.WaitForAssertion(() => Assert.Empty(popovers.FindAll("[role=toolbar]")));
        Assert.Equal("false", cut.Find("[role=tab]").GetAttribute("aria-expanded"));

        cut.Find("[role=tab]").Click();
        popovers.WaitForAssertion(() => Assert.Single(popovers.FindAll("[role=toolbar]")));
        popovers.Find(".mud-ex-ribbon-dialog-launcher").Click();
        cut.WaitForAssertion(() => Assert.Equal(1, launches));
        popovers.WaitForAssertion(() => Assert.Empty(popovers.FindAll("[role=toolbar]")));

        cut.Render(p => p.Add(x => x.Collapsed, false));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[role=toolbar]")));
        Assert.False(cut.Find("[role=tab]").HasAttribute("aria-expanded"));
        Assert.Equal(cut.Find("[role=tabpanel]").Id, cut.Find("[role=tab]").GetAttribute("aria-controls"));
    }

    [Theory]
    [InlineData(MudExRibbonMode.Classic, "[role=tablist]")]
    [InlineData(MudExRibbonMode.Simplified, "[role=tablist]")]
    [InlineData(MudExRibbonMode.Menu, ".mud-ex-ribbon-menubar")]
    [InlineData(MudExRibbonMode.Toolbar, ".mud-ex-ribbon-toolbar")]
    public void Header_HasTitleBarAndSeparateTabRow_InEveryMode(MudExRibbonMode mode, string tabs)
    {
        var (cut, _) = RenderModes(p => p.Add(x => x.Mode, mode)
            .Add(x => x.FileMenu, "<div>Recent</div>")
            .Add(x => x.TitleContent, "<span class=\"doc-title\">Document1</span>")
            .Add(x => x.HeaderEndContent, "<span class=\"header-end\">User</span>"));

        var rows = cut.Find(".mud-ex-ribbon-header").Children.Select(e => e.ClassName!).ToArray();
        Assert.Equal(["mud-ex-ribbon-title-bar", "mud-ex-ribbon-tab-row"], rows);
        var titleBar = cut.Find(".mud-ex-ribbon-title-bar");
        var tabRow = cut.Find(".mud-ex-ribbon-tab-row");
        Assert.NotNull(titleBar.QuerySelector(".doc-title"));
        Assert.NotNull(titleBar.QuerySelector(".mud-ex-ribbon-command-search-host .mud-ex-ribbon-command-search"));
        Assert.NotNull(titleBar.QuerySelector(".header-end"));
        Assert.NotNull(titleBar.QuerySelector(".mud-ex-ribbon-mode-switch"));
        Assert.Null(titleBar.QuerySelector(".mud-ex-ribbon-file-tab"));
        Assert.Contains("mud-ex-ribbon-file-tab", tabRow.FirstElementChild!.ClassList);
        Assert.NotNull(tabRow.QuerySelector(tabs));
        Assert.Null(tabRow.QuerySelector(".mud-ex-ribbon-command-search-host"));
        Assert.Null(tabRow.QuerySelector(".mud-ex-ribbon-mode-switch"));
    }

    [Fact]
    public void Header_WithoutTitleBarContent_RendersOnlyTabRow()
    {
        var (cut, _) = RenderModes(p => p.Add(x => x.ShowCommandSearch, false).Add(x => x.ShowModeSwitch, false));

        Assert.Equal(["mud-ex-ribbon-tab-row"], cut.Find(".mud-ex-ribbon-header").Children.Select(e => e.ClassName!));
    }

    [Fact]
    public void CommandSearchToggle_ExpandsSearchAndFocusesInput()
    {
        var context = NewContext();
        context.Render<MudPopoverProvider>();
        var cut = context.Render<MudExRibbon>(p => p.AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Home")
            .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Font").AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Bold")))));

        var host = cut.Find(".mud-ex-ribbon-command-search-host");
        Assert.DoesNotContain("mud-ex-ribbon-command-search-expanded", host.ClassList);
        Assert.Equal("Search commands", cut.Find(".mud-ex-ribbon-command-search-toggle").GetAttribute("aria-label"));

        cut.Find(".mud-ex-ribbon-command-search-toggle").Click();

        cut.WaitForAssertion(() => Assert.Contains("mud-ex-ribbon-command-search-expanded", cut.Find(".mud-ex-ribbon-command-search-host").ClassList));
        cut.WaitForAssertion(() => Assert.NotEmpty(context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]));

        cut.Find(".mud-ex-ribbon-command-search-host").TriggerEvent("onfocusout", new FocusEventArgs());
        cut.WaitForAssertion(() => Assert.DoesNotContain("mud-ex-ribbon-command-search-expanded", cut.Find(".mud-ex-ribbon-command-search-host").ClassList));
    }

    [Fact]
    public void Collapsed_OverlayIsBoundToRibbonWidth_ToggleInTabRow()
    {
        var (cut, popovers) = RenderModes(p => p.Add(x => x.Collapsed, true));

        Assert.NotNull(cut.Find(".mud-ex-ribbon-tab-row > .mud-ex-ribbon-collapse-toggle"));
        cut.FindAll("[role=tab]")[0].Click();
        popovers.WaitForAssertion(() => Assert.NotEmpty(popovers.FindAll("[role=toolbar]")));

        var overlay = popovers.Find(".mud-ex-ribbon-overlay:not(.mud-ex-ribbon-group-popover)");
        Assert.Contains("mud-popover-relative-width", overlay.ClassList);
        Assert.DoesNotContain("mud-popover-adaptive-width", overlay.ClassList);
        Assert.Equal(cut.Instance.PanelId, overlay.QuerySelector(".mud-ex-ribbon-overlay-content > .mud-ex-ribbon-body")!.Id);
    }

    [Fact]
    public void DoubleClickTab_TogglesCollapsed()
    {
        var raised = new List<bool>();
        var (cut, _) = RenderModes(p => p.Add(x => x.CollapsedChanged, v => raised.Add(v)));

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[role=toolbar]").Count));
        cut.FindAll("[role=tab]")[0].DoubleClick();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role=toolbar]")));
        Assert.Equal([true], raised);

        cut.FindAll("[role=tab]")[0].DoubleClick();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[role=toolbar]").Count));
        Assert.Equal([true, false], raised);

        cut.Find(".mud-ex-ribbon-body .mud-ex-ribbon-collapse-toggle").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role=toolbar]")));
        cut.Find(".mud-ex-ribbon-header .mud-ex-ribbon-collapse-toggle").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[role=toolbar]").Count));
        Assert.Equal([true, false, true, false], raised);

        cut.Render(p => p.Add(x => x.AllowCollapse, false));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-ribbon-collapse-toggle")));
        cut.FindAll("[role=tab]")[0].DoubleClick();
        Assert.Equal(2, cut.FindAll("[role=toolbar]").Count);
        Assert.Equal(4, raised.Count);
    }

    [Fact]
    public void Menu_ExecutingCommand_ClosesMenu()
    {
        var pastes = 0;
        bool? bold = null;
        var (cut, popovers) = RenderModes(p => p.Add(x => x.Mode, MudExRibbonMode.Menu), () => pastes++, v => bold = v);

        cut.WaitForAssertion(() => Assert.Equal(["Home", "Insert"], cut.FindAll(".mud-ex-ribbon-menubar-menu > button").Select(e => e.TextContent.Trim())));
        cut.FindAll(".mud-ex-ribbon-menubar-menu > button")[0].Click();
        popovers.WaitForAssertion(() => Assert.Equal(["Clipboard", "Font"], popovers.FindAll(".mud-ex-ribbon-menu-subheader").Select(e => e.TextContent.Trim())));
        var boldEntry = popovers.Find("[role=menuitemcheckbox]");
        Assert.Equal("false", boldEntry.GetAttribute("aria-checked"));

        boldEntry.Click();

        cut.WaitForAssertion(() => Assert.True(bold));
        cut.WaitForAssertion(() => Assert.Equal("false", cut.FindAll(".mud-ex-ribbon-menubar-menu > button")[0].GetAttribute("aria-expanded")));

        cut.FindAll(".mud-ex-ribbon-menubar-menu > button")[0].Click();
        popovers.WaitForAssertion(() => Assert.Equal("true", popovers.Find("[role=menuitemcheckbox]").GetAttribute("aria-checked")));
        popovers.FindAll("[aria-haspopup=menu]").First(e => e.TextContent.Contains("Paste")).Click();
        popovers.WaitForAssertion(() => Assert.Contains(popovers.FindAll("[role=menuitem]"), e => e.TextContent.Contains("Keep Text Only")));
        popovers.FindAll("[role=menuitem]").Single(e => e.GetAttribute("aria-haspopup") == null && e.TextContent.Contains("Paste")).Click();

        cut.WaitForAssertion(() => Assert.Equal(1, pastes));
        cut.WaitForAssertion(() => Assert.Equal("false", cut.FindAll(".mud-ex-ribbon-menubar-menu > button")[0].GetAttribute("aria-expanded")));
    }

    [Fact]
    public async Task CollapsedGroup_RendersSingleButton_OpeningPopover()
    {
        var pastes = 0;
        var (cut, popovers) = RenderModes(_ => { }, () => pastes++);
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[role=toolbar]").Count));

        await cut.InvokeAsync(() => cut.Instance.SetReductionStep(100));

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".mud-ex-ribbon-group-collapsed").Count));
        Assert.Equal(6, cut.Instance.ReductionStep);
        Assert.Empty(cut.FindAll("[role=toolbar]"));
        Assert.All(cut.FindAll(".mud-ex-ribbon-group-collapsed"), g => Assert.Single(g.QuerySelectorAll("button")));
        var buttons = cut.FindAll(".mud-ex-ribbon-group-collapsed > .mud-ex-ribbon-button.mud-ex-ribbon-large");
        Assert.Equal(["Clipboard", "Font"], buttons.Select(b => b.TextContent.Trim()));
        Assert.All(buttons, b => Assert.Equal("false", b.GetAttribute("aria-expanded")));
        Assert.All(buttons, b => Assert.Equal("dialog", b.GetAttribute("aria-haspopup")));

        buttons[0].Click();
        popovers.WaitForAssertion(() => Assert.Equal(["Clipboard"], popovers.FindAll("[role=toolbar]").Select(e => e.GetAttribute("aria-label"))));
        Assert.Equal("true", cut.FindAll(".mud-ex-ribbon-group-collapsed > .mud-ex-ribbon-button")[0].GetAttribute("aria-expanded"));
        Assert.Single(popovers.FindAll(".mud-ex-ribbon-split-large"));

        popovers.Find(".mud-ex-ribbon-split-face").Click();
        cut.WaitForAssertion(() => Assert.Equal(1, pastes));
        popovers.WaitForAssertion(() => Assert.Empty(popovers.FindAll("[role=toolbar]")));

        cut.FindAll(".mud-ex-ribbon-group-collapsed > .mud-ex-ribbon-button")[1].Click();
        popovers.WaitForAssertion(() => Assert.Equal(["Font"], popovers.FindAll("[role=toolbar]").Select(e => e.GetAttribute("aria-label"))));
        Assert.Equal("Font", popovers.Find("[role=dialog]").GetAttribute("aria-label"));
        popovers.Find(".mud-ex-ribbon-group-popover .mud-ex-ribbon-overlay-content").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        popovers.WaitForAssertion(() => Assert.Empty(popovers.FindAll("[role=toolbar]")));

        cut.FindAll(".mud-ex-ribbon-group-collapsed > .mud-ex-ribbon-button")[1].Click();
        popovers.WaitForAssertion(() => Assert.Single(popovers.FindAll("[role=toolbar]")));
        cut.FindAll(".mud-ex-ribbon-group-collapsed > .mud-ex-ribbon-button")[1].KeyDown(new KeyboardEventArgs { Key = "Escape" });
        popovers.WaitForAssertion(() => Assert.Empty(popovers.FindAll("[role=toolbar]")));
    }

    [Theory]
    [InlineData(MudExRibbonMode.Classic, 6)]
    [InlineData(MudExRibbonMode.Simplified, 4)]
    public async Task MaxReductionStep_LetsTheBodyScroll(MudExRibbonMode mode, int max)
    {
        var (cut, _) = RenderModes(p => p.Add(x => x.Mode, mode));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-body")));
        Assert.Empty(cut.FindAll(".mud-ex-ribbon-body-scroll"));

        await cut.InvokeAsync(() => cut.Instance.SetReductionStep(max - 1));
        cut.WaitForAssertion(() => Assert.Equal(max - 1, cut.Instance.ReductionStep));
        Assert.Empty(cut.FindAll(".mud-ex-ribbon-body-scroll"));

        await cut.InvokeAsync(() => cut.Instance.SetReductionStep(max));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-body.mud-ex-ribbon-body-scroll")));

        cut.FindAll("[role=tab]")[1].Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-ribbon-body-scroll")));
    }

    [Fact]
    public void TogglingCollapsedOrOverlay_ResetsJsMeasuring()
    {
        var context = NewContext();
        context.Render<MudPopoverProvider>();
        var cut = context.Render<MudExRibbon>(p => p.AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Home")
            .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Font")
                .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Bold")))));
        IEnumerable<bool> Resets() => context.JSInterop.Invocations["update"].Select(i => (bool)i.Arguments[2]!);

        cut.WaitForAssertion(() => Assert.Equal([true], Resets()));

        cut.Render(p => p.Add(x => x.Collapsed, true));
        cut.WaitForAssertion(() => Assert.Equal([true, true], Resets()));

        cut.Find("[role=tab]").Click();
        cut.WaitForAssertion(() => Assert.Equal([true, true, true], Resets()));
    }

    [Fact]
    public async Task ReducedAndCompactGroups_ShrinkItemLayouts()
    {
        var (cut, _) = RenderModes(_ => { });
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-split-large")));

        await cut.InvokeAsync(() => cut.Instance.SetReductionStep(2));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-ribbon-large")));
        Assert.Single(cut.FindAll(".mud-ex-ribbon-split-medium"));
        Assert.Contains(cut.FindAll(".mud-ex-ribbon-medium"), e => e.GetAttribute("aria-label") == "Styles");

        await cut.InvokeAsync(() => cut.Instance.SetReductionStep(4));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-ribbon-medium")));
        Assert.Single(cut.FindAll(".mud-ex-ribbon-split-small"));
        Assert.Equal(2, cut.FindAll("[role=toolbar]").Count);
        Assert.Equal(2, cut.FindAll(".mud-ex-ribbon-group-title").Count);
    }

    [Fact]
    public async Task Simplified_ReductionStep_MovesItemsFromTheRightIntoOverflowMenu()
    {
        var (cut, popovers) = RenderModes(p => p.Add(x => x.Mode, MudExRibbonMode.Simplified));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-simplified-body")));
        Assert.Empty(cut.FindAll(".mud-ex-ribbon-overflow"));

        await cut.InvokeAsync(() => cut.Instance.SetReductionStep(1));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-overflow")));
        Assert.DoesNotContain(cut.FindAll(".mud-ex-ribbon-button"), e => e.GetAttribute("aria-label") == "Styles");
        Assert.Contains(cut.FindAll(".mud-ex-ribbon-button"), e => e.GetAttribute("aria-label") == "Italic");

        await cut.InvokeAsync(() => cut.Instance.SetReductionStep(3));
        cut.WaitForAssertion(() => Assert.Equal(["Clipboard"], cut.FindAll("[role=toolbar]").Select(e => e.GetAttribute("aria-label"))));
        Assert.Empty(cut.FindAll(".mud-ex-ribbon-simplified-body > .mud-ex-ribbon-separator"));
        Assert.DoesNotContain(cut.FindAll(".mud-ex-ribbon-button"), e => e.GetAttribute("aria-label") == "Cut");

        cut.Find(".mud-ex-ribbon-overflow button").Click();
        popovers.WaitForAssertion(() =>
        {
            Assert.Contains(popovers.FindAll("[role=menuitem]"), e => e.TextContent.Contains("Cut"));
            Assert.Contains(popovers.FindAll("[role=menuitemcheckbox]"), e => e.TextContent.Contains("Bold"));
            Assert.Contains(popovers.FindAll("[role=menuitem]"), e => e.TextContent.Contains("Styles"));
        });
        Assert.Single(popovers.FindAll(".mud-ex-ribbon-menu-section-divider"));

        await cut.InvokeAsync(() => cut.Instance.SetReductionStep(99));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role=toolbar]")));
        Assert.Equal(4, cut.Instance.ReductionStep);
    }

    [Fact]
    public async Task ChangingTabOrMode_ResetsReductionStep_MenuAndToolbarIgnoreIt()
    {
        var (cut, _) = RenderModes(_ => { });
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[role=toolbar]").Count));

        await cut.InvokeAsync(() => cut.Instance.SetReductionStep(6));
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".mud-ex-ribbon-group-collapsed").Count));

        cut.FindAll("[role=tab]")[1].Click();
        cut.WaitForAssertion(() => Assert.Equal(["Tables"], cut.FindAll("[role=toolbar]").Select(e => e.GetAttribute("aria-label"))));
        Assert.Equal(0, cut.Instance.ReductionStep);
        cut.FindAll("[role=tab]")[0].Click();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[role=toolbar]").Count));
        Assert.Empty(cut.FindAll(".mud-ex-ribbon-group-collapsed"));

        await cut.InvokeAsync(() => cut.Instance.SetReductionStep(6));
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".mud-ex-ribbon-group-collapsed").Count));
        cut.Render(p => p.Add(x => x.Mode, MudExRibbonMode.Simplified));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-simplified-body")));
        Assert.Equal(0, cut.Instance.ReductionStep);
        Assert.Empty(cut.FindAll(".mud-ex-ribbon-overflow"));

        cut.Render(p => p.Add(x => x.Mode, MudExRibbonMode.Toolbar));
        await cut.InvokeAsync(() => cut.Instance.SetReductionStep(3));
        Assert.Equal(0, cut.Instance.ReductionStep);
        cut.WaitForAssertion(() => Assert.Contains(cut.FindAll(".mud-ex-ribbon-toolbaricon"), e => e.GetAttribute("aria-label") == "Styles"));
    }

    [Fact]
    public void Select_DatalistIdIsStablePerItemAndLayout()
    {
        var (cut, _, context) = RenderGroup(g => g
            .AddChildContent<MudExRibbonSelect<string>>(b => b.Add(x => x.Text, "Size").Add(x => x.Items, ["10", "11"]).Add(x => x.Editable, true))
            .AddChildContent<MudExRibbonSelect<string>>(b => b.Add(x => x.Text, "Font").Add(x => x.Items, ["Arial"]).Add(x => x.Editable, true)));

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("datalist").Count));
        Assert.NotEqual(cut.FindAll("datalist")[0].Id, cut.FindAll("datalist")[1].Id);
        var item = cut.Instance.Tabs[0].Groups[0].Items[0];
        var medium = context.Render(item.Render(MudExRibbonItemLayout.Medium));
        var again = context.Render(item.Render(MudExRibbonItemLayout.Medium));
        var menu = context.Render(item.Render(MudExRibbonItemLayout.MenuEntry));

        Assert.Equal(medium.Find("datalist").Id, again.Find("datalist").Id);
        Assert.NotEqual(medium.Find("datalist").Id, menu.Find("datalist").Id);
        Assert.Equal(medium.Find("datalist").Id, medium.Find("input").GetAttribute("list"));
        Assert.Equal(menu.Find("datalist").Id, menu.Find("input").GetAttribute("list"));

        var id = cut.Find("datalist").Id;
        cut.Instance.Invalidate();
        cut.WaitForAssertion(() => Assert.Equal(id, cut.Find("datalist").Id));
    }

    private static RenderFragment Tab(string text, string group, string item, string? contextualTitle = null, MudExColor? color = null, bool visible = true) => b =>
    {
        b.OpenComponent<MudExRibbonTab>(0);
        b.AddAttribute(1, nameof(MudExRibbonTab.Text), text);
        b.AddAttribute(2, nameof(MudExRibbonTab.Contextual), contextualTitle != null);
        b.AddAttribute(3, nameof(MudExRibbonTab.ContextualTitle), contextualTitle);
        b.AddAttribute(4, nameof(MudExRibbonTab.ContextualColor), color);
        b.AddAttribute(5, nameof(MudExRibbonTab.Visible), visible);
        b.AddAttribute(6, nameof(MudExRibbonTab.ChildContent), (RenderFragment)(g =>
        {
            g.OpenComponent<MudExRibbonGroup>(0);
            g.AddAttribute(1, nameof(MudExRibbonGroup.Text), group);
            g.AddAttribute(2, nameof(MudExRibbonGroup.ChildContent), (RenderFragment)(i =>
            {
                i.OpenComponent<MudExRibbonButton>(0);
                i.AddAttribute(1, nameof(MudExRibbonButton.Text), item);
                i.CloseComponent();
            }));
            g.CloseComponent();
        }));
        b.CloseComponent();
    };

    private static void ContextualRibbon(ComponentParameterCollectionBuilder<MudExRibbon> p, bool tableVisible)
    {
        RenderFragment[] tabs =
        [
            Tab("Home", "Clipboard", "Paste"),
            Tab("Design", "Styles", "Shading", "Table Tools", visible: tableVisible),
            Tab("Layout", "Merge", "Merge Cells", "Table Tools", visible: tableVisible),
            Tab("Picture", "Adjust", "Crop", "Picture Tools", Color.Tertiary, tableVisible),
            Tab("Insert", "Tables", "Table")
        ];
        p.Add(x => x.ChildContent, b =>
        {
            foreach (var tab in tabs)
                b.AddContent(0, tab);
        });
    }

    [Fact]
    public void ContextualTab_HiddenUntilVisible_RenderedAfterNormalTabs()
    {
        var context = NewContext();
        var popovers = context.Render<MudPopoverProvider>();
        var cut = context.Render<MudExRibbon>(p => ContextualRibbon(p, false));

        cut.WaitForAssertion(() => Assert.Equal(["Home", "Insert"], cut.FindAll("[role=tab]").Select(e => e.TextContent.Trim())));
        Assert.Empty(cut.FindAll(".mud-ex-ribbon-contextual-title"));

        cut.Render(p => ContextualRibbon(p, true));

        cut.WaitForAssertion(() => Assert.Equal(["Home", "Insert", "Design", "Layout", "Picture"], cut.FindAll("[role=tab]").Select(e => e.TextContent.Trim())));
        Assert.Equal(["Table Tools", "Picture Tools"], cut.FindAll(".mud-ex-ribbon-contextual-title").Select(e => e.TextContent.Trim()));
        var sets = cut.FindAll(".mud-ex-ribbon-contextual");
        Assert.Equal([["Design", "Layout"], ["Picture"]], sets.Select(s => s.QuerySelectorAll("[role=tab]").Select(e => e.TextContent.Trim()).ToArray()));
        Assert.Contains("var(--mud-palette-secondary)", sets[0].GetAttribute("style"));
        Assert.Contains("var(--mud-palette-tertiary)", sets[1].GetAttribute("style"));
        Assert.All(sets.SelectMany(s => s.QuerySelectorAll("[role=tab]")), e => Assert.Contains("mud-ex-ribbon-tab-contextual", e.ClassList));
        Assert.Equal("Home", cut.Find(".mud-ex-ribbon-tab-active").TextContent.Trim());

        cut.Render(p => p.Add(x => x.Mode, MudExRibbonMode.Menu));
        cut.WaitForAssertion(() => Assert.Equal(["Home", "Insert", "Design", "Layout", "Picture"],
            cut.FindAll(".mud-ex-ribbon-menubar-menu > button").Select(e => e.TextContent.Trim())));
        Assert.Equal(["Design", "Layout", "Picture"], cut.FindAll(".mud-ex-ribbon-menubar-menu.mud-ex-ribbon-tab-contextual > button").Select(e => e.TextContent.Trim()));
        Assert.Contains("var(--mud-palette-secondary)", cut.FindAll(".mud-ex-ribbon-menubar-menu.mud-ex-ribbon-tab-contextual")[0].GetAttribute("style"));

        cut.Render(p => p.Add(x => x.Mode, MudExRibbonMode.Toolbar));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-tab-select button")));
        cut.Find(".mud-ex-ribbon-tab-select button").Click();
        popovers.WaitForAssertion(() => Assert.Equal(["Home", "Insert", "Design", "Layout", "Picture"],
            popovers.FindAll("[role=menuitem]").Select(e => e.TextContent.Trim())));
        Assert.Equal(["Design", "Layout", "Picture"], popovers.FindAll("[role=menuitem].mud-ex-ribbon-tab-contextual").Select(e => e.TextContent.Trim()));
    }

    [Fact]
    public void ContextualTab_BecomingVisible_DoesNotStealActiveTab()
    {
        var raised = new List<int>();
        var cut = Render(p =>
        {
            p.Add(x => x.ActiveTabIndex, 0).Add(x => x.ActiveTabIndexChanged, i => raised.Add(i));
            ContextualRibbon(p, false);
        });
        cut.WaitForAssertion(() => Assert.Equal("Home", cut.Find(".mud-ex-ribbon-tab-active").TextContent.Trim()));

        cut.Render(p => ContextualRibbon(p, true));
        cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll("[role=tab]").Count));
        Assert.Equal("Home", cut.Find(".mud-ex-ribbon-tab-active").TextContent.Trim());
        Assert.Equal(0, cut.Instance.ActiveTabIndex);
        Assert.Empty(raised);

        cut.FindAll("[role=tab]").Single(e => e.TextContent.Trim() == "Layout").Click();
        cut.WaitForAssertion(() => Assert.Equal("Layout", cut.Find(".mud-ex-ribbon-tab-active").TextContent.Trim()));
        Assert.Equal([2], raised);
        Assert.Equal(["Merge"], cut.FindAll("[role=toolbar]").Select(e => e.GetAttribute("aria-label")));

        cut.Render(p => ContextualRibbon(p, false));
        cut.WaitForAssertion(() => Assert.Equal("Home", cut.Find(".mud-ex-ribbon-tab-active").TextContent.Trim()));
        Assert.Equal([2, 0], raised);
        Assert.Equal(0, cut.Instance.ActiveTabIndex);
        Assert.Equal(["Clipboard"], cut.FindAll("[role=toolbar]").Select(e => e.GetAttribute("aria-label")));
    }

    [Theory]
    [InlineData(MudExRibbonMode.Classic)]
    [InlineData(MudExRibbonMode.Simplified)]
    [InlineData(MudExRibbonMode.Menu)]
    [InlineData(MudExRibbonMode.Toolbar)]
    public void FileMenu_OpensBackstage_EscapeCloses(MudExRibbonMode mode)
    {
        var raised = new List<bool>();
        var (cut, _) = RenderModes(p => p.Add(x => x.Mode, mode)
            .Add(x => x.FileMenu, "<div class=\"recent\">Recent</div>")
            .Add(x => x.FileMenuOpenChanged, v => raised.Add(v)));

        var fileTab = cut.Find(".mud-ex-ribbon-header .mud-ex-ribbon-file-tab");
        Assert.Equal("File", fileTab.TextContent.Trim());
        Assert.Equal("false", fileTab.GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll(".mud-ex-ribbon-backstage"));
        Assert.Contains("background-color:var(--mud-palette-primary-text)", fileTab.GetAttribute("style"));
        Assert.Contains("color:var(--mud-palette-primary)", fileTab.GetAttribute("style"));
        if (mode is MudExRibbonMode.Classic or MudExRibbonMode.Simplified)
            Assert.Equal("Home", cut.Find("[role=tab]").TextContent.Trim());

        fileTab.Click();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-backstage .recent")));
        Assert.Equal([true], raised);
        Assert.True(cut.Instance.FileMenuOpen);
        Assert.Equal("true", cut.Find(".mud-ex-ribbon-file-tab").GetAttribute("aria-expanded"));
        Assert.Contains("mud-ex-ribbon-header-raised", cut.Find(".mud-ex-ribbon-header").ClassList);
        Assert.Equal("dialog", cut.Find(".mud-ex-ribbon-backstage").GetAttribute("role"));

        cut.Find(".mud-ex-ribbon-backstage").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-ribbon-backstage")));
        Assert.Equal([true, false], raised);
        Assert.DoesNotContain("mud-ex-ribbon-header-raised", cut.Find(".mud-ex-ribbon-header").ClassList);

        cut.Render(p => p.Add(x => x.FileMenuOpen, true).Add(x => x.FileMenuText, "Datei"));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ribbon-backstage .recent")));
        Assert.Equal("Datei", cut.Find(".mud-ex-ribbon-file-tab").TextContent.Trim());

        cut.Find(".mud-ex-ribbon-backstage-back").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-ribbon-backstage")));
        Assert.Equal([true, false, false], raised);

        cut.Render(p => p.Add(x => x.FileMenu, (RenderFragment?)null));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-ribbon-file-tab")));
    }

    [Fact]
    public void FileMenu_PlacesBackstageBelowHeader_AndTabClickCloses()
    {
        var context = NewContext();
        var raised = new List<bool>();
        var cut = context.Render<MudExRibbon>(p => p.Add(x => x.FileMenu, "<div class=\"recent\">Recent</div>").Add(x => x.FileMenuOpenChanged, v => raised.Add(v))
            .AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Home").AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Clipboard")))
            .AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Insert").AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Tables"))));
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[role=tab]").Count));
        Assert.Empty(context.JSInterop.Invocations["placeBackstage"]);

        cut.Find(".mud-ex-ribbon-file-tab").Click();

        cut.WaitForAssertion(() => Assert.Equal(cut.Instance.BackstageId, Assert.Single(context.JSInterop.Invocations["placeBackstage"]).Arguments[0]));
        Assert.Equal(cut.Instance.BackstageId, cut.Find(".mud-ex-ribbon-backstage").Id);
        Assert.Equal(cut.Instance.BackstageId, cut.Find(".mud-ex-ribbon-file-tab").GetAttribute("aria-controls"));
        Assert.Contains("mud-ex-ribbon-header", cut.Find(".mud-ex-ribbon-backstage").PreviousElementSibling!.ClassList);

        List<string> Focused() => context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Select(i => ((ElementReference)i.Arguments[0]!).Id).ToList();
        string ReferenceOf(string selector) => cut.Find(selector).GetAttribute("blazor:elementReference")!;
        cut.WaitForAssertion(() => Assert.Equal([ReferenceOf(".mud-ex-ribbon-backstage-back")], Focused()));

        cut.Find(".mud-ex-ribbon-backstage").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        cut.WaitForAssertion(() => Assert.Equal(2, Focused().Count));
        var fileTab = Focused()[1];
        Assert.NotEqual(Focused()[0], fileTab);

        cut.Find(".mud-ex-ribbon-file-tab").Click();
        cut.WaitForAssertion(() => Assert.Equal(3, Focused().Count));
        cut.Find(".mud-ex-ribbon-backstage-back").Click();
        cut.WaitForAssertion(() => Assert.Equal(4, Focused().Count));
        Assert.Equal(fileTab, Focused()[3]);

        cut.Find(".mud-ex-ribbon-file-tab").Click();
        cut.WaitForAssertion(() => Assert.Equal(5, Focused().Count));
        cut.FindAll("[role=tab]")[1].Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-ribbon-backstage")));
        Assert.Equal([true, false, true, false, true, false], raised);
        Assert.Equal(1, cut.Instance.ActiveTabIndex);
        Assert.Equal(5, Focused().Count);
    }

    [Fact]
    public void PinnedCommand_OfHiddenContextualTab_IsNotShown()
    {
        var cut = Render(p =>
        {
            p.Add(x => x.PinnedCommandIds, ["Design/Styles/Shading", "Home/Clipboard/Paste"]);
            ContextualRibbon(p, false);
        });
        cut.WaitForAssertion(() => Assert.Equal(["Paste"], cut.FindAll(".mud-ex-ribbon-quick-access .mud-ex-ribbon-button").Select(e => e.GetAttribute("aria-label"))));

        cut.Render(p => ContextualRibbon(p, true));
        cut.WaitForAssertion(() => Assert.Equal(["Shading", "Paste"], cut.FindAll(".mud-ex-ribbon-quick-access .mud-ex-ribbon-button").Select(e => e.GetAttribute("aria-label"))));
    }

    [Theory]
    [InlineData(MudExRibbonMode.Classic)]
    [InlineData(MudExRibbonMode.Simplified)]
    [InlineData(MudExRibbonMode.Toolbar)]
    public void PinCommand_RaisesPinnedCommandIdsChanged_AndRendersInHeader(MudExRibbonMode mode)
    {
        var raised = new List<string[]>();
        var undos = 0;
        var (cut, popovers) = RenderModes(p => p.Add(x => x.Mode, mode)
            .Add(x => x.PinnedCommandIdsChanged, ids => raised.Add(ids.ToArray()))
            .Add<MudExRibbonButton>(x => x.QuickAccess, b => b.Add(x => x.Text, "Undo").Add(x => x.Icon, Icons.Material.Filled.Undo).Add(x => x.OnClick, () => undos++)));
        AngleSharp.Dom.IElement QuickAccess(string label) => cut.FindAll(".mud-ex-ribbon-header .mud-ex-ribbon-quick-access .mud-ex-ribbon-button.mud-ex-ribbon-small")
            .Single(e => e.GetAttribute("aria-label") == label);

        cut.WaitForAssertion(() => Assert.Equal(["Undo"], cut.FindAll(".mud-ex-ribbon-quick-access .mud-ex-ribbon-button").Select(e => e.GetAttribute("aria-label"))));
        Assert.Contains("mud-ex-ribbon-quick-access", cut.Find(".mud-ex-ribbon-header > .mud-ex-ribbon-title-bar").FirstElementChild!.ClassList);
        QuickAccess("Undo").Click();
        cut.WaitForAssertion(() => Assert.Equal(1, undos));

        cut.FindAll(".mud-ex-ribbon-button").First(e => e.GetAttribute("aria-label") == "Cut").ContextMenu();
        popovers.WaitForAssertion(() => Assert.Single(popovers.FindAll(".mud-ex-ribbon-context-menu-item")));
        Assert.Equal("Add to Quick Access Toolbar", popovers.Find(".mud-ex-ribbon-context-menu-item").TextContent.Trim());
        popovers.Find(".mud-ex-ribbon-context-menu-item").Click();

        cut.WaitForAssertion(() => Assert.Equal([["Home/Clipboard/Cut"]], raised));
        cut.WaitForAssertion(() => Assert.Equal(["Undo", "Cut"], cut.FindAll(".mud-ex-ribbon-quick-access .mud-ex-ribbon-button").Select(e => e.GetAttribute("aria-label"))));
        Assert.Equal(["Home/Clipboard/Cut"], cut.Instance.PinnedCommandIds);
        popovers.WaitForAssertion(() => Assert.Empty(popovers.FindAll(".mud-ex-ribbon-context-menu-item")));

        Assert.Throws<MissingEventHandlerException>(() => QuickAccess("Undo").ContextMenu());

        QuickAccess("Cut").ContextMenu();
        popovers.WaitForAssertion(() => Assert.Equal("Remove from Quick Access Toolbar", popovers.Find(".mud-ex-ribbon-context-menu-item").TextContent.Trim()));
        popovers.Find(".mud-ex-ribbon-context-menu-item").Click();
        cut.WaitForAssertion(() => Assert.Equal([["Home/Clipboard/Cut"], []], raised));
        cut.WaitForAssertion(() => Assert.Equal(["Undo"], cut.FindAll(".mud-ex-ribbon-quick-access .mud-ex-ribbon-button").Select(e => e.GetAttribute("aria-label"))));

        cut.Render(p => p.Add(x => x.PinnedCommandIds, ["Insert/Tables/Table", "missing/command", "Home/Clipboard/Paste/Keep Text Only"]));
        cut.WaitForAssertion(() => Assert.Equal(["Undo", "Table", "Keep Text Only"],
            cut.FindAll(".mud-ex-ribbon-quick-access .mud-ex-ribbon-button").Select(e => e.GetAttribute("aria-label"))));
        Assert.Equal(2, raised.Count);
    }

    private static readonly string[] SearchIds =
    [
        "Home/Font/Bold", "Home/Font/Italic", "Home/Font/Format", "Home/Font/Format/Strikethrough", "Home/Font/Format/Case", "Home/Font/Format/Case/UPPERCASE",
        "Home/Font/Size", "Home/Font/Hidden", "Design/Styles/Shading", "Secret/Group/Spy", "Insert/Tables/Table", "Insert/Tables/Table/Quick Table"
    ];

    private (IRenderedComponent<MudExRibbon> Ribbon, IRenderedComponent<MudPopoverProvider> Popovers) RenderSearch(
        List<string> executed, bool boldDisabled = false, bool designVisible = true)
    {
        var context = NewContext();
        var popovers = context.Render<MudPopoverProvider>();
        var cut = context.Render<MudExRibbon>(p => p
            .AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Home")
                .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Font")
                    .AddChildContent<MudExRibbonRow>(r => r
                        .AddChildContent<MudExRibbonToggleButton>(b => b.Add(x => x.Text, "Bold").Add(x => x.Description, "Make the selected text bold.")
                            .Add(x => x.Disabled, boldDisabled).Add(x => x.CheckedChanged, _ => executed.Add("Bold")))
                        .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Italic").Add(x => x.Tooltip, "Slanted text").Add(x => x.OnClick, () => executed.Add("Italic"))))
                    .AddChildContent<MudExRibbonMenuButton>(m => m.Add(x => x.Text, "Format")
                        .AddChildContent<MudExRibbonMenuItem>(i => i.Add(x => x.Text, "Strikethrough").Add(x => x.Description, "Draw a line through the selected text.")
                            .Add(x => x.OnClick, () => executed.Add("Strikethrough")))
                        .AddChildContent<MudExRibbonMenuItem>(i => i.Add(x => x.Text, "Case")
                            .AddChildContent<MudExRibbonMenuItem>(c => c.Add(x => x.Text, "UPPERCASE").Add(x => x.Description, "Turn the selected text into capitals.")
                                .Add(x => x.OnClick, () => executed.Add("UPPERCASE")))))
                    .AddChildContent<MudExRibbonNumberBox>(n => n.Add(x => x.Text, "Size").Add(x => x.Description, "Size of the selected text."))
                    .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Hidden").Add(x => x.Visible, false).Add(x => x.Description, "Hide the selected text."))))
            .AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Design").Add(x => x.Contextual, true).Add(x => x.Visible, designVisible)
                .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Styles")
                    .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Shading").Add(x => x.Description, "Shade the selected text.")
                        .Add(x => x.OnClick, () => executed.Add("Shading")))))
            .AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Secret").Add(x => x.Visible, false)
                .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Group")
                    .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Spy").Add(x => x.Description, "Spy on the selected text."))))
            .AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Insert")
                .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Tables")
                    .AddChildContent<MudExRibbonMenuButton>(m => m.Add(x => x.Text, "Table").Add(x => x.Id, "Insert/Tables/Table").Add(x => x.Split, true)
                        .Add(x => x.OnClick, () => executed.Add("Table"))
                        .AddChildContent<MudExRibbonMenuItem>(i => i.Add(x => x.Text, "Quick Table").Add(x => x.OnClick, () => executed.Add("Quick Table")))))));
        cut.WaitForAssertion(() => Assert.Equal(4, cut.Instance.Tabs.Count));
        return (cut, popovers);
    }

    private static List<AngleSharp.Dom.IElement> SearchResults(IRenderedComponent<MudExRibbon> cut, IRenderedComponent<MudPopoverProvider> popovers, string text)
    {
        cut.Find(".mud-ex-ribbon-command-search input").Input(text);
        var results = new List<AngleSharp.Dom.IElement>();
        popovers.WaitForAssertion(() =>
        {
            results = popovers.FindAll(".mud-ex-ribbon-search-result").ToList();
            Assert.NotEmpty(results);
        });
        return results;
    }

    private static void ClearSearch(IRenderedComponent<MudExRibbon> cut, IRenderedComponent<MudPopoverProvider> popovers)
    {
        cut.Find(".mud-ex-ribbon-command-search input").Input(string.Empty);
        popovers.WaitForAssertion(() => Assert.Empty(popovers.FindAll(".mud-ex-ribbon-search-result")));
    }

    private static string ResultText(AngleSharp.Dom.IElement result)
        => $"{result.QuerySelector(".mud-ex-ribbon-search-text")!.TextContent.Trim()} | {result.QuerySelector(".mud-ex-ribbon-search-path")!.TextContent.Trim()}";

    [Fact]
    public void CommandSearch_FindsByDescription_AndExecutes()
    {
        var executed = new List<string>();
        var (cut, popovers) = RenderSearch(executed);

        Assert.Equal(SearchIds, cut.Instance.Commands.Where(c => c is not MudExRibbonRow).Select(c => c.Id));
        Assert.Equal("Search commands", cut.Find(".mud-ex-ribbon-command-search input").GetAttribute("placeholder"));

        var results = SearchResults(cut, popovers, "SELECTED TEXT");
        Assert.Equal(["Bold | Home › Font", "Strikethrough | Home › Font › Format", "UPPERCASE | Home › Font › Format › Case", "Shading | Design › Styles"],
            results.Select(ResultText));

        results[2].Closest(".mud-list-item")!.Click();
        cut.WaitForAssertion(() => Assert.Equal(["UPPERCASE"], executed));
        cut.WaitForAssertion(() => Assert.Equal(string.Empty, cut.Find(".mud-ex-ribbon-command-search input").GetAttribute("value") ?? string.Empty));

        Assert.Empty(cut.Instance.SearchCommands("format"));
        Assert.Equal(["UPPERCASE | Home › Font › Format › Case"], SearchResults(cut, popovers, "case").Select(ResultText));
        ClearSearch(cut, popovers);
        Assert.Equal(["Table | Insert › Tables", "Quick Table | Insert › Tables › Table"], SearchResults(cut, popovers, "table").Select(ResultText));
        ClearSearch(cut, popovers);

        SearchResults(cut, popovers, "slanted").Single().Closest(".mud-list-item")!.Click();
        cut.WaitForAssertion(() => Assert.Equal(["UPPERCASE", "Italic"], executed));

        SearchResults(cut, popovers, "bold").Single().Closest(".mud-list-item")!.Click();
        cut.WaitForAssertion(() => Assert.Equal(["UPPERCASE", "Italic", "Bold"], executed));
        cut.WaitForAssertion(() => Assert.Equal("true", cut.FindAll(".mud-ex-ribbon-button").Single(e => e.GetAttribute("aria-label") == "Bold").GetAttribute("aria-pressed")));
    }

    [Fact]
    public void CommandSearch_SkipsDisabledCommands()
    {
        var executed = new List<string>();
        var (cut, popovers) = RenderSearch(executed, boldDisabled: true, designVisible: false);

        Assert.Equal(["Strikethrough", "UPPERCASE"], SearchResults(cut, popovers, "selected").Select(r => r.QuerySelector(".mud-ex-ribbon-search-text")!.TextContent.Trim()));

        cut.Render(p => p.Add(x => x.ShowCommandSearch, false));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-ribbon-command-search")));
        Assert.Empty(executed);
    }

    private static string[] Tips(IRenderedComponent<MudExRibbon> cut, string level)
        => cut.FindAll($"[data-mudex-keytip-level={level}]").Select(e => e.GetAttribute("data-mudex-keytip")!).ToArray();

    [Fact]
    public void KeyTips_AreUniquePerLevel()
    {
        Assert.Equal(["B", "O", "U"], MudExRibbonKeyTips.Assign([("Bold", null), ("Border", null), ("Bullets", null)]));
        Assert.Equal(["A1", "A2", "A3", "B"], MudExRibbonKeyTips.Assign([("A", null), ("A", null), ("a!", null), ("Ab", null)]));

        var (cut, _) = RenderModes(p => p.Add(x => x.FileMenu, "<div>Backstage</div>"));

        cut.WaitForAssertion(() => Assert.Equal(["F", "H", "I"], Tips(cut, "tab")));
        Assert.Equal("File", cut.Find("[data-mudex-keytip=F]").TextContent.Trim());
        Assert.Equal(["P", "C", "B", "I", "S"], Tips(cut, "item"));
        Assert.Equal("Paste", cut.Find("[data-mudex-keytip-level=item][data-mudex-keytip=P]").GetAttribute("aria-label"));

        cut.FindAll("[role=tab]")[1].Click();
        cut.WaitForAssertion(() => Assert.Equal(["T"], Tips(cut, "item")));

        cut.Render(p => p.Add(x => x.Mode, MudExRibbonMode.Toolbar));
        cut.WaitForAssertion(() => Assert.Equal(["T"], Tips(cut, "item")));
        cut.Render(p => p.Add(x => x.Mode, MudExRibbonMode.Menu));
        cut.WaitForAssertion(() => Assert.Equal(["F", "H", "I"], Tips(cut, "tab")));
        Assert.Empty(Tips(cut, "item"));
    }

    [Fact]
    public void ExplicitKeyTip_Wins()
    {
        Assert.Equal(["O", "B"], MudExRibbonKeyTips.Assign([("Bold", null), ("Border", "b")]));
        Assert.Equal(["O", "FS"], MudExRibbonKeyTips.Assign([("Font", null), ("Font Size", "fs")]));
        Assert.Equal(["X", "A1"], MudExRibbonKeyTips.Assign([("X", "x"), ("X", null)]));

        var cut = Render(p => p
            .AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Home").Add(x => x.KeyTip, "n")
                .AddChildContent<MudExRibbonGroup>(g => g.Add(x => x.Text, "Font")
                    .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Bold"))
                    .AddChildContent<MudExRibbonButton>(b => b.Add(x => x.Text, "Border").Add(x => x.KeyTip, "B"))))
            .AddChildContent<MudExRibbonTab>(t => t.Add(x => x.Text, "Next")));

        cut.WaitForAssertion(() => Assert.Equal(["N", "E"], Tips(cut, "tab")));
        Assert.Equal(["O", "B"], Tips(cut, "item"));

        cut.Render(p => p.Add(x => x.EnableKeyTips, false));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-mudex-keytip]")));
    }

    [Fact]
    public void Tabs_HaveTablistRolesAndAriaSelected()
    {
        var cut = Render(p => ContextualRibbon(p, true));

        cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll("[role=tablist] [role=tab]").Count));
        var tabs = cut.FindAll("[role=tab]");
        Assert.Equal(["true", "false", "false", "false", "false"], tabs.Select(e => e.GetAttribute("aria-selected")));
        Assert.Equal(["0", "-1", "-1", "-1", "-1"], tabs.Select(e => e.GetAttribute("tabindex")));

        var titles = cut.FindAll(".mud-ex-ribbon-contextual-title");
        Assert.All(titles, t => Assert.False(string.IsNullOrEmpty(t.Id)));
        Assert.Equal([null, null, titles[0].Id, titles[0].Id, titles[1].Id], tabs.Select(e => e.GetAttribute("aria-describedby")));

        tabs[3].Click();
        cut.WaitForAssertion(() => Assert.Equal(["false", "false", "false", "true", "false"], cut.FindAll("[role=tab]").Select(e => e.GetAttribute("aria-selected"))));
        Assert.Equal(["-1", "-1", "-1", "0", "-1"], cut.FindAll("[role=tab]").Select(e => e.GetAttribute("tabindex")));
    }

    [Fact]
    public void Groups_HaveToolbarRoleAndLabel()
    {
        var (cut, _) = RenderModes(_ => { });

        cut.WaitForAssertion(() => Assert.Equal(["Clipboard", "Font"], cut.FindAll(".mud-ex-ribbon-body [role=toolbar]").Select(e => e.GetAttribute("aria-label"))));
        Assert.Equal("-1", cut.Find(".mud-ex-ribbon-split-arrow").GetAttribute("tabindex"));
        Assert.Equal("-1", cut.Find(".mud-ex-ribbon-menu-face").GetAttribute("tabindex"));
        Assert.Null(cut.Find(".mud-ex-ribbon-split-face").GetAttribute("tabindex"));
        Assert.All(cut.FindAll(".mud-ex-ribbon-menu [aria-haspopup=menu]"), e => Assert.Equal("0", e.GetAttribute("tabindex")));

        cut.Render(p => p.Add(x => x.Mode, MudExRibbonMode.Simplified));
        cut.WaitForAssertion(() => Assert.Equal(["Clipboard", "Font"], cut.FindAll(".mud-ex-ribbon-body [role=toolbar]").Select(e => e.GetAttribute("aria-label"))));

        cut.Render(p => p.Add(x => x.Mode, MudExRibbonMode.Toolbar));
        cut.WaitForAssertion(() => Assert.Equal("Home", cut.Find(".mud-ex-ribbon-toolbar-items").GetAttribute("aria-label")));
        Assert.Equal("toolbar", cut.Find(".mud-ex-ribbon-toolbar-items").GetAttribute("role"));
        Assert.Equal(["Clipboard", "Font"], cut.FindAll(".mud-ex-ribbon-toolbar-group[role=group]").Select(e => e.GetAttribute("aria-label")));
    }

    private sealed class FrozenTime : TimeProvider
    {
        private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => _now;
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
