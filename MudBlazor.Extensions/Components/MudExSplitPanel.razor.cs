using Microsoft.AspNetCore.Components;
namespace MudBlazor.Extensions.Components;

/// <summary>
/// A SplitPanel Component 
/// </summary>
public partial class MudExSplitPanel
{
    [Parameter] public RenderFragment Left { get; set; }
    [Parameter] public RenderFragment Right { get; set; }
    [Parameter] public bool ColumnLayout { get; set; } = true;
    [Parameter] public bool Reverse { get; set; } = false;
    [Parameter] public string Class { get; set; }
    [Parameter] public string Style { get; set; }
    [Parameter] public bool UpdateSizesInPercentage { get; set; } = true;
    [Parameter] public bool Splittable { get; set; } = true;

    // Its not a bug. if columnLayout is true flex-direction is row
    public string GetStyle()
    {
        return $"flex-direction:{(ColumnLayout ? "row" : "column")}{(Reverse ? "-reverse" : "")};";
    }
}