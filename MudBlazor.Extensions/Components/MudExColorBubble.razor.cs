using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;
using MudBlazor.Utilities;


namespace MudBlazor.Extensions.Components;

public partial class MudExColorBubble
{
    [Inject] private IJSRuntime _jsRuntime { get; set; }
    
    [Parameter] public int MinLuminance { get; set; } = 17;
    [Parameter] public int MaxLuminance { get; set; } = 86;
    [Parameter] public bool ShowColorPreview { get; set; } = true;
    [Parameter] public int SelectorSize { get; set; } = 161;
    [Parameter] public MudColor Color { get; set; } = new MudColor("#ff0000");


    private IJSObjectReference _jsReference;
    private ElementReference _elementReference;
    private ElementReference _canvasContainerReference;
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        MudColorPicker p;
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            var references = await _jsRuntime.ImportModuleAndCreateJsAsync<MudExColorBubble>(_elementReference, _canvasContainerReference, DotNetObjectReference.Create(this), Options());
            _jsReference = references.jsObjectReference;
            await _jsReference.InvokeVoidAsync("init");
        }
    }

    private string Style()
    {
        return $"background-color: {Color}; border-color: pink";
    }

    private string CanvasContainerStyle()
    {
        return $"border-radius: {(ShowColorPreview ? "0" : "100%")}; width: {SelectorSize}px; height: {SelectorSize}px";

    }

    private object Options()
    {
        return new {
            MinLuminance,
            MaxLuminance,
            ShowColorPreview,
            SelectorSize
        };
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
       
    }

}