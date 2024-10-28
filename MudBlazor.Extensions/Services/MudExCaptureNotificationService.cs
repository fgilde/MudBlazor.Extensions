using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Components;
using Nextended.Core.Attributes;

namespace MudBlazor.Extensions.Services;

/// <summary>
/// Capture service for capturing audio and video.
/// </summary>
[RegisterAs(typeof(MudExCaptureNotificationService), RegisterAsImplementation = true, ServiceLifetime = ServiceLifetime.Transient)]
internal class MudExCaptureNotificationService : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, Snackbar> _toasts = new();
    private readonly ISnackbar _snackBarService;

    public MudExCaptureNotificationService(IServiceProvider serviceProvider)
    {
        _snackBarService = serviceProvider.GetService<ISnackbar>();
    }

    RenderFragment CaptureInfo(TimeSpan? maxRecordingTime) => builder =>
    {
        int seq = 0;
        builder.OpenComponent(seq++, typeof(CaptureInfoComponent));
        builder.AddAttribute(seq++, nameof(CaptureInfoComponent.MaxRecordingTime), maxRecordingTime);
        builder.CloseComponent();
    };

    public void RemoveRecordingInfo(string id)
    {
        if (_toasts.TryRemove(id, out var toast))
            _snackBarService.Remove(toast);
    }

    public void ShowRecordingInfo(string captureId, TimeSpan? maxRecordingTime, Func<string, Snackbar, Task> onClick)
    {
        _snackBarService.Configuration.PositionClass = Defaults.Classes.Position.TopCenter;

        var toast =_snackBarService.Add(CaptureInfo(maxRecordingTime), Severity.Error, config =>
        {
            config.ShowTransitionDuration = 400;
            config.SnackbarVariant = Variant.Outlined;
            config.Icon = Icons.Material.Filled.Stop;
            config.BackgroundBlurred = true;
            config.RequireInteraction = true;
            config.ShowCloseIcon = false;
            config.Onclick = bar => onClick(captureId, bar);
        }, key: captureId);
        _toasts.TryAdd(captureId, toast);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        foreach (var snackbar in _toasts)
        {
            _snackBarService.Remove(snackbar.Value);
        }
        return ValueTask.CompletedTask;
    }

}