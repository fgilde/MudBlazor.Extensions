```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Core

<MudExRibbon @ref="ComponentRef" Mode="@_mode" ModeChanged="@(m => _mode = m)" ShowCommandSearch="false">
    <MudExRibbonTab Text="@L["Design"]" KeyTip="D">
        <MudExRibbonGroup Text="@L["Themes"]" Icon="@Icons.Material.Filled.Palette">
            <MudExRibbonGallery TItem="Swatch" Text="@L["Themes"]" Items="@_swatches" @bind-SelectedItem="_swatch" ItemText="@(s => s.Name)" VisibleRows="2" Columns="4">
                <ItemTemplate Context="swatch">
                    <div style="width: 36px; height: 24px; border-radius: 4px; background: linear-gradient(135deg, @swatch.From, @swatch.To);" title="@swatch.Name"></div>
                </ItemTemplate>
            </MudExRibbonGallery>
        </MudExRibbonGroup>
        <MudExRibbonGroup Text="@L["Text only gallery"]" Icon="@Icons.Material.Filled.TextFormat">
            <MudExRibbonGallery TItem="string" Text="@L["Layouts"]" Items="@_layouts" @bind-SelectedItem="_layout" VisibleRows="1" Columns="3" />
        </MudExRibbonGroup>
    </MudExRibbonTab>
</MudExRibbon>

<MudText Typo="Typo.body2" Class="pa-4">@L["Selected theme: {0} | Selected layout: {1}", _swatch.Name, _layout]</MudText>

@code {
    private record Swatch(string Name, string From, string To);

    private static readonly Swatch[] _swatches =
    [
        new("Ocean", "#0077B6", "#90E0EF"),
        new("Forest", "#2D6A4F", "#95D5B2"),
        new("Sunset", "#C43E1C", "#FFB703"),
        new("Violet", "#7719AA", "#E0AAFF"),
        new("Slate", "#343A40", "#ADB5BD"),
        new("Rose", "#B5179E", "#F72585"),
        new("Gold", "#BF9000", "#FFE066"),
        new("Mint", "#2A9D8F", "#B7E4C7")
    ];
    private static readonly string[] _layouts = ["Compact", "Normal", "Wide"];

    private MudExRibbonMode _mode = MudExRibbonMode.Classic;
    private Swatch _swatch = _swatches[0];
    private string _layout = "Normal";
}

```
