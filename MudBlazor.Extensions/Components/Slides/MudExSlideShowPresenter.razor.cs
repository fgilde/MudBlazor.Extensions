using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>The presenter view of <see cref="MudExSlideShow"/>: the current slide, the next slide or build step, the notes, the time and a stopwatch.</summary>
public partial class MudExSlideShowPresenter
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly CancellationTokenSource _ticks = new();
    private int _notesSize = 20;

    /// <summary>The presentation that is shown.</summary>
    [Parameter] public Presentation Presentation { get; set; }

    /// <summary>The image URLs of the presentation.</summary>
    [Parameter] public SlideImageUrls Images { get; set; }

    /// <summary>The current slide, whose notes are shown; null on the end screen.</summary>
    [Parameter] public Slide Slide { get; set; }

    /// <summary>The index of the current slide.</summary>
    [Parameter] public int SlideIndex { get; set; }

    /// <summary>The slide shown as the next step: the current slide with its next build step played, or the next slide; null at the end.</summary>
    [Parameter] public Slide NextSlide { get; set; }

    /// <summary>The visibility of the animated shapes of <see cref="NextSlide"/>.</summary>
    [Parameter] public IReadOnlyDictionary<(uint Shape, int? Paragraph), SlideTargetState> NextTargets { get; set; }

    /// <summary>The stage the audience sees, shown as the current slide.</summary>
    [Parameter] public RenderFragment Stage { get; set; }

    /// <summary>Raised by the previous button.</summary>
    [Parameter] public EventCallback PreviousRequested { get; set; }

    /// <summary>Raised by the next button.</summary>
    [Parameter] public EventCallback NextRequested { get; set; }

    /// <summary>Raised by the end button.</summary>
    [Parameter] public EventCallback EndRequested { get; set; }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
            _ = TickAsync();
    }

    private async Task TickAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(_ticks.Token))
                await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void ToggleTimer()
    {
        if (_stopwatch.IsRunning)
            _stopwatch.Stop();
        else
            _stopwatch.Start();
    }

    private void RestartTimer()
    {
        if (_stopwatch.IsRunning)
            _stopwatch.Restart();
        else
            _stopwatch.Reset();
    }

    private void Resize(int delta) => _notesSize = Math.Clamp(_notesSize + delta, 10, 48);

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await _ticks.CancelAsync();
        await base.DisposeAsync();
    }
}
