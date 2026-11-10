using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>Status bar of the slide editor: the slide position and a zoom slider.</summary>
public partial class MudExSlideStatusBar
{
    /// <summary>The number of the shown slide, starting at 1.</summary>
    [Parameter] public int SlideNumber { get; set; }

    /// <summary>The number of slides.</summary>
    [Parameter] public int SlideCount { get; set; }

    /// <summary>Zoom factor, 1 is 100%; null fits the slide into the window.</summary>
    [Parameter] public double? Zoom { get; set; }

    /// <summary>Raised when the user changes the zoom; null means fit to window.</summary>
    [Parameter] public EventCallback<double?> ZoomChanged { get; set; }

    private double Value => Zoom ?? 1;

    private int Percent => (int)Math.Round(Value * 100);

    private Task OnSliderChangedAsync(ChangeEventArgs e)
        => double.TryParse(e.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) ? SetZoomAsync(percent / 100) : Task.CompletedTask;

    private Task SetZoomAsync(double zoom) => ZoomChanged.InvokeAsync(Math.Clamp(Math.Round(zoom, 2), 0.1, 4));
}
