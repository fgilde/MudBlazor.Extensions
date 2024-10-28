using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components.Base;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component to move the ChildContent to a specific element that can defined by ElementSelector.
/// </summary>
public partial class CaptureInfoComponent 
{
    [Parameter]
    public TimeSpan? MaxRecordingTime { get; set; }

    private DateTime RecordingStartTime { get; set; } = DateTime.Now;
    private System.Timers.Timer _timer;
    private TimeSpan _elapsedTime;
    private DateTime? _endTime;

    protected override void OnInitialized()
    {
        RecordingStartTime = DateTime.Now;
        if (MaxRecordingTime.HasValue)
        {
            _endTime = RecordingStartTime.Add(MaxRecordingTime.Value);
        }

        _timer = new System.Timers.Timer(1000); // Aktualisiert jede Sekunde
        _timer.Elapsed += (sender, args) =>
        {
            _elapsedTime = DateTime.Now - RecordingStartTime;
            InvokeAsync(StateHasChanged);
        };
        _timer.Start();
    }

    public void Dispose()
    {
        _timer?.Stop();
        _timer?.Dispose();
    }

}