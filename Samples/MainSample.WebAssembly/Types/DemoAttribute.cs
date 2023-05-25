using System.Reflection;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Extensions.Helper;

namespace MainSample.WebAssembly.Types;

public class DemoNewAttribute : DemoAttribute
{
    private string? _icon;

    public override Color IconColor
    {
        get => Color.Warning;
        set { }
    }

    public override string? Icon
    {
        get
        {
            var newIcon = Icons.Material.Filled.Star;
            var result = string.IsNullOrEmpty(_icon) ? newIcon : MudExSvg.CombineIconsHorizontal(_icon, newIcon);
            return result;
        }
        set => _icon = value;
    }
}


[AttributeUsage(AttributeTargets.Class)]
public class DemoAttribute : Attribute
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