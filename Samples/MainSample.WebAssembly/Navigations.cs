using MainSample.WebAssembly.Types;
using MudBlazor;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Api;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;

namespace MainSample.WebAssembly;

public static class Navigations
{
    public static HashSet<NavigationEntry> Default(NavigationManager navigation)
    {
        var navigationEntries = new HashSet<NavigationEntry>
        {
            new("Home", Icons.Material.Outlined.Home, "/"),
            new("Readme", Icons.Material.Outlined.ReadMore, "/readme"),
            new("API", Icons.Material.Outlined.Api, "/api"),
            new("-")
        };

        var host = navigation.ToAbsoluteUri(navigation.Uri).Host.ToLower();
        if (MudExResource.IsDebug && host is "localhost" or "127.0.0.1" )
        {
            navigationEntries.Add(new NavigationEntry("TEST", Icons.Material.Outlined.BugReport, "/test"));
        }

        navigationEntries.UnionWith(DemoAttribute.AllEntries());

        navigationEntries.Add(new NavigationEntry("Utilities")
        {
            Children = GetUtils()
        });

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

    private static HashSet<NavigationEntry> GetUtils()
    {
        return ComponentTypes.DocumentedUtils()
            .Select(d => new NavigationEntry(ApiMemberInfo.GetGenericFriendlyTypeName(d.Type) ?? d.Type.Name, "",
                d.Documentation != null
                 ? $"/d/{Path.GetFileNameWithoutExtension(d.Documentation.MarkdownFile)}/{d.Type.Name}"
                 : $"/a/{d.Type.Name}"
                ))            
            .ToHashSet();
    }

    internal static HashSet<NavigationEntry> ReflectMudExComponents()
    {
        return ComponentTypes.AllMudExComponents().Select(type => new NavigationEntry($"{ApiMemberInfo.GetGenericFriendlyTypeName(type) ?? type.Name}", "", $"/c/{type.Name}") {Type = type}).ToHashSet();
    }

    internal static HashSet<NavigationEntry> ReflectMudBlazorComponents()
    {
        return ComponentTypes.AllMudBlazorComponents().Select(type => new NavigationEntry($"{ApiMemberInfo.GetGenericFriendlyTypeName(type) ?? type.Name}", "", $"/c/{type.Name}") { Type = type }).ToHashSet();
    }

}