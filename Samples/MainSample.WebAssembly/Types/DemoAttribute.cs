using System.Reflection;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;

namespace MainSample.WebAssembly.Types;

/// <summary>
/// Navigation category constants for demo organization.
/// Use nested paths with '/' separator for multi-level navigation (e.g., "Components/Forms/Inputs").
/// </summary>
public static class DemoCategories
{
    public const string Components = "Components";
    public const string Dialogs = "Components/Dialogs";
    public const string Forms = "Components/Forms";
    public const string Selection = "Components/Selection";
    public const string FileHandling = "Components/File Handling";
    public const string Layout = "Components/Layout";
    public const string Data = "Components/Data Display";
    public const string Navigation = "Components/Navigation";
    public const string Utilities = "Utilities";
}

public class DemoNewAttribute : DemoAttribute
{
    private string? _icon;
    public override string GetIconStatusIcon() => Icons.Material.Filled.NewReleases;
    public override Color IconColor
    {
        get => Color.Warning;
        set { }
    }

    public override Severity Severity
    {
        get => Severity.Warning;
        set { }
    }

    //public override string? Icon
    //{
    //    get
    //    {
    //        var newIcon = Icons.Material.Filled.Star;
    //        var result = string.IsNullOrEmpty(_icon) ? newIcon : MudExSvg.CombineIconsHorizontal(_icon, newIcon);
    //        return result;
    //    }
    //    set => _icon = value;
    //}
}

public class DemoUpdatedAttribute : DemoAttribute
{
    private string? _icon;
    public override string GetIconStatusIcon() => Icons.Material.Filled.Label;

    public override Color IconColor
    {
        get => Color.Info;
        set { }
    }

    public override Severity Severity
    {
        get => Severity.Info;
        set { }
    }

    //public override string? Icon
    //{
    //    get
    //    {
    //        var newIcon = Icons.Material.Filled.Update;
    //        var result = string.IsNullOrEmpty(_icon) ? newIcon : MudExSvg.CombineIconsHorizontal(_icon, newIcon);
    //        return result;
    //    }
    //    set => _icon = value;
    //}
}


