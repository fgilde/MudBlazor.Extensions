using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Utilities;
using OneOf;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component to move the ChildContent to a specific element that can defined by ElementSelector.
/// </summary>
public partial class MoveContent
{
    /// <summary>
    /// ChildContent
    /// </summary>
    [Parameter]
    [SafeCategory("Common")]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Content to render only if <see cref="ElementSelector"/> found
    /// </summary>
    [Parameter]
    [SafeCategory("Data")]
    public RenderFragment Found { get; set; }

    /// <summary>
    /// Content to render only if <see cref="ElementSelector"/> not found
    /// </summary>
    [Parameter]
    [SafeCategory("Data")]
    public RenderFragment NotFound { get; set; }

    /// <summary>
    /// Element selector's to move content to.
    /// If you set an array of selectors the first found element will be used.
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public OneOf<string, string[]> ElementSelector { get; set; }

    /// <summary>
    /// Mode how to move the content you can MoveToSelector or move from MoveFromSelector
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public MoveContentMode Mode { get; set; } = MoveContentMode.MoveToSelector;

    /// <summary>
    /// Position to move the ChildContent to
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public MoveContentPosition Position { get; set; } = MoveContentPosition.AfterEnd;

    /// <summary>
    /// Owner element to search for the element defined by <see cref="ElementSelector"/>
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public ElementReference? QueryOwner { get; set; }

    /// <summary>
    /// If this is true the QueryOwner (or the parent of this element)'s parent will be used as query owner.
    /// Instead of true or false you can also set a number to search the parent of the QueryOwner n-times.
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public OneOf<bool, int> QueryFromParent { get; set; } = false;

    /// <summary>
    /// Returns true if element is found
    /// </summary>
    public bool? ElementFound { get; private set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
       // ElementFound = await JsRuntime.InvokeAsync<object>("document.querySelector", ElementSelector) != null;
    }

    /// <summary>
    /// Invoked when the element is found or not found
    /// </summary>
    [JSInvokable]
    public void ElementFoundChanged(bool found)
    {
        ElementFound = found;
        InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    public override async Task ImportModuleAndCreateJsAsync()
    {
        await base.ImportModuleAndCreateJsAsync();
        await JsReference.InvokeVoidAsync("move", ElementSelector.IsT0 ? ElementSelector.AsT0 : ElementSelector.AsT1, 
            Mode.ToString(), 
            Position.ToString(), 
            QueryOwner, 
            QueryFromParent.IsT0 ? QueryFromParent.AsT0 : QueryFromParent.AsT1 );
    }

}