using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.Base;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

public partial class MudExSplitter : IJsMudExComponent<MudExSplitter>
{
    private string _dataId = Guid.NewGuid().ToFormattedId();
    private const string ClassName = "mud-ex-splitter";
    private RenderFragment Inherited() => builder =>
    {
        base.BuildRenderTree(builder);
    };

    public IJSObjectReference JsReference { get; set; }
    public IJSObjectReference ModuleReference { get; set; }
    public ElementReference ElementReference { get; set; }
    
    private IJsMudExComponent<MudExSplitter> AsJsComponent => this;

    
    protected override Task OnInitializedAsync()
    {
        (UserAttributes ??= new Dictionary<string,object>()).AddOrUpdate("data-id", _dataId);
        Color = Color.Primary;
        Size = 5;
        return base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        Class = $"{ClassName} {Class}";
        if (AsJsComponent.JsReference != null)
            await AsJsComponent.JsReference.InvokeVoidAsync("initialize", Options());
    }

    public virtual object[] GetJsArguments() => new[] { AsJsComponent.ElementReference, AsJsComponent.CreateDotNetObjectReference(), Options() };

    private object Options()
    {
        return new
        {
            Id = _dataId,
            Style = GetStyle(),
            VerticalSplit = !Vertical // Splitter is vertical so container is horizontal
        };
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
            await AsJsComponent.ImportModuleAndCreateJsAsync();
    }

    public ValueTask DisposeAsync()
    {
        return AsJsComponent.DisposeModulesAsync();
    }
}