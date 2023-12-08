using MainSample.WebAssembly.Types;
using Microsoft.AspNetCore.Components;
using Nextended.Core.Extensions;
using Nextended.Core.Types;

namespace MainSample.WebAssembly.Shared;

public partial class NavMenu
{
    private ExpandMode _expandMode;

    [Parameter] public bool ShowUserCard { get; set; } = true;

    [Parameter] public bool ShowApplicationLogo { get; set; } = false;

    [Parameter]
    public ExpandMode ExpandMode
    {
        get => _expandMode;
        set
        {
            if (value != _expandMode)
            {
                _expandMode = value;
                SetAllExpanded(ExpandMode != ExpandMode.SingleExpand);
            }
        }
    }

    [Parameter] public HashSet<NavigationEntry>? Entries { get; set; } = null;

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        //if (firstRender)
        //    SetAllExpanded(ExpandMode != ExpandMode.SingleExpand);
        return base.OnAfterRenderAsync(firstRender);
    }

    protected override Task OnParametersSetAsync()
    {
        Entries ??= Navigations.Default(NavigationManager);
        ExpandToCurrentUrl();
        return base.OnParametersSetAsync();
    }


    private void ExpandToCurrentUrl()
    {
        var url = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        if (ExpandMode != ExpandMode.None)
        {
            if (!string.IsNullOrWhiteSpace(url) && url != "/")
            {
                FindEntriesForUrl(url)
                    .SelectMany(e => e.Path)
                    .Apply(e => e.IsExpanded = true);
            }
        }
    }
    
    public IEnumerable<NavigationEntry> FindEntriesForUrl(string url = null)
    {
        url = (url ?? NavigationManager.ToBaseRelativePath(NavigationManager.Uri)).EnsureStartsWith("/").ToLower();
        return Entries.Find(e => e.Href.EnsureStartsWith("/").ToLower() == url);
    }

    private void OnExpandCollapseClick(NavigationEntry entry)
    {
        if (ExpandMode != ExpandMode.None)
        {
            var state = !entry.IsExpanded;
            if (ExpandMode == ExpandMode.SingleExpand)
                SetAllExpanded(false, e => e != entry && !e.ContainsChild(entry));
            entry.IsExpanded = state;
        }
    }

    private void SetAllExpanded(bool expand, Func<NavigationEntry, bool> predicate = null)
    {
        predicate ??= n => ExpandMode == ExpandMode.SingleExpand || n.Parent == null;
        Entries.Recursive(n => n.Children.EmptyIfNull()).Where(predicate).Apply(e => e.IsExpanded = expand);
    }


    private bool HasAction(NavigationEntry entry)
    {
        return !string.IsNullOrWhiteSpace(entry.Href);
    }

    private bool CanExpand(NavigationEntry context)
    {
        return context.HasChildren && ExpandMode != ExpandMode.None && (context.Parent == null || context.Parent.IsExpanded);
    }
}

public enum ExpandMode
{
    Default,
    SingleExpand,
    None
}