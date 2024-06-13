using MudBlazor;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Types;

namespace MainSample.WebAssembly.Types;

public class NavigationEntry : Hierarchical<NavigationEntry>
{
    private Type _type;

    public NavigationEntry(string text = "", string icon = "", string href = "", string target = "")
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