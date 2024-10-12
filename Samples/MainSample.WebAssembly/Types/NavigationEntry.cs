using MudBlazor;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Types;

namespace MainSample.WebAssembly.Types;

public class NavigationEntry : Hierarchical<NavigationEntry>
{
    protected bool Equals(NavigationEntry other)
    {
        return Text == other.Text && Href == other.Href;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((NavigationEntry)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Text, Href);
    }

    private Type _type;

    public NavigationEntry()
    {}

    public NavigationEntry(string text = "", string icon = "", string href = "", string target = "") : this()
    {
        Icon = icon;
        Text = text;
        Href = href;
        Target = target;
    }

    public Type Type
    {
        get => _type;
        set
        {
            _type = value;
            _=LoadDoc();
        }
    }

    private async Task LoadDoc()
    {
        Documentation ??= await MudExResource.GetSummaryDocumentationAsync(Type);
    }

    public string? Documentation { get; set; }
    public string Text { get; set; }
    public string Icon { get; set; }
    public string Href { get; set; }
    public string Target { get; set; }
    public bool? Bold { get; set; }
    internal DemoAttribute? Demo { get; set; }
    public Color GetIconColor() => Demo?.IconColor ?? Color.Default;
    public override string ToString()
    {
        return Text;
    }
}