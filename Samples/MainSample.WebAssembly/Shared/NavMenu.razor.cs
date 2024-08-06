using MainSample.WebAssembly.Types;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core.Enums;
using Nextended.Core.Extensions;
using Nextended.Core.Types;

namespace MainSample.WebAssembly.Shared;

public partial class NavMenu
{

    private NavigationEntry _selectedNavEntry;
    private TreeViewExpandBehaviour _expandBehaviour;
    private TreeViewMode _viewMode = TreeViewMode.Default;

    public NavigationEntry SelectedNavEntry
    {
        get => _selectedNavEntry;
        set
        {
            if (_selectedNavEntry != value)
            {
                _selectedNavEntry = value;
                if (HasAction(value))
                {
                    try
                    {
                        NavigationManager.NavigateTo(value.Href);
                    }
                    catch
                    {}
                }
            }
        }
    }

    [Parameter] public TreeViewMode ViewMode
    {
        get => _viewMode;
        set
        {
            if (_viewMode == value)
                return;
            _viewMode = value;
            InvokeAsync(StateHasChanged);
        }
    }


    [Parameter] public HashSet<NavigationEntry>? Entries { get; set; } = null;

    [Parameter]
    public TreeViewExpandBehaviour ExpandBehaviour
    {
        get => _expandBehaviour;
        set
        {
            if (value != _expandBehaviour)
            {
                _expandBehaviour = value;
                InvokeAsync(StateHasChanged);
            }
        }
    }


    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            NavigationManager.LocationChanged += (s, e) => ExpandToCurrentUrl();
            //SetAllExpanded(ExpandMode != ExpandMode.SingleExpand);
        }

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
        var current = SelectedNavEntry;
        var url = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        SelectedNavEntry = FindEntriesForUrl(url)?.FirstOrDefault();
        if(SelectedNavEntry != null && SelectedNavEntry != current)
            InvokeAsync(StateHasChanged);
    }

    public IEnumerable<NavigationEntry> FindEntriesForUrl(string url = null)
    {
        url = (url ?? NavigationManager.ToBaseRelativePath(NavigationManager.Uri)).EnsureStartsWith("/").ToLower();
        return Entries.Find(e => e.Href.EnsureStartsWith("/").ToLower() == url);
    }



    private bool HasAction(NavigationEntry entry)
    {
        return !string.IsNullOrWhiteSpace(entry?.Href);
    }


}