[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DemoAttribute : Attribute
{
    public Type PageType { get; private set; }

    public bool IsPlaygroundDemo { get; set; }
    
    public DemoAttribute(Type pageType)
    {
        UpdateType(pageType);
    }

    public DemoAttribute()
    { }

    public virtual string GetIconStatusIcon() => string.Empty;


    private void UpdateType(Type pageType)
    {
        PageType = pageType;
        Name ??= PageType.Name.Replace("Page_", "");
        Url ??= PageType.GetCustomAttribute<RouteAttribute>()?.Template;
    }

    public NavigationEntry ToNavigationEntry(Type type)
    {
        UpdateType(type);
        return new(Name, Icon, Url, Url?.ToLower()?.StartsWith("http") == true ? "_blank" : "")
        {
            Demo = this,
            //IconColor = IconColor,
            Documentation = Documentation
        };
    }

    public NavigationEntry ToNavigationEntry() => ToNavigationEntry(PageType);

    public string Documentation { get; set; }
    public Type? ForComponentType { get; set; }
    public Type[]? ForComponentTypes { get; set; }
    public string? Name { get; set; }
    public string? Group { get; set; }
    public virtual string? Icon { get; set; }
    public virtual Color IconColor { get; set; } = Color.Default;
    public virtual Severity Severity { get; set; } = Severity.Normal;
    public int Order { get; set; } = 999;
    public string? Url { get; set; }

    public IEnumerable<Type> RelatedComponents => (ForComponentTypes ?? Array.Empty<Type>()).Union(ForComponentType != null ? new[] { ForComponentType } : Array.Empty<Type>());

    public static NavigationEntry? FindPlaygroundEntryForType(Type? type)
    {
        if (type == null)
            return null;
        return AllEntries().Recursive(e => e?.Children ?? Enumerable.Empty<NavigationEntry>())
            .FirstOrDefault(entry => entry?.Demo?.IsPlaygroundDemo == true && entry?.Demo?.RelatedComponents?.Any(t => IsMatchType(type, t)) == true);
    }

    private static bool IsMatchType(Type type, Type other)
    {
        return other == type || (other.IsGenericType && type is { IsGenericType: true } && type.GetGenericTypeDefinition() == other.GetGenericTypeDefinition());
    }

    public static HashSet<NavigationEntry> AllEntries(bool flat = false, string ungrouppedName = AppConstants.UngroupedNavCategory)
    {
        var attrType = typeof(DemoAttribute);

        // Collect all types with DemoAttribute
        var allTypeAttributes =
            attrType.Assembly.GetTypes()
                .SelectMany(t =>
                    t.GetCustomAttributes<DemoAttribute>(inherit: false)
                     .Select(a => new { Type = t, Attribute = a }));

        if (!flat)
        {
            var navigationEntries = new HashSet<NavigationEntry>();
            var groupCache = new Dictionary<string, NavigationEntry>();

            foreach (var item in allTypeAttributes
                         .OrderBy(r => r.Attribute.Order)
                         .ThenBy(r => r.Attribute?.ToNavigationEntry(r.Type)?.Text))
            {
                var groupPath = string.IsNullOrWhiteSpace(item.Attribute.Group) 
                    ? ungrouppedName 
                    : item.Attribute.Group;
                
                var parentEntry = GetOrCreateGroupHierarchy(groupPath, groupCache, navigationEntries);
                var navigationEntry = item.Attribute.ToNavigationEntry(item.Type);
                navigationEntry.Parent = parentEntry;
                parentEntry.Children.Add(navigationEntry);
            }

            return navigationEntries;
        }

        // Flat list of all attributes across all types
        return allTypeAttributes
            .OrderBy(r => r.Attribute.Order)
            .ThenBy(r => r.Attribute?.ToNavigationEntry(r.Type)?.Text)
            .Select(r => r.Attribute.ToNavigationEntry(r.Type))
            .ToHashSet();
    }

    /// <summary>
    /// Creates or retrieves the navigation hierarchy for a group path.
    /// Supports multi-level paths separated by '/' (e.g., "Components/Forms/Inputs").
    /// </summary>
    private static NavigationEntry GetOrCreateGroupHierarchy(
        string groupPath, 
        Dictionary<string, NavigationEntry> groupCache, 
        HashSet<NavigationEntry> rootEntries)
    {
        if (groupCache.TryGetValue(groupPath, out var existing))
            return existing;

        var pathParts = groupPath.Split('/');
        NavigationEntry? parent = null;
        var currentPath = "";

        for (int i = 0; i < pathParts.Length; i++)
        {
            var part = pathParts[i].Trim();
            currentPath = i == 0 ? part : $"{currentPath}/{part}";

            if (!groupCache.TryGetValue(currentPath, out var entry))
            {
                entry = new NavigationEntry
                {
                    Text = part,
                    Href = $"/demos/{Uri.EscapeDataString(currentPath)}",
                    Children = new HashSet<NavigationEntry>(),
                    Icon = GetGroupIcon(currentPath)
                };
                groupCache[currentPath] = entry;

                if (parent == null)
                {
                    rootEntries.Add(entry);
                }
                else
                {
                    entry.Parent = parent;
                    parent.Children.Add(entry);
                }
            }
            parent = entry;
        }

        return parent!;
    }

    /// <summary>
    /// Returns an appropriate icon for known group categories.
    /// </summary>
    private static string GetGroupIcon(string groupPath)
    {
        return groupPath.ToLowerInvariant() switch
        {
            "components" => Icons.Material.Outlined.Widgets,
            "components/dialogs" => Icons.Material.Outlined.OpenInNew,
            "components/forms" => Icons.Material.Outlined.Edit,
            "components/selection" => Icons.Material.Outlined.CheckBox,
            "components/file handling" => Icons.Material.Outlined.FolderOpen,
            "components/layout" => Icons.Material.Outlined.Dashboard,
            "components/data display" => Icons.Material.Outlined.DataArray,
            "components/navigation" => Icons.Material.Outlined.Navigation,
            "utilities" => Icons.Material.Outlined.Build,
            _ => Icons.Material.Outlined.Folder
        };
    }

}