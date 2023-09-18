using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;


class ColorItem
{
    public ColorItem(MudExColor color, string name, string group)
    {
        Color = color;
        Name = name;
        Group = group;
    }
    public MudExColor Color { get; set; }
    public string Name { get; set; }
    public string Group { get; set; }
    public override string ToString()
    {
        return Name;
    }
}

public enum ColorPreviewMode
{
    None,
    Text,
    Icon,
    Both
}

public enum AutoCloseBehaviour
{
    Never,
    Always,
    OnDefinedSelect,
    OnCustomSelect
}