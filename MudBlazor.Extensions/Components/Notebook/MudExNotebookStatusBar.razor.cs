using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>Status bar of the notebook editor: the section, the page position and a zoom slider.</summary>
public partial class MudExNotebookStatusBar
{
    /// <summary>The name of the shown section.</summary>
    [Parameter] public string SectionName { get; set; }

    /// <summary>The number of the shown page in its section, starting at 1.</summary>
    [Parameter] public int PageNumber { get; set; }

    /// <summary>The number of pages in the section.</summary>
    [Parameter] public int PageCount { get; set; }

    /// <summary>Zoom factor, 1 is 100%.</summary>
    [Parameter] public double Zoom { get; set; } = 1;

    /// <summary>Raised when the user changes the zoom.</summary>
    [Parameter] public EventCallback<double> ZoomChanged { get; set; }

    private int Percent => (int)Math.Round(Zoom * 100);

    private Task OnSliderChangedAsync(ChangeEventArgs e)
        => double.TryParse(e.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) ? SetZoomAsync(percent / 100) : Task.CompletedTask;

    private Task SetZoomAsync(double zoom) => ZoomChanged.InvokeAsync(Math.Clamp(Math.Round(zoom, 2), 0.25, 4));
}
