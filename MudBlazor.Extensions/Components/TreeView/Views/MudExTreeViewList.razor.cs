using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

public partial class MudExTreeViewList<T> 
    where T : IHierarchical<T>
{
    [Parameter] public bool AnimateNavigation { get; set; } = true;
    [Parameter] public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(300);
    [Parameter] public string RootName { get; set; } = "Root";
    [Parameter] public string BackLinkLabel { get; set; } = "Back to {0}";

    private AnimationDirection? _animationDirection;

    protected override void NodeClick(T node)
    {
        if (_animationDirection == null)
        {
            _animationDirection =
                node == null || SelectedNode?.Parent?.Equals(node) == true ||
                SelectedNode?.Parent?.Parent?.Equals(node) == true
                    ? AnimationDirection.In
                    : AnimationDirection.Out;
            if (!node.HasChildren())
                _animationDirection = null;
        }

        base.NodeClick(node);
    }

    private string ListBoxStyleStr()
    {
        if (_animationDirection == null)
        {
            return string.Empty;
        }
        Task.Delay(AnimationDuration).ContinueWith(_ =>
        {
            _animationDirection = null;
            InvokeAsync(StateHasChanged);
        });
        return MudExStyleBuilder.Default.WithAnimation(
            AnimationType.Slide,
            AnimationDuration,
            _animationDirection,
            AnimationTimingFunction.EaseInOut,
            DialogPosition.CenterRight, when: _animationDirection != null && AnimateNavigation).Style;
    }
}

