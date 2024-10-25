using BlazorJS;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Interop;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// An item that only renders the content if it is in the viewport.
/// </summary>
public partial class MudExVirtualItem
{
    private ElementReference _container;
    private bool _isVisible = true;
    private DotNetObjectReference<MudExVirtualItem> _objectReference;
    private string _style = "";

    /// <summary>
    /// Is invoked when the visibility of the element changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> IsVisibleChanged { get; set; }
    /// <summary>
    /// Child content of component.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// this content is rendered when element is not into view.
    /// </summary>
    [Parameter]
    public RenderFragment NotIntersectingContent { get; set; }

    /// <summary>
    /// If this is false the <see cref="ChildContent"/>> will always be rendered.
    /// </summary>
    [Parameter]
    public bool Virtualize { get; set; } = true;
    
    /// <summary>
    /// The css class that will be applied to the container.
    /// </summary>
    [Parameter]
    public CssAndStyleApplyMode CssAndStyleApplyMode { get; set; } = CssAndStyleApplyMode.BeforeContentRendered;

    /// <summary>
    /// the class that will be applied to the container if the content is not intersecting.
    /// </summary>
    [Parameter]
    public string NotIntersectingClass { get; set; }

    /// <summary>
    /// the class that will be applied to the container if the content is intersecting.
    /// </summary>
    [Parameter]
    public string IntersectingClass { get; set; }

    /// <summary>
    /// the style that will be applied to the container if the content is not intersecting.
    /// </summary>
    [Parameter]
    public string NotIntersectingStyle { get; set; }

    /// <summary>
    /// the style that will be applied to the container if the content is intersecting.
    /// </summary>
    [Parameter]
    public string IntersectingStyle { get; set; }



    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await UpdateStyle();
            _objectReference = DotNetObjectReference.Create(this);
            await JsRuntime.InvokeVoidAsync("MudExObserver.observeVisibility", _container, _objectReference);
        }
    }

    private async Task UpdateStyle()
    {
        var size = await JsRuntime.DInvokeAsync<BoundingClientRect>((w, e) => e?.getBoundingClientRect(), _container);
        _style = MudExStyleBuilder.Default.WithSize(size.ToDimension(CssUnit.Pixels)).AddRaw(Style).ToString();
    }


    [JSInvokable]
    public async Task OnVisibilityChanged(bool isVisible)
    {
        await IsVisibleChanged.InvokeAsync(isVisible);
        _isVisible = isVisible;
        if (Virtualize)
            await InvokeAsync(StateHasChanged);
    }

    public override async ValueTask DisposeAsync()
    {
        await JsRuntime.InvokeVoidAsync("MudExObserver.unObserveVisibility", _container);
        await base.DisposeAsync();
    }

}