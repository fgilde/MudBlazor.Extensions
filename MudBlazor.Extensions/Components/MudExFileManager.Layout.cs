using System.Text.Json;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core.FileManager;

namespace MudBlazor.Extensions.Components;

// The dock arrangement, written as a dockview layout: the declarative panel order can only place a panel
// relative to the whole grid, never beside one specific panel. Sizes are ratios, not pixels.
public partial class MudExFileManager
{
    internal const string TreePanelId = "tree";
    internal const string FilesPanelId = "files";
    internal const string PreviewPanelId = "preview";
    internal const string MetaPanelId = "meta";

    private string _layoutJson;

    /// <summary>Url of the page a popped out panel is hosted in.</summary>
    [Parameter, SafeCategory("Behavior")]
    public string PopoutUrl { get; set; } = "_content/MudBlazor.Extensions/popout.html";

    /// <summary>Id of the dock layout, so a stored arrangement can be matched back to it.</summary>
    private string DockId => $"{ElementId}-dock";

    /// <summary>Tree left, files in the middle, preview and details stacked right.</summary>
    private string BuildDefaultLayoutJson()
    {
        object Leaf(string groupId, int size, params string[] views) => new
        {
            type = "leaf",
            size,
            data = new { id = groupId, views, activeView = views[0] }
        };

        object Branch(int size, params object[] children) => new { type = "branch", size, data = children };

        object Panel(string id, string title) => new { id, title, renderer = "always" };

        var panels = new Dictionary<string, object>();
        var root = new List<object>();
        var groupId = 1;
        var activeGroup = (string)null;

        if (HasPanel(MudExFileManagerPanels.Tree))
        {
            var id = (groupId++).ToString();
            root.Add(Leaf(id, TreePanelWidth, TreePanelId));
            panels[TreePanelId] = Panel(TreePanelId, TryLocalize("Structure"));
            activeGroup ??= id;
        }

        if (HasPanel(MudExFileManagerPanels.Files))
        {
            var id = (groupId++).ToString();
            root.Add(Leaf(id, TreePanelWidth + SidePanelWidth + 240, FilesPanelId));
            panels[FilesPanelId] = Panel(FilesPanelId, TryLocalize("Files"));
            activeGroup = id;
        }

        // One group, so preview and details are tabs rather than two columns eating the width.
        var sideViews = new List<string>();
        if (HasPanel(MudExFileManagerPanels.Preview))
        {
            sideViews.Add(PreviewPanelId);
            panels[PreviewPanelId] = Panel(PreviewPanelId, TryLocalize("Preview"));
        }
        if (HasPanel(MudExFileManagerPanels.Meta))
        {
            sideViews.Add(MetaPanelId);
            panels[MetaPanelId] = Panel(MetaPanelId, TryLocalize("Details"));
        }
        if (sideViews.Count > 0)
        {
            var id = (groupId++).ToString();
            root.Add(Leaf(id, SidePanelWidth, sideViews.ToArray()));
            activeGroup ??= id;
        }

        if (panels.Count == 0)
            return null;

        return JsonSerializer.Serialize(new
        {
            grid = new
            {
                width = 1400,
                height = 800,
                orientation = "HORIZONTAL",
                root = Branch(800, root.ToArray())
            },
            panels,
            activeGroup
        });
    }
}
