using System.Reflection;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Nextended.Core.Extensions;

namespace MainSample.WebAssembly.Types;

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

        var allTypeAttributes =
            attrType.Assembly.GetTypes()
                .SelectMany(t =>
                    t.GetCustomAttributes<DemoAttribute>(inherit: false)
                        .Select(a => new { Type = t, Attribute = a }));

        if (!flat)
        {
            var navigationEntries = new List<NavigationEntry>();
            var groupCache = new Dictionary<string, NavigationEntry>();

            foreach (var item in allTypeAttributes
                         .OrderBy(r => r.Attribute.Order)
                         .ThenBy(r => r.Attribute.Name)) // oder Text/Url, je nach Geschmack
            {
                var groupPath = string.IsNullOrWhiteSpace(item.Attribute.Group)
                    ? ungrouppedName
                    : item.Attribute.Group;

                var parentEntry = GetOrCreateGroupHierarchy(groupPath, groupCache, navigationEntries);
                var navigationEntry = item.Attribute.ToNavigationEntry(item.Type);
                navigationEntry.Parent = parentEntry;
                parentEntry.Children.Add(navigationEntry);
            }

            SortEntriesRecursively(navigationEntries);
            return navigationEntries.ToHashSet();
        }

        // flat
        return allTypeAttributes
            .OrderBy(r => r.Attribute.Order)
            .ThenBy(r => r.Attribute.Name)
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
        List<NavigationEntry> rootEntries)
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
                    Children = new HashSet<NavigationEntry>()
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

    private static void SortEntriesRecursively(IList<NavigationEntry> entries)
    {
        // Erst sortieren
        var ordered = entries
            .OrderBy(e => e.Demo?.Order ?? 999)
            .ThenBy(e => e.Text)
            .ToList();

        entries.Clear();
        foreach (var e in ordered)
        {
            entries.Add(e);
            if (e.Children is IList<NavigationEntry> childList && childList.Count > 0)
            {
                SortEntriesRecursively(childList);
            }
        }
    }



}