using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace MainSample.WebAssembly.Types;

[AttributeUsage(AttributeTargets.Class)]
internal class DemoAttribute : Attribute
{
    public Type PageType { get; private set; }

    public DemoAttribute(Type pageType)
    {
        UpdateType(pageType);
    }

    public DemoAttribute()
    { }

    private void UpdateType(Type pageType)
    {
        PageType = pageType;
        Name ??= PageType.Name.Replace("Page_", "");
        Url ??= PageType.GetCustomAttribute<RouteAttribute>()?.Template;
    }

    public NavigationEntry ToNavigationEntry(Type type)
    {
        UpdateType(type);
        return new(Name, Icon, Url, Url?.ToLower()?.StartsWith("http") == true ? "_blank" : "") { Demo = this };
    }

    public NavigationEntry ToNavigationEntry() => ToNavigationEntry(PageType);


    public Type? ForComponentType { get; set; }
    public Type[]? ForComponentTypes { get; set; }
    public string? Name { get; set; }
    public string? Group { get; set; }
    public string? Icon { get; set; }
    public int Order { get; set; } = 999;
    public string? Url { get; set; }

    public IEnumerable<Type> RelatedComponents => (ForComponentTypes ?? Array.Empty<Type>()).Union(ForComponentType != null ? new[] { ForComponentType } : Array.Empty<Type>());


    public static HashSet<NavigationEntry> AllEntries(bool flat = false, string ungrouppedName = "Other Components")
    {
        var attrType = typeof(DemoAttribute);

        if (!flat)
        {
            var grouped = attrType.Assembly.GetTypes()
                .Select(t => new { Type = t, Attribute = t.GetCustomAttribute<DemoAttribute>() })
                .Where(r => r.Attribute != null)
                .GroupBy(arg => arg?.Attribute?.Group);

            var navigationEntries = new HashSet<NavigationEntry>();
            foreach (var g in grouped.OrderBy(g => g.Key == null ? 1 : 0).ThenBy(g => g.Key))
            {
                var groupNavigationEntry = new NavigationEntry
                {
                    Text = g.Key ?? ungrouppedName,
                    Children = new HashSet<NavigationEntry>(),
                    // IsExpanded = g.Key == null
                };
                foreach (var r in g.OrderBy(r => r.Attribute.Order))
                {
                    var navigationEntry = r.Attribute.ToNavigationEntry(r.Type);
                    groupNavigationEntry.Children.Add(navigationEntry);
                }

                navigationEntries.Add(groupNavigationEntry);
            }

            return navigationEntries;
        }

        return attrType.Assembly.GetTypes()
            .Select(t => new { Type = t, Attribute = t.GetCustomAttribute<DemoAttribute>() })
            .Where(r => r.Attribute != null)
            .OrderBy(r => r.Attribute.Order)
            .Select(r => r.Attribute.ToNavigationEntry(r.Type))
            .ToHashSet();
    }
}