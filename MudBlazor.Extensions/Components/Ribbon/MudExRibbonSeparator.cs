using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A vertical rule between ribbon items, a divider in menus.
/// </summary>
public class MudExRibbonSeparator : MudExRibbonItemBase
{
    /// <inheritdoc />
    public override bool IsCommand => false;

    /// <inheritdoc />
    public override RenderFragment Render(MudExRibbonItemLayout layout) => builder =>
    {
        builder.OpenComponent<MudDivider>(0);
        builder.AddAttribute(1, nameof(MudDivider.Class), "mud-ex-ribbon-separator");
        builder.AddAttribute(2, nameof(MudDivider.Vertical), layout != MudExRibbonItemLayout.MenuEntry);
        builder.AddAttribute(3, nameof(MudDivider.FlexItem), layout != MudExRibbonItemLayout.MenuEntry);
        builder.CloseComponent();
    };
}
