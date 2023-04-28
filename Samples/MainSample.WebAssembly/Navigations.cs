using MainSample.WebAssembly.Types;
using MudBlazor;

namespace MainSample.WebAssembly;

public static class Navigations
{
    public static HashSet<NavigationEntry> Default()
    {
        var navigationEntries = new HashSet<NavigationEntry>
        {
            new("Home", Icons.Material.Outlined.Home, "/"),
            new("Readme", Icons.Material.Outlined.ReadMore, "/readme"),
        };

        navigationEntries.UnionWith(DemoAttribute.AllEntries());

        navigationEntries.Add(new NavigationEntry("Dynamic component tests")
        {
            Children = new()
            {
                new NavigationEntry("MudEx Components", null) {Children = ReflectMudExComponents()},
                new NavigationEntry("MudBlazor Components", null) {Children = ReflectMudBlazorComponents()},
            }
        });

        return navigationEntries;
    }

    internal static HashSet<NavigationEntry> ReflectMudExComponents()
    {
        return ComponentTypes.AllMudExComponents().Select(type => new NavigationEntry($"{type.Name}", "", $"/c/{type.Name}") {Type = type}).ToHashSet();
    }

    private static HashSet<NavigationEntry> ReflectMudBlazorComponents()
    {
        return ComponentTypes.AllMudBlazorComponents().Select(type => new NavigationEntry($"{type.Name}", "", $"/c/{type.Name}") { Type = type }).ToHashSet();
    }

}