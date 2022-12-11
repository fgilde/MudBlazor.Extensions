using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Utilities;


namespace MudBlazor.Extensions.Components;

public partial class MudExColorBubble
{
    [Inject] private IJSRuntime _jsRuntime { get; set; }
    [Inject] private IServiceProvider _serviceProvider { get; set; }
    
    private IDialogService _dialogService => _serviceProvider.GetService<IDialogService>();
    private IStringLocalizer _localizer => Localizer ?? _serviceProvider.GetService<IStringLocalizer<MudExColorBubble>>() ?? _serviceProvider.GetService<IStringLocalizer>();

    [Parameter] 
    public IStringLocalizer Localizer { get; set; }

    [Parameter] public string Style { get; set; }
    [Parameter] public string Class { get; set; }

    [Parameter] 
    public string SelectColorText { get; set; } = "Select color";

    [Parameter]
    public bool ShowColorPreview
    {
        get => _showColorPreview;
        set
        {
            _showColorPreview = value;
            UpdateJsOptions();
        }
    }

    [Parameter]
    public bool AllowSelectOnPreviewClick
    {
        get => _allowSelectOnPreviewClick;
        set
        {
            _allowSelectOnPreviewClick = value;
            UpdateJsOptions();
        }
    }

    [Parameter]
    public bool CloseAfterSelect
    {
        get => _closeAfterSelect;
        set
        {
            _closeAfterSelect = value;
            UpdateJsOptions();
        }
    }

    [Parameter]
    public int MinLuminance
    {
        get => _minLuminance;
        set
        {
            _minLuminance = Math.Max(0, Math.Min(value, 100));
            UpdateJsOptions();
        }
    }

    [Parameter]
    public int MaxLuminance
    {
        get => _maxLuminance;
        set
        {
            _maxLuminance = Math.Max(0, Math.Min(value, 100)); ;
            UpdateJsOptions();
        }
    }
    
    [Parameter]
    public int SelectorSize
    {
        get => _selectorSize;
        set
        {
            _selectorSize = value;
            UpdateJsOptions();
        }
    }
    
    [Parameter] 
    public EventCallback<MudColor> ColorChanged { get; set; }
    
    [Parameter]
    public MudColor Color
    {
        get => _color;
        set
        {
            _color = value;
            UpdateJsOptions();
        }
    }

    [Parameter] 
    public int Height { get; set; } = 16;

    [Parameter] 
    public int Width { get; set; } = 16;

    private IJSObjectReference _jsReference;
    private ElementReference _elementReference;
    private ElementReference _canvasContainerReference;
    private MudColor _color;
    private int _selectorSize = 161;
    private bool _showColorPreview = true;
    private int _maxLuminance = 86;
    private int _minLuminance = 17;
    private bool _allowSelectOnPreviewClick = true;
    private bool _closeAfterSelect = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            var references = await _jsRuntime.ImportModuleAndCreateJsAsync<MudExColorBubble>(_elementReference, _canvasContainerReference, DotNetObjectReference.Create(this), Options());
            _jsReference = references.jsObjectReference;
            await _jsReference.InvokeVoidAsync("init");
        }
    }

    [JSInvokable]
    public async Task OnColorChanged(string color)
    {
        Color = new MudColor(color);
        await ColorChanged.InvokeAsync(Color);
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnColorPreviewClick()
    {
        string title = _localizer.TryLocalize(SelectColorText);
        var res = await _dialogService?.ShowComponentInDialogAsync<MudExColorPicker>(title, "",
            new Dictionary<string, object>()
            {
                {nameof(MudColorPicker.PickerVariant), PickerVariant.Static },
                {nameof(MudColorPicker.DisableAlpha), true },
                {nameof(MudColorPicker.DisableToolbar), false }
            },
            dialog =>
            {
                dialog.Icon = Icons.Material.Filled.ColorLens;
                dialog.Buttons = MudExDialogResultAction.OkCancel();
            }, new DialogOptionsEx
            {
                Animation = AnimationType.FlipY,
                DragMode = MudDialogDragMode.Simple,
                ShowAtCursor = true,
                CloseButton = true
            }
            );
        if (!res.DialogResult.Cancelled)
        {
            Color = res.Component.Value;
            await ColorChanged.InvokeAsync(Color);
            StateHasChanged();
        }
    }


    private string StyleStr() => $"background-color: {Color}; height: {Height}px; width: {Width}px; {Style}";

    private string CanvasContainerStyle() => $"border-radius: {(ShowColorPreview ? "0" : "100%")}; width: {SelectorSize}px; height: {SelectorSize}px";

    private void UpdateJsOptions()
    {
        if (_jsReference != null)
            _jsReference.InvokeVoidAsync("setOptions", Options());
    }

    private object Options()
    {
        _selectorSize = _selectorSize % 2 != 0 ? _selectorSize : _selectorSize + 1;
        var color = Color.ToString(MudColorOutputFormats.Hex);
        return new {
            Color = color, // Color on js needs to be a string
            MinLuminance,
            CloseAfterSelect,
            AllowSelectOnPreviewClick,
            MaxLuminance,
            ShowColorPreview,
            SelectorSize
        };
    }

    private async Task OnClick()
    {
        await _jsReference.InvokeVoidAsync("showSelector");
    }
}