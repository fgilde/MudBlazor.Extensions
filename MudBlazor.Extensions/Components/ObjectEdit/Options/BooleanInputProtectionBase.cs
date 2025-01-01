using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <summary>
/// Base class for a boolean input protection instance, providing a set of properties and methods to configure the boolean input protection.
/// </summary>
public abstract class BooleanInputProtectionBase<TComponent> : IConfirmationProtection where TComponent : MudBooleanInput<bool>, new()
{
    /// <summary>
    /// Constructor for a boolean input protection instance.
    /// </summary>
    /// <param name="tooltip"></param>
    /// <param name="configure"></param>
    protected BooleanInputProtectionBase(string tooltip = null, Action<TComponent>? configure = null)
    {
        AdditionalRenderData = RenderData.For<TComponent>(cb =>
        {
            cb.ValueChanged = EventCallback.Factory.Create<bool>(new object(), b => ConfirmationCallback?.Invoke(b));
            configure?.Invoke(cb);
        });
        if (!string.IsNullOrWhiteSpace(tooltip))
        {
            AdditionalRenderData.OnRendered<TComponent>(cb => cb.UserAttributes.Add("title", tooltip));
        }
    }
    /// <inheritdoc />
    public IRenderData AdditionalRenderData { get; set; }

    /// <inheritdoc />
    public Action<bool>? ConfirmationCallback { get; set; }
}