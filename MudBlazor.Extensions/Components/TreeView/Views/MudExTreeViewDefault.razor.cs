using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Css;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

public partial class MudExTreeViewDefault<T> 
    where T : IHierarchical<T>
{
    /// <inheritdoc />
    protected override string ItemStyleStr(TreeViewItemContext<T> context)
    {
        return MudExStyleBuilder.FromStyle(base.ItemStyleStr(context))
            .WithDisplay(Display.Flex)
            .WithAlignItems(Core.Css.AlignItems.Center)
            .WithFlexFlow(FlexFlow.RowReverse, ReverseExpandButton)
            .WithJustifyContent(JustifyContent.SpaceBetween, ReverseExpandButton)
            .Style;
    }

}

