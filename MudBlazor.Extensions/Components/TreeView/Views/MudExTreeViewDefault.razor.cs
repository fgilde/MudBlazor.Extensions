using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Css;
using MudBlazor.Extensions.Core.Enums;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

public partial class MudExTreeViewDefault<T> 
    where T : IHierarchical<T>
{
    private TreeViewExpandBehaviour _expandBehaviour;

    /// <summary>
    /// This behaviour controls how the tree view will be expanded.
    /// </summary>
    [Parameter]
    public TreeViewExpandBehaviour ExpandBehaviour
    {
        get =>  _expandBehaviour;
        set
        {
            if (value != _expandBehaviour)
            {
                _expandBehaviour = value;
                SetAllExpanded(ExpandBehaviour != TreeViewExpandBehaviour.SingleExpand);
            }
        }
    }


    /// <inheritdoc />
    public override bool IsExpanded(T node) => ExpandBehaviour == TreeViewExpandBehaviour.None || base.IsExpanded(node);

    /// <inheritdoc />
    public override void SetExpanded(T context, bool expanded)
    {
        if (ExpandBehaviour != TreeViewExpandBehaviour.None)
        {
            if (ExpandBehaviour == TreeViewExpandBehaviour.SingleExpand && expanded)
            {
                SetAllExpanded(false, n => n.Parent == null && context.Parent == null || n.Parent?.Equals(context.Parent) == true);
            }

            base.SetExpanded(context, expanded);
        }
    }

    private string ContentFlexStyleStr()
    {
        return MudExStyleBuilder.Default
            .WithWidth(100, CssUnit.Percentage)
            .WithDisplay(Display.Flex)
            .WithAlignItems(Core.Css.AlignItems.Center)
            .WithFlexFlow(FlexFlow.RowReverse, ExpandButtonDirection == LeftOrRight.Right)
            .WithJustifyContent(JustifyContent.SpaceBetween, ExpandButtonDirection == LeftOrRight.Right)
            .Style;
    }


}

