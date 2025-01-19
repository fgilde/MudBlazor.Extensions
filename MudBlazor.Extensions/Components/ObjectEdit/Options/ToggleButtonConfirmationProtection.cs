using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <summary>
/// Protection with a toggle button that requires confirmation before changing the value.
/// </summary>
public class ToggleButtonConfirmationProtection : IConfirmationProtection
{
    /// <summary>
    /// The button instance.
    /// </summary>
    protected MudToggleIconButton Button { get; set; }

    /// <summary>
    /// Constructor for a boolean input protection instance.
    /// </summary>
    /// <param name="tooltip"></param>
    /// <param name="configure"></param>
    public ToggleButtonConfirmationProtection(string tooltip = null, Action<MudToggleIconButton>? configure = null)
    {
        AdditionalRenderData = RenderData.For<MudToggleIconButton>(cb =>
        {
            cb.Style = MudExStyleBuilder.Default
                .WithSize(20)
                .WithMarginTop(20)
                .WithMarginLeft(10)
                .ToString();
            cb.Icon = Icons.Material.Filled.Lock;
            cb.ToggledIcon = Icons.Material.Filled.LockOpen;
            cb.Size = Size.Small;
            cb.Color = Color.Default;
            cb.ToggledColor = Color.Primary;
            cb.ToggledChanged = EventCallback.Factory.Create<bool>(new object(), b => ConfirmationCallback?.Invoke(b));
            configure?.Invoke(cb);
        });

        AdditionalRenderData.OnRendered<MudToggleIconButton>(cb =>
        {
            if(!string.IsNullOrWhiteSpace(tooltip))
                cb.UserAttributes.Add("title", tooltip);
            Button = cb;
            OnButtonSet(cb);
        });

    }

    protected virtual void OnButtonSet(MudToggleIconButton cb)
    {}

    /// <inheritdoc />
    public IRenderData AdditionalRenderData { get; set; }

    /// <inheritdoc />
    public Action<bool>? ConfirmationCallback { get; set; }
}