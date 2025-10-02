using System.Reflection;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Extensions.Helper;

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


    public static HashSet<NavigationEntry> AllEntries(bool flat = false, string ungrouppedName = AppConstants.UngroupedNavCategory)
    {
        var attrType = typeof(DemoAttribute);

        // Alle Typen + alle Vorkommen von DemoAttribute flach auflösen
        var allTypeAttributes =
            attrType.Assembly.GetTypes()
                .SelectMany(t =>
                    t.GetCustomAttributes<DemoAttribute>(inherit: false)
                     .Select(a => new { Type = t, Attribute = a }));

        if (!flat)
        {
            // Leere/whitespace Gruppen wie "ungrouped" behandeln
            var grouped = allTypeAttributes
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Attribute.Group) ? null : x.Attribute.Group);

            var navigationEntries = new HashSet<NavigationEntry>();

            foreach (var g in grouped
                         .OrderBy(g => g.Key == null ? 1 : 0)
                         .ThenBy(g => g.Key))
            {
                var groupName = g.Key ?? ungrouppedName;

                var groupNavigationEntry = new NavigationEntry
                {
                    Text = groupName,
                    Href = $"/demos/{groupName}",
                    Children = new HashSet<NavigationEntry>(),
                    // IsExpanded = g.Key == null
                };

                foreach (var r in g
                             .OrderBy(r => r.Attribute.Order)
                             .ThenBy(r => r.Attribute?.ToNavigationEntry(r.Type)?.Text)) // optional: stabilere Sortierung
                {
                    var navigationEntry = r.Attribute.ToNavigationEntry(r.Type);
                    navigationEntry.Parent = groupNavigationEntry;
                    groupNavigationEntry.Children.Add(navigationEntry);
                }

                navigationEntries.Add(groupNavigationEntry);
            }

            return navigationEntries;
        }

        // Flache Liste aller Attribute über alle Typen
        return allTypeAttributes
            .OrderBy(r => r.Attribute.Order)
            .ThenBy(r => r.Attribute?.ToNavigationEntry(r.Type)?.Text) // optional
            .Select(r => r.Attribute.ToNavigationEntry(r.Type))
            .ToHashSet();
    }

}