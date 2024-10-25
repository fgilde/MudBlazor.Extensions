using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

public partial class MudExTreeViewCardGrid<T>
    where T : IHierarchical<T>
{

    /// <summary>
    /// Gets or Sets MudExColor HoverColor Property.
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public MudExColor HoverColor { get; set; } = Color.Primary;

    [Parameter]
    [SafeCategory("Behavior")]
    public MudExCardHoverMode? HoverMode { get; set; }

    /// <summary>
    /// Gets or Sets Justify Property.
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public Justify Justify { get; set; } = Justify.Center;

    /// <summary>
    /// Gets or Sets Spacing Property.
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public int Spacing { get; set; } = 15;

    /// <summary>
    /// Gets or Sets Light Bulb Size Property.
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public int LightBulbSize { get; set; } = 30;

    /// <summary>
    /// Gets or Sets Light Bulb Size Unit Property.
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public CssUnit LightBulbSizeUnit { get; set; } = CssUnit.Percentage;

    /// <summary>
    /// Max width of the item
    /// </summary>
    [Parameter] public MudExSize<double> MaxItemWidth { get; set; } = 450;

    /// <inheritdoc />
    protected override string ItemStyleStr(TreeViewItemContext<T> context, string mergeWith = "")
    {
        return MudExStyleBuilder.FromStyle(base.ItemStyleStr(context, mergeWith))
            .WithMaxWidth(MaxItemWidth, !MaxItemWidth.IsZero())
            .Style;
    }

    protected override void OnInitialized()
    {
        if (!IsOverwritten(new[] { nameof(AnimationIn), nameof(AnimationOut), nameof(AnimationInDirection), nameof(AnimationOutDirection), nameof(AnimationInPosition), nameof(AnimationOutPosition) }))
        {
            AnimationIn = AnimationType.Back;
            AnimationOut = AnimationType.Back;

            AnimationInDirection = AnimationDirection.In;
            AnimationOutDirection = AnimationDirection.In;

            AnimationInPosition = DialogPosition.CenterRight;
            AnimationOutPosition = DialogPosition.CenterLeft;
        }


        base.OnInitialized();
    }
}

