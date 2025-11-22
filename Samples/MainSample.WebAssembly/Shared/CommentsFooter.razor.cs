using BlazorJS;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;

namespace MainSample.WebAssembly.Shared;

public partial class CommentsFooter
{
    private bool _selfActivated;
    private bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                if (AlwaysClose && !_isExpanded)
                    _ = Close();
            }
        }
    }

    private bool _closing;
    private bool _available = true;
    private bool _dragging;
    private ElementReference _footer;
    private Disqus _disqus;
    private bool _isExpanded;

    [Parameter] public bool SimpleMode { get; set; }
    [Parameter] public bool AlwaysClose { get; set; }

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
        _selfActivated = false;
        await InvokeAsync(StateHasChanged);
        await Unload();
        await Task.Delay(400);

        if(!SimpleMode)
            Available = false;
        _closing = false;
        IsExpanded = false;
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
        .AddClass("comment-footer-collapsed", !IsExpanded)
        .AddClass("comment-footer-expanded", IsExpanded)
        .Build();

    private async Task OpenComments()
    {
        try
        {
            _selfActivated = true;
            Available = true;
            await InvokeAsync(StateHasChanged);
            await Task.Delay(100);
            await _disqus.LoadDisqusIfNotLoadedAsync();
        }
        catch (Exception e)
        {
        }
        IsExpanded = true;
    }

    private bool IsAvailable()
    {
        if (SimpleMode)
            return _selfActivated;
        return Available;
    }
}