using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core.Enums;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

public partial class MudExTreeViewList<T> 
    where T : IHierarchical<T>
{
    /// <summary>
    /// The Animation to use when navigating through the tree. Default is Slide.
    /// Set this to AnimationType.Default to disable animations.
    /// </summary>
    [Parameter] public AnimationType AnimationIn { get; set; } = AnimationType.Slide;

    /// <summary>
    /// The Animation to use when navigating back through the tree. Default is Slide.
    /// </summary>
    [Parameter] public AnimationType AnimationOut { get; set; } = AnimationType.Slide;

    /// <summary>
    /// The position of the animation.
    /// </summary>
    [Parameter] public DialogPosition AnimationInPosition { get; set; } = DialogPosition.CenterRight;

    /// <summary>
    /// The position of the out animation.
    /// </summary>
    [Parameter] public DialogPosition AnimationOutPosition { get; set; } = DialogPosition.CenterRight;

    /// <summary>
    /// The direction of the animation when navigating deeper in the tree. Default is In.
    /// </summary>
    [Parameter] public AnimationDirection AnimationInDirection { get; set; } = AnimationDirection.In;

    /// <summary>
    /// The direction of the animation when navigating back in the tree. Default is Out.
    /// </summary>
    [Parameter] public AnimationDirection AnimationOutDirection { get; set; } = AnimationDirection.Out;

    /// <summary>
    /// Duration of the in animation.
    /// </summary>
    [Parameter] public TimeSpan AnimationInDuration { get; set; } = TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Duration of the out animation.
    /// </summary>
    [Parameter] public TimeSpan AnimationOutDuration { get; set; } = TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Name of the root node.
    /// </summary>
    [Parameter] public string RootName { get; set; } = "Root";

    /// <summary>
    /// Label for the back link.
    /// </summary>
    [Parameter] public string BackLinkLabel { get; set; } = "Back to {0}";

    /// <summary>
    /// Set false to hide the back link.
    /// </summary>
    [Parameter] public bool RenderBackLink { get; set; } = true;

    /// <summary>
    /// Set false to hide the home link.
    /// </summary>
    [Parameter] public bool RenderHomeLink { get; set; } = true;

    /// <summary>
    /// Css class for the toolbar where back and home link are located in
    /// </summary>
    [Parameter] public string NavigationLinkBarClass { get; set; }

    /// <summary>
    /// Style for the toolbar where back and home link are located in
    /// </summary>
    [Parameter] public string NavigationLinkBarStyle { get; set; }

    private bool? _animationIn;

    /// <summary>
    /// Context and label of back node
    /// </summary>
    /// <returns></returns>
    protected (string Label, TreeViewItemContext<T> Context) GetBackNodeTarget()
    {
        var ctx = CreateContext(LastSelectedNode.Children?.Any() == true ? LastSelectedNode.Parent : LastSelectedNode.Parent.Parent, "");
        var label = TryLocalize(BackLinkLabel, ctx != null && ctx.Value != null ? TryLocalize(TextFunc(ctx.Value)) : TryLocalize(RootName));
        return (label, ctx);
    }
    
    /// <summary>
    /// Returns the root node and its children if the current node is null.
    /// Otherwise, returns the children of the current node.
    /// </summary>
    protected (bool IsRoot, IReadOnlyCollection<T> Nodes) LevelNodes()
    {
        var root = false;
        IReadOnlyCollection<T> nodes = (LastSelectedNode != null ? (LastSelectedNode.Children?.Any() == true ? LastSelectedNode.Children : LastSelectedNode?.Parent?.Children) : null);
        if (nodes == null)
        {
            nodes = FilterManager.FilteredItems();
            root = true;
        }
        return (root, nodes);
    }

    /// <inheritdoc />
    public override void NodeClick(T node)
    {
        if (_animationIn == null)
        {
            _animationIn =
                node == null || LastSelectedNode?.Parent?.Equals(node) == true ||
                LastSelectedNode?.Parent?.Parent?.Equals(node) == true;
            if (!node.HasChildren())
                _animationIn = null;
        }

        base.NodeClick(node);
    }

    private string ListItemClassStr()
    {
        return MudExCssBuilder.Default.
            AddClass("mud-ex-simple-flex")
            .AddClass("mud-ex-flex-reverse-end", ExpandButtonDirection == LeftOrRight.Left)
            .ToString();
    }

    /// <summary>
    /// Creates the style with animation styles included.
    /// </summary>
    protected virtual string AnimationStyleStr()
    {
        if (_animationIn == null)
            return StyleStr(b => b.WithMarginTop(10));

        return _animationIn.Value
            ? AnimationStyleStr(AnimationIn, AnimationInDirection, AnimationInPosition, AnimationInDuration)
            : AnimationStyleStr(AnimationOut, AnimationOutDirection, AnimationOutPosition, AnimationOutDuration);
    }

    private string AnimationStyleStr(AnimationType animation, AnimationDirection direction, DialogPosition position, TimeSpan duration)
    {
        Task.Delay(duration).ContinueWith(_ =>
        {
            _animationIn = null;
            InvokeAsync(StateHasChanged);
        });
        return MudExStyleBuilder.FromStyle(StyleStr(b => b.WithMarginTop(10)))
            .WithAnimation(animation, duration, direction, AnimationTimingFunction.EaseOut, position, when: _animationIn != null && animation != AnimationType.Default)
            .Style;
    }
}

