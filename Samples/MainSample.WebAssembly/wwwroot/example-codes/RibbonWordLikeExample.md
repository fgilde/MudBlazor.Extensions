```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Core
@inject ISnackbar Snackbar

<div style="position: relative; width: @Width; max-width: 100%; border: 1px solid var(--mud-palette-lines-default); overflow: hidden;">
    <MudExRibbon @ref="ComponentRef"
                 Mode="@Mode"
                 ModeChanged="@OnModeChanged"
                 Color="@Color"
                 ShowModeSwitch="@ShowModeSwitch"
                 ShowCommandSearch="@ShowCommandSearch"
                 EnableKeyTips="@EnableKeyTips"
                 AllowCollapse="@AllowCollapse"
                 @bind-Collapsed="_collapsed"
                 @bind-ActiveTabIndex="_activeTab"
                 @bind-PinnedCommandIds="_pinned"
                 @bind-FileMenuOpen="_fileMenuOpen"
                 FileMenuText="@L["File"]">
        <TitleContent>
            <MudText Typo="Typo.subtitle2" Class="px-2">@Title</MudText>
        </TitleContent>
        <HeaderEndContent>
            <MudText Typo="Typo.caption" Class="px-2">@Mode</MudText>
        </HeaderEndContent>
        <FileMenu>
            <MudStack Class="pa-6" Spacing="2">
                <MudText Typo="Typo.h6">@L["File"]</MudText>
                <MudButton Variant="Variant.Text" StartIcon="@Icons.Material.Filled.NoteAdd" OnClick="@(() => CloseFileMenu("New"))">@L["New"]</MudButton>
                <MudButton Variant="Variant.Text" StartIcon="@Icons.Material.Filled.FolderOpen" OnClick="@(() => CloseFileMenu("Open"))">@L["Open"]</MudButton>
                <MudButton Variant="Variant.Text" StartIcon="@Icons.Material.Filled.Save" OnClick="@(() => CloseFileMenu("Save"))">@L["Save"]</MudButton>
            </MudStack>
        </FileMenu>
        <QuickAccess>
            <MudExRibbonButton Id="qa-undo" Text="@L["Undo"]" Icon="@Icons.Material.Filled.Undo" Shortcut="Ctrl+Z" OnClick="@(() => Log("Undo"))" />
            <MudExRibbonButton Id="qa-redo" Text="@L["Redo"]" Icon="@Icons.Material.Filled.Redo" Shortcut="Ctrl+Y" OnClick="@(() => Log("Redo"))" />
        </QuickAccess>
        <ChildContent>
            <MudExRibbonTab Text="@L["Home"]" KeyTip="H">
                <MudExRibbonGroup Text="@L["Clipboard"]" Icon="@Icons.Material.Filled.ContentPaste" CollapsePriority="1" OnDialogLauncherClick="@(() => Log("Clipboard dialog launcher"))">
                    <MudExRibbonMenuButton Id="paste" Text="@L["Paste"]" Icon="@Icons.Material.Filled.ContentPaste" Split="true" Size="MudExRibbonItemSize.Large" KeyTip="V" Shortcut="Ctrl+V" Tooltip="@L["Paste from clipboard"]" OnClick="@(() => Log("Paste"))">
                        <MudExRibbonMenuItem Id="paste-keep" Text="@L["Keep source formatting"]" Icon="@Icons.Material.Filled.FormatPaint" OnClick="@(() => Log("Paste: keep source formatting"))" />
                        <MudExRibbonMenuItem Id="paste-text" Text="@L["Paste as text"]" Icon="@Icons.Material.Filled.TextFields" OnClick="@(() => Log("Paste: as text"))" />
                        <MudExRibbonMenuItem Id="paste-special" Text="@L["Paste special"]">
                            <MudExRibbonMenuItem Id="paste-html" Text="@L["HTML"]" OnClick="@(() => Log("Paste special: HTML"))" />
                            <MudExRibbonMenuItem Id="paste-image" Text="@L["Picture"]" OnClick="@(() => Log("Paste special: picture"))" />
                        </MudExRibbonMenuItem>
                    </MudExRibbonMenuButton>
                    <MudExRibbonButton Id="cut" Text="@L["Cut"]" Icon="@Icons.Material.Filled.ContentCut" Size="MudExRibbonItemSize.Small" Shortcut="Ctrl+X" OnClick="@(() => Log("Cut"))" />
                    <MudExRibbonButton Id="copy" Text="@L["Copy"]" Icon="@Icons.Material.Filled.ContentCopy" Size="MudExRibbonItemSize.Small" Shortcut="Ctrl+C" OnClick="@(() => Log("Copy"))" />
                </MudExRibbonGroup>

                <MudExRibbonGroup Text="@L["Font"]" Icon="@Icons.Material.Filled.FormatSize" CollapsePriority="3">
                    <MudExRibbonRow>
                        <MudExRibbonSelect T="string" Text="@L["Font"]" Items="@_fonts" Value="@_font" ValueChanged="@OnFontChanged" Width="150" />
                        <MudExRibbonNumberBox Text="@L["Font size"]" Value="@_size" ValueChanged="@OnSizeChanged" Min="6" Max="96" Step="1" Unit="pt" />
                    </MudExRibbonRow>
                    <MudExRibbonRow>
                        <MudExRibbonToggleButton Id="bold" Text="@L["Bold"]" Icon="@Icons.Material.Filled.FormatBold" Shortcut="Ctrl+B" Size="MudExRibbonItemSize.Small" Checked="@_bold" CheckedChanged="@(v => OnToggle("Bold", v, x => _bold = x))" />
                        <MudExRibbonToggleButton Id="italic" Text="@L["Italic"]" Icon="@Icons.Material.Filled.FormatItalic" Shortcut="Ctrl+I" Size="MudExRibbonItemSize.Small" Checked="@_italic" CheckedChanged="@(v => OnToggle("Italic", v, x => _italic = x))" />
                        <MudExRibbonToggleButton Id="underline" Text="@L["Underline"]" Icon="@Icons.Material.Filled.FormatUnderlined" Shortcut="Ctrl+U" Size="MudExRibbonItemSize.Small" Checked="@_underline" CheckedChanged="@(v => OnToggle("Underline", v, x => _underline = x))" />
                        <MudExRibbonSeparator />
                        <MudExRibbonColorPicker Id="font-color" Text="@L["Font color"]" Icon="@Icons.Material.Filled.FormatColorText" Size="MudExRibbonItemSize.Small" Value="@_fontColor" ValueChanged="@OnFontColorChanged" />
                        <MudExRibbonColorPicker Id="highlight" Text="@L["Highlight"]" Icon="@Icons.Material.Filled.FormatColorFill" Size="MudExRibbonItemSize.Small" Value="@_highlight" ValueChanged="@OnHighlightChanged" />
                    </MudExRibbonRow>
                </MudExRibbonGroup>

                <MudExRibbonGroup Text="@L["Styles"]" Icon="@Icons.Material.Filled.Style" CollapsePriority="2">
                    <MudExRibbonGallery TItem="string" Items="@_styles" SelectedItem="@_style" SelectedItemChanged="@OnStyleChanged" VisibleRows="2" Columns="3" Text="@L["Styles"]" />
                </MudExRibbonGroup>
            </MudExRibbonTab>

            <MudExRibbonTab Text="@L["Insert"]" KeyTip="N">
                <MudExRibbonGroup Text="@L["Illustrations"]" Icon="@Icons.Material.Filled.Image" CollapsePriority="2">
                    <MudExRibbonButton Id="picture" Text="@L["Picture"]" Icon="@Icons.Material.Filled.Image" Size="MudExRibbonItemSize.Large" KeyTip="P" Tooltip="@L["Insert a picture"]" OnClick="@(() => Log("Insert picture"))" />
                    <MudExRibbonButton Id="shapes" Text="@L["Shapes"]" Icon="@Icons.Material.Filled.Category" OnClick="@(() => Log("Insert shapes"))" />
                    <MudExRibbonButton Id="chart" Text="@L["Chart"]" Icon="@Icons.Material.Filled.BarChart" OnClick="@(() => Log("Insert chart"))" />
                </MudExRibbonGroup>
                <MudExRibbonGroup Text="@L["Tables"]" Icon="@Icons.Material.Filled.TableChart" CollapsePriority="1">
                    <MudExRibbonButton Id="table" Text="@L["Table"]" Icon="@Icons.Material.Filled.TableChart" Size="MudExRibbonItemSize.Large" OnClick="@(() => Log("Insert table"))" />
                </MudExRibbonGroup>
                <MudExRibbonGroup Text="@L["Links"]" Icon="@Icons.Material.Filled.Link" CollapsePriority="3">
                    <MudExRibbonButton Id="link" Text="@L["Link"]" Icon="@Icons.Material.Filled.Link" Size="MudExRibbonItemSize.Large" Shortcut="Ctrl+K" OnClick="@(() => Log("Insert link"))" />
                    <MudExRibbonButton Id="bookmark" Text="@L["Bookmark"]" Icon="@Icons.Material.Filled.Bookmark" Size="MudExRibbonItemSize.Small" OnClick="@(() => Log("Insert bookmark"))" />
                    <MudExRibbonButton Id="cross-reference" Text="@L["Cross-reference"]" Icon="@Icons.Material.Filled.CompareArrows" Size="MudExRibbonItemSize.Small" Disabled="true" />
                </MudExRibbonGroup>
            </MudExRibbonTab>

            <MudExRibbonTab Text="@L["View"]" KeyTip="W">
                <MudExRibbonGroup Text="@L["Show"]" Icon="@Icons.Material.Filled.Visibility" CollapsePriority="2">
                    <MudExRibbonToggleButton Id="ruler" Text="@L["Ruler"]" Icon="@Icons.Material.Filled.Straighten" Checked="@_ruler" CheckedChanged="@(v => OnToggle("Ruler", v, x => _ruler = x))" />
                    <MudExRibbonToggleButton Id="gridlines" Text="@L["Gridlines"]" Icon="@Icons.Material.Filled.GridOn" Checked="@_gridlines" CheckedChanged="@(v => OnToggle("Gridlines", v, x => _gridlines = x))" />
                </MudExRibbonGroup>
                <MudExRibbonGroup Text="@L["Zoom"]" Icon="@Icons.Material.Filled.ZoomIn" CollapsePriority="1">
                    <MudExRibbonNumberBox Text="@L["Zoom"]" Value="@_zoom" ValueChanged="@OnZoomChanged" Min="10" Max="500" Step="10" Unit="%" />
                    <MudExRibbonButton Id="zoom-100" Text="@L["Zoom 100%"]" Icon="@Icons.Material.Filled.ZoomInMap" OnClick="@(() => OnZoomChanged(100))" />
                </MudExRibbonGroup>
                <MudExRibbonGroup Text="@L["Custom content"]" Icon="@Icons.Material.Filled.Widgets" CollapsePriority="4">
                    <MudExRibbonContent>
                        <MudChip T="string" Size="Size.Small" OnClick="@(() => Log("Custom chip"))">@L["Any markup"]</MudChip>
                    </MudExRibbonContent>
                </MudExRibbonGroup>
            </MudExRibbonTab>

            <MudExRibbonTab Text="@L["Picture format"]" KeyTip="J" Visible="@PictureSelected" Contextual="true" ContextualTitle="@L["Picture tools"]" ContextualColor="@ContextualColor">
                <MudExRibbonGroup Text="@L["Adjust"]" Icon="@Icons.Material.Filled.Tune">
                    <MudExRibbonButton Id="crop" Text="@L["Crop"]" Icon="@Icons.Material.Filled.Crop" Size="MudExRibbonItemSize.Large" OnClick="@(() => Log("Crop picture"))" />
                    <MudExRibbonButton Id="rotate" Text="@L["Rotate"]" Icon="@Icons.Material.Filled.RotateRight" Size="MudExRibbonItemSize.Large" OnClick="@(() => Log("Rotate picture"))" />
                    <MudExRibbonButton Id="remove-bg" Text="@L["Remove background"]" Icon="@Icons.Material.Filled.Layers" OnClick="@(() => Log("Remove background"))" />
                </MudExRibbonGroup>
            </MudExRibbonTab>
        </ChildContent>
    </MudExRibbon>

    <div class="pa-4" style="min-height: 120px;">
        <MudText Typo="Typo.caption" Color="MudBlazor.Color.Secondary">@L["Last action: {0}", _last]</MudText>
        <MudText Style="@DocumentStyle">@L["The quick brown fox jumps over the lazy dog."]</MudText>
        <MudText Typo="Typo.caption" Class="d-block mt-2">@L["Style: {0} | Active tab: {1} | Collapsed: {2} | Zoom: {3}%", _style, _activeTab, _collapsed, _zoom]</MudText>
        <MudText Typo="Typo.caption" Class="d-block">@L["Pinned: {0}", string.Join(", ", _pinned ?? new List<string>())]</MudText>
    </div>
</div>

@code {
    [Parameter] public MudExRibbonMode Mode { get; set; } = MudExRibbonMode.Classic;
    [Parameter] public EventCallback<MudExRibbonMode> ModeChanged { get; set; }
    [Parameter] public MudExColor Color { get; set; } = MudBlazor.Color.Primary;
    [Parameter] public MudExColor ContextualColor { get; set; } = "#C43E1C";
    [Parameter] public string Title { get; set; } = "Document1.docx";
    [Parameter] public string Width { get; set; } = "100%";
    [Parameter] public bool PictureSelected { get; set; }
    [Parameter] public bool ShowModeSwitch { get; set; } = true;
    [Parameter] public bool ShowCommandSearch { get; set; } = true;
    [Parameter] public bool EnableKeyTips { get; set; } = true;
    [Parameter] public bool AllowCollapse { get; set; } = true;
    [Parameter] public bool StartCollapsed { get; set; }
    [Parameter] public bool ShowSnackbar { get; set; }

    private static readonly string[] _fonts = ["Calibri", "Arial", "Times New Roman", "Courier New", "Segoe UI"];
    private static readonly string[] _styles = ["Normal", "Heading 1", "Heading 2", "Title", "Subtitle", "Quote"];

    private bool _collapsed;
    private int _activeTab;
    private bool _fileMenuOpen;
    private IList<string> _pinned = new List<string> { "bold" };
    private string _font = "Calibri";
    private double _size = 11;
    private double _zoom = 100;
    private string _style = "Normal";
    private bool _bold, _italic, _underline, _ruler = true, _gridlines;
    private MudExColor _fontColor = "#217346";
    private MudExColor _highlight = "#FFF2CC";
    private string _last = "-";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _collapsed = StartCollapsed;
    }

    private string DocumentStyle => $"font-family: {_font}; font-size: {_size}pt; font-weight: {(_bold ? "bold" : "normal")}; font-style: {(_italic ? "italic" : "normal")}; text-decoration: {(_underline ? "underline" : "none")}; color: {_fontColor.ToCssStringValue()}; background: {_highlight.ToCssStringValue()};";

    private void Log(string message)
    {
        _last = message;
        if (ShowSnackbar)
            Snackbar.Add(message, Severity.Info);
        StateHasChanged();
    }

    private async Task OnModeChanged(MudExRibbonMode mode)
    {
        Mode = mode;
        Log($"Mode: {mode}");
        await ModeChanged.InvokeAsync(mode);
    }

    private void OnToggle(string name, bool value, Action<bool> set)
    {
        set(value);
        Log($"{name} {(value ? "on" : "off")}");
    }

    private void OnFontChanged(string font) { _font = font; Log($"Font: {font}"); }
    private void OnSizeChanged(double size) { _size = size; Log($"Font size: {size}pt"); }
    private void OnZoomChanged(double zoom) { _zoom = zoom; Log($"Zoom: {zoom}%"); }
    private void OnStyleChanged(string style) { _style = style; Log($"Style: {style}"); }
    private void OnFontColorChanged(MudExColor color) { _fontColor = color; Log($"Font color: {color.ToCssStringValue()}"); }
    private void OnHighlightChanged(MudExColor color) { _highlight = color; Log($"Highlight: {color.ToCssStringValue()}"); }

    private void CloseFileMenu(string action)
    {
        _fileMenuOpen = false;
        Log($"File: {action}");
    }
}

```
