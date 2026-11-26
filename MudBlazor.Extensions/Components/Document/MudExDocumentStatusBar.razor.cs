using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>Status bar of the document editor: the word count and a zoom slider.</summary>
public partial class MudExDocumentStatusBar
{
    /// <summary>The current page and the number of pages; shown when the total is above zero.</summary>
    [Parameter] public (int Current, int Total) Pages { get; set; }

    /// <summary>The number of words in the document.</summary>
    [Parameter] public int Words { get; set; }

    /// <summary>Zoom factor, 1 is 100%.</summary>
    [Parameter] public double Zoom { get; set; } = 1;

    /// <summary>Raised when the user changes the zoom.</summary>
    [Parameter] public EventCallback<double> ZoomChanged { get; set; }

    private int Percent => (int)Math.Round(Zoom * 100);

    private Task OnSliderChangedAsync(ChangeEventArgs e)
        => double.TryParse(e.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) ? SetZoomAsync(percent / 100) : Task.CompletedTask;

    private Task SetZoomAsync(double zoom) => ZoomChanged.InvokeAsync(ClampZoom(zoom));

    internal static double ClampZoom(double zoom) => double.IsFinite(zoom) ? Math.Clamp(Math.Round(zoom, 2), 0.1, 5) : 1;
}
