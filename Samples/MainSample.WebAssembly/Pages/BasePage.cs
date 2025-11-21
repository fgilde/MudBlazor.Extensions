using MainSample.WebAssembly.Shared;
using MainSample.WebAssembly.Types;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Nextended.Core.Extensions;
using Nextended.Core.Types;
using System.Linq;

namespace MainSample.WebAssembly.Pages;

public class BasePage : ComponentBase
{
    private NavigationEntry? _current;
    private static readonly HashSet<NavigationEntry> _demoEntries = DemoAttribute.AllEntries(true);
    private static readonly HashSet<NavigationEntry> _demoTree = DemoAttribute.AllEntries();

    internal readonly HashSet<NavigationEntry> Demos = _demoEntries;
    internal readonly HashSet<NavigationEntry> Tree = _demoTree;

    [Inject] protected IStringLocalizer<BasePage> L { get; set; }
    [Inject] protected NavigationManager Navigation { get; set; }

    internal NavigationEntry? FindCurrentDemoEntry()
    {
        if(_current != null)
            return _current;
        var uri = Navigation.Uri;
        return _current = Tree.Recursive(d => d?.Children ?? []).FirstOrDefault(d => uri.EndsWith(d.Href, StringComparison.OrdinalIgnoreCase));
    }

    internal NavigationEntry? FindNextDemo() => FindAdjacentDemo(1);
    internal NavigationEntry? FindPreviousDemo() => FindAdjacentDemo(-1);

    private NavigationEntry? FindAdjacentDemo(int offset)
    {
        var current = FindCurrentDemoEntry();
        if (current is null)
            return null;

        var children = current.Parent?.Children;
        if (children is null)
            return null;

        var navigationEntries = children.ToArray();
        if (navigationEntries.Length == 0)
            return null;

        var index = Array.IndexOf(navigationEntries, current);
        if (index < 0)
            return null;

        var newIndex = index + offset;

        if (newIndex < 0 || newIndex >= navigationEntries.Length)
            return null;

        return navigationEntries[newIndex];
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (MainLayout.Instance != null)
        {
            //MainLayout.Instance.ThemeChanged += (_, _) => StateHasChanged();
            MainLayout.Instance.LanguageChanged += (_, _) => InvokeAsync(StateHasChanged);
        }
    }



}