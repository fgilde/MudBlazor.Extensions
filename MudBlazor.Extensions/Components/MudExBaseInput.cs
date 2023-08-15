using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// MudExBaseInput
/// </summary>
public abstract class MudExBaseInput<T> : MudBaseInput<T>
{

    /// <summary>
    /// The Adornment if used. By default, it is set to None.
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.FormComponent.Behavior)]
    public RenderFragment AdornmentStart { get; set; }

    /// <summary>
    /// The Adornment if used. By default, it is set to None.
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.FormComponent.Behavior)]
    public RenderFragment AdornmentEnd { get; set; }

    /// <summary>
    /// ForceShrink
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.FormComponent.Behavior)]
    public bool ForceShrink { get; set; }


    /// <summary>
    /// ChildContentStyle
    /// </summary>
    [Parameter]
    public string ChildContentStyle { get; set; }

    internal virtual InputType GetInputType() => InputType.Text;

    /// <summary>
    /// SkipUpdateProcessOnSetParameters
    /// </summary>
    protected virtual bool SkipUpdateProcessOnSetParameters { get; set; }


    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (SkipUpdateProcessOnSetParameters)        
            return;       
        await base.SetParametersAsync(parameters);

    }

}
