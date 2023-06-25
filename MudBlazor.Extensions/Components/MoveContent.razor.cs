using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component to move the ChildContent to a specific element that can defined by ElementSelector.
/// </summary>
public partial class MoveContent
{
    /// <summary>
    /// ChildContent
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Content to render only if <see cref="ElementSelector"/> found
    /// </summary>
    [Parameter] public RenderFragment Found { get; set; }

    /// <summary>
    /// Content to render only if <see cref="ElementSelector"/> not found
    /// </summary>
    [Parameter] public RenderFragment NotFound { get; set; }

    /// <summary>
    /// Element selector to move content to
    /// </summary>
    [Parameter] public string ElementSelector { get; set; }

    /// <summary>
    /// Mode how to move the content you can MoveToSelector or move from MoveFromSelector
    /// </summary>
    [Parameter] public MoveContentMode Mode { get; set; } = MoveContentMode.MoveToSelector;

    /// <summary>
    /// Position to move the ChildContent to
    /// </summary>
    [Parameter] public MoveContentPosition Position { get; set; } = MoveContentPosition.AfterEnd;

    /// <summary>
    /// Returns true if element is found
    /// </summary>
    public bool ElementFound { get; private set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        ElementFound = await JsRuntime.InvokeAsync<object>("document.querySelector", ElementSelector) != null;
    }

    /// <inheritdoc />
    public override async Task ImportModuleAndCreateJsAsync()
    {
        await base.ImportModuleAndCreateJsAsync();
        await JsReference.InvokeVoidAsync("move", ElementSelector, Mode.ToString(), Position.ToString());
    }

}