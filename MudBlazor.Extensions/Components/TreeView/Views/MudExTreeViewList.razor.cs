using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

public partial class MudExTreeViewList<T> 
    where T : IHierarchical<T>
{
    [Parameter] public string BackLinkLabel { get; set; } = "Back to {0}";

    private AnimationDirection? _animationDirection;

    protected override void NodeClick(T node)
    {
        _animationDirection = node == null || selectedNode?.Parent?.Equals(node) == true || selectedNode?.Parent?.Parent?.Equals(node) == true ? AnimationDirection.In : AnimationDirection.Out;
        if (!node.HasChildren())
            _animationDirection = null;
        base.NodeClick(node);
    }

    private string ListBoxStyleStr()
    {
        if (_animationDirection == null)
        {
            return string.Empty;
        }
        var duration = TimeSpan.FromMilliseconds(300);
        Task.Delay(duration).ContinueWith(_ =>
        {
            _animationDirection = null;
            InvokeAsync(StateHasChanged);
        });
        return MudExStyleBuilder.Default.WithAnimation(
            AnimationType.Slide,
            duration,
            _animationDirection,
            AnimationTimingFunction.EaseInOut,
            DialogPosition.CenterRight, when: _animationDirection != null).Style;
    }
}

