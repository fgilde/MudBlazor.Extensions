using BlazorJS;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;

namespace MainSample.WebAssembly.Shared;

public partial class CommentsFooter
{
    private bool _isExpanded;
    private bool _closing;
    private bool _available = true;
    private bool _dragging;
    private ElementReference _footer;

    [Parameter]
    public bool Available
    {
        get => _available;
        set
        {
            if (_available == value)
                return;
            _available = value;
            AvailableChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<bool> AvailableChanged { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (MainLayout.Instance != null)
            MainLayout.Instance.LanguageChanged += (_, _) => InvokeAsync(StateHasChanged);
    }
    private async Task Close()
    {
        _closing = true;
        await InvokeAsync(StateHasChanged);
        await Unload();
        await Task.Delay(800);
        Available = false;
        _closing = false;
        _isExpanded = false;
    }

    private async Task Unload()
    {
        await JsRuntime.UnloadFilesAsync(Disqus.DisqusSrc);
        await JsRuntime.InvokeVoidAsync("eval", $"delete window['{Disqus.DisqusNamespace}']");
        //await LocalStorageService.RemoveItemAsync(Disqus.DisqusAutoLoadStorageKey);
    }
    
    private string ClassStr() => MudExCssBuilder.From("mud-appbar mud-appbar-fixed-bottom")
        .AddClass("mud-ex-fade-out-500ms", _closing)
        .AddClass("mud-ex-animate-all-properties", !_closing && !_dragging)
        .AddClass("comment-footer-collapsed", !_isExpanded)
        .AddClass("comment-footer-expanded", _isExpanded)
        .Build();
}