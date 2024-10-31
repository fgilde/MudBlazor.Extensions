using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

public partial class MudExPicker
{
    /// <summary>
    /// Child content of the picker
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    /// <inheritdoc />
    protected override RenderFragment PickerContent => ChildContent;
}