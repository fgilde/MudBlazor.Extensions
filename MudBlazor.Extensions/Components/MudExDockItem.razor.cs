using BlazorJS.Attributes;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System.ComponentModel;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;


public enum DockRendererMode
{
    [Description("always")]
    Always,
    [Description("onlyWhenVisible")]
    OnlyWhenVisible
}

public partial class MudExDockItem
{
    [CascadingParameter] public MudExDockItem ParentItem { get; set; }
    [CascadingParameter] public MudExDockLayout Layout { get; set; }

    [ForJs, Parameter] public string Id { get; set; }

    [ForJs, Parameter] public string Title { get; set; } = "Panel";
    [ForJs, Parameter] public DockDirection Direction { get; set; } = DockDirection.Right;
    [ForJs, Parameter] public bool HideHeader { get; set; }
    [ForJs, Parameter] public bool Float { get; set; }
    [ForJs, Parameter] public bool Locked { get; set; }
    [ForJs, Parameter] public bool CanClose { get; set; } = true;

    [ForJs, Parameter] public bool Inactive { get; set; }
    [ForJs, Parameter] public bool IsGroupActive { get; set; }
    [ForJs, Parameter] public bool IsActive { get; set; }
    [ForJs, Parameter] public bool IsFocused { get; set; }

    [ForJs, Parameter] public int? MinimumWidth { get; set; }
    [ForJs, Parameter] public int? MinimumHeight { get; set; }
    [ForJs, Parameter] public int? MaximumWidth { get; set; }
    [ForJs, Parameter] public int? MaximumHeight { get; set; }
    [ForJs, Parameter] public int? InitialWidth { get; set; }
    [ForJs, Parameter] public int? InitialHeight { get; set; }

    [ForJs, Parameter] public DockRendererMode Renderer { get; set; } = DockRendererMode.OnlyWhenVisible;

    [Parameter] public RenderFragment ChildContent { get; set; }

    internal List<MudExDockItem> Children { get; } = new();

    private string Options => JsonConvert.SerializeObject(this.AsJsObject(new
    {
        className = ClassStr
    }));

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (ParentItem is not null)
            ParentItem.Children.Add(this);
        else
            Layout?.RegisterRoot(this);

        if (string.IsNullOrWhiteSpace(Id) || string.Equals(Id, "auto", StringComparison.OrdinalIgnoreCase))
            Id = BuildPathId();
    }



    private string BuildPathId()
    {
        var segments = new List<int>();
        var node = this;

        while (node is not null)
        {
            if (node.ParentItem is not null)
            {
                var idx = node.ParentItem.Children.IndexOf(node);
                segments.Add(idx);
                node = node.ParentItem;
            }
            else
            {
                var idx = Layout?.RootItems.IndexOf(node) ?? 0;
                segments.Add(idx);
                break;
            }
        }

        segments.Reverse();
        return $"{Layout?.Id}-{string.Join(".", segments)}";
    }

    private string ClassStr => MudExCssBuilder.From("dv-node").AddClass(Class).AddClass("dv-hide-close", !CanClose).ToString();

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        if (ParentItem is not null)
            ParentItem.Children.Remove(this);
        else
            Layout?.UnregisterRoot(this);
        return base.DisposeAsync();
    }
}
