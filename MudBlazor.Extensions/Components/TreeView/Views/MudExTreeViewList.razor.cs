using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

public partial class MudExTreeViewList<T> 
    where T : IHierarchical<T>
{
    /// <summary>
    /// Set to true to animate navigation.
    /// </summary>
    [Parameter] public bool AnimateNavigation { get; set; } = true;

    /// <summary>
    /// Duration of the animation.
    /// </summary>
    [Parameter] public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Name of the root node.
    /// </summary>
    [Parameter] public string RootName { get; set; } = "Root";

    /// <summary>
    /// Label for the back link.
    /// </summary>
    [Parameter] public string BackLinkLabel { get; set; } = "Back to {0}";

    private AnimationDirection? _animationDirection;

    /// <inheritdoc />
    protected override void NodeClick(T node)
    {
        if (_animationDirection == null)
        {
            _animationDirection =
                node == null || LastSelectedNode?.Parent?.Equals(node) == true ||
                LastSelectedNode?.Parent?.Parent?.Equals(node) == true
                    ? AnimationDirection.In
                    : AnimationDirection.Out;
            if (!node.HasChildren())
                _animationDirection = null;
        }

        base.NodeClick(node);
    }

    private string ListItemClassStr()
    {
        return MudExCssBuilder.Default.
            AddClass("mud-ex-simple-flex")
            .AddClass("mud-ex-flex-reverse-end", ReverseExpandButton)
            .ToString();
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
        return MudExStyleBuilder.FromStyle(Style)
            .WithAnimation(AnimationType.Slide, AnimationDuration, _animationDirection, AnimationTimingFunction.EaseInOut, DialogPosition.CenterRight, when: _animationDirection != null && AnimateNavigation).Style;
    }
}

