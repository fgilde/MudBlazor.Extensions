using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Office.Presentation;
using AnimationPreset = MudBlazor.Extensions.Office.Presentation.AnimationPreset;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Plays a presentation as a slide show: full screen on a black stage with the slide transitions and build animations of the file. The keyboard follows
/// PowerPoint (Right, Space, Enter and N advance; Left, Backspace and P go back; a number and Enter jumps; B and W blank the screen; Esc ends), slides
/// advance by themselves after their <see cref="SlideTransition.AdvanceAfterSeconds"/>, and hidden slides are skipped. With <see cref="PresenterView"/>
/// the audience sees the show in a second browser window while this window shows the presenter view with notes, the next slide and timers.
/// The component can be used on its own; the host removes it when <see cref="Closed"/> is raised.
/// </summary>
public partial class MudExSlideShow
{
    private readonly EventCallback<KeyboardEventArgs> _keyDown;
    private SlideShowState _state;
    private SlideImageUrls _images;
    private IReadOnlyDictionary<(uint Shape, int? Paragraph), SlideTargetState> _targets;
    private (Slide Slide, IReadOnlyDictionary<(uint Shape, int? Paragraph), SlideTargetState> Targets)? _previous;
    private (Slide Slide, IReadOnlyDictionary<(uint Shape, int? Paragraph), SlideTargetState> Targets) _next;
    private SlideTransition _transition;
    private double _running;
    private TimeSpan? _utcOffset;
    private bool _presenter;
    private bool _popupBlocked;
    private bool _closed;
    private bool _jsStarted;
    private CancellationTokenSource _timer;
    private ElementReference _root;
    private readonly JsModule<MudExSlideShow> _js = new();

    /// <summary>Creates the slide show.</summary>
    public MudExSlideShow()
    {
        _keyDown = new EventCallback<KeyboardEventArgs>(WithoutRender.Instance, (Func<KeyboardEventArgs, Task>)OnKeyDownAsync);
    }

    /// <summary>The presentation to show.</summary>
    [Parameter] public Presentation Presentation { get; set; }

    /// <summary>The index of the slide the show starts with; a hidden slide is skipped.</summary>
    [Parameter] public int StartIndex { get; set; }

    /// <summary>
    /// If true the audience sees the show in a second browser window and this window shows the presenter view. If the browser blocks the window, a
    /// message appears and the show runs in this window.
    /// </summary>
    [Parameter] public bool PresenterView { get; set; }

    /// <summary>
    /// If true the show plays the transition and all build steps of the start slide once inside its container, without full screen and keyboard,
    /// and raises <see cref="Closed"/> afterwards or on a click.
    /// </summary>
    [Parameter] public bool Preview { get; set; }

    /// <summary>The image URLs of the presentation, for example those of the editor; without them pictures are shown from data URIs.</summary>
    [Parameter] public SlideImageUrls Images { get; set; }

    /// <summary>Raised when the show ends: by Esc, by a click on the end screen, when the audience window or full screen is closed, or when a preview finished.</summary>
    [Parameter] public EventCallback Closed { get; set; }

    /// <summary>Raised with the index of the slide whenever the show moves to another slide.</summary>
    [Parameter] public EventCallback<int> SlideChanged { get; set; }

    private string RatioStyle => $"--mud-ex-show-ratio:{(Presentation.SlideWidth / (double)Presentation.SlideHeight).ToString(CultureInfo.InvariantCulture)}";

    private string TransitionStyle => _transition == null ? null : $"animation-duration:{_transition.DurationSeconds.ToString(CultureInfo.InvariantCulture)}s";

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        ArgumentNullException.ThrowIfNull(Presentation);
        _images = Images ?? new SlideImageUrls(Presentation);
        _state = new SlideShowState(Presentation.Slides, StartIndex);
        if (Preview)
            _state.GoTo(StartIndex);
        Arrive(true, -1, false);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (Preview || _jsStarted)
            return;
        _jsStarted = true;
        try
        {
            await _root.FocusAsync(true);
        }
        catch (Exception e) when (e is JSException or JSDisconnectedException or InvalidOperationException)
        {
        }

        try
        {
            if (await _js.StartAsync(JsRuntime, this, null, dotnet => [_root, dotnet]) is not { } js || _closed)
            {
                await _js.DisposeAsync();
                _popupBlocked = PresenterView && !_closed;
                return;
            }

            if (!PresenterView)
                await js.InvokeVoidAsync("start");
            else if (await js.InvokeAsync<bool>("openAudience", TryLocalize("Slide Show")))
            {
                _presenter = true;
                _utcOffset = TimeSpan.FromMinutes(await js.InvokeAsync<int>("utcOffset"));
            }
            else
            {
                _popupBlocked = true;
                await js.InvokeVoidAsync("start");
            }
        }
        catch (Exception e) when (e is JSException or JSDisconnectedException or InvalidOperationException)
        {
            _popupBlocked = PresenterView;
        }

        if (PresenterView)
            StateHasChanged();
    }

    /// <summary>Called by the browser with the result of a key or click in the show.</summary>
    /// <param name="command">next, click, previous, first, last, goto or exit.</param>
    /// <param name="number">For goto: the number of the slide, starting at 1.</param>
    /// <returns>A task that completes when the show has moved.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnShowCommand(string command, int number) => InvokeAsync(() => command switch
    {
        "next" => _state.IsEnd ? CloseAsync() : MoveAsync(_state.Next, true),
        "click" => ClickAsync(),
        "previous" => MoveAsync(_state.Previous, false),
        "first" => MoveAsync(_state.First, true),
        "last" => MoveAsync(_state.Last, true),
        "goto" => MoveAsync(() => _state.GoTo(number - 1), true),
        "exit" => CloseAsync(),
        _ => Task.CompletedTask
    });

    private Task ClickAsync()
    {
        if (_state.IsEnd)
            return CloseAsync();
        return _state.FullyBuilt && !_state.Slide.Transition.AdvanceOnClick ? Task.CompletedTask : MoveAsync(_state.Next, true);
    }

    private Task OnKeyDownAsync(KeyboardEventArgs e) => e.CtrlKey || e.AltKey || e.MetaKey ? Task.CompletedTask : e.Key switch
    {
        "Escape" => CloseAsync(),
        "ArrowRight" or "ArrowDown" or " " or "PageDown" or "n" or "N" or "Enter" => OnShowCommand("next", 0),
        "ArrowLeft" or "ArrowUp" or "Backspace" or "PageUp" or "p" or "P" => OnShowCommand("previous", 0),
        _ => Task.CompletedTask
    };

    private async Task MoveAsync(Action move, bool forward)
    {
        if (_closed)
            return;
        var (slide, step, end) = (_state.Slide, _state.Step, _state.IsEnd);
        move();
        if (ReferenceEquals(slide, _state.Slide) && step == _state.Step && end == _state.IsEnd)
            return;
        _popupBlocked = false;
        if (ReferenceEquals(slide, _state.Slide))
        {
            _previous = null;
            _transition = null;
            Arrive(false, _state.Step > step ? step : -1, true);
        }
        else
        {
            var targets = _targets;
            Arrive(forward, -1, false);
            _previous = _transition != null && slide != null && _state.Slide != null ? (slide, targets) : null;
            if (_state.Slide != null)
                await SlideChanged.InvokeAsync(_state.SlideIndex);
        }

        StateHasChanged();
    }

    private void Arrive(bool forward, int animate, bool sameSlide)
    {
        var slide = _state.Slide;
        if (!sameSlide)
        {
            _transition = forward && slide != null ? Effective(slide.Transition) : null;
            _previous = null;
            animate = forward && _state.Step == 1 && _state.Steps[0].Automatic ? 0 : -1;
        }

        var offset = _transition?.DurationSeconds ?? 0;
        _targets = slide == null ? null : Targets(slide, _state.Steps, _state.Step, animate, offset);
        _running = offset + (animate >= 0 ? _state.Steps[animate].DurationSeconds : 0);
        var (next, nextStep) = _state.Peek();
        _next = (next, next == null ? null : Targets(next, SlideBuildSteps.For(next), nextStep, -1, 0));
        Schedule();
    }

    internal static IReadOnlyDictionary<(uint Shape, int? Paragraph), SlideTargetState> Targets(Slide slide, IReadOnlyList<BuildStep> steps, int step, int animate, double offset)
    {
        var result = SlideBuildSteps.VisibleAfter(slide, step).ToDictionary(v => v.Key, v => new SlideTargetState(!v.Value));
        if (animate < 0 || animate >= steps.Count)
            return result;
        var before = SlideBuildSteps.VisibleAfter(slide, animate);
        foreach (var scheduled in steps[animate].Effects)
        {
            var key = (scheduled.Effect.ShapeId, scheduled.Effect.ParagraphIndex);
            if (scheduled.Effect.Preset == AnimationPreset.Other && before.GetValueOrDefault(key, true))
                continue;
            result[key] = result[key] with { Effect = scheduled.Effect, StartSeconds = scheduled.StartSeconds + offset };
        }

        return result;
    }

    private static SlideTransition Effective(SlideTransition transition) => transition.Kind switch
    {
        TransitionKind.None or TransitionKind.Cut => null,
        TransitionKind.Other => transition with { Kind = TransitionKind.Fade },
        _ => transition
    };

    private string FrameClass(string phase)
    {
        var name = _transition == null ? null : _transition.Kind.ToString().ToLowerInvariant() + (_transition.Direction == TransitionDirection.None ? "" : "-" + _transition.Direction.ToString().ToLowerInvariant());
        return $"mud-ex-slide-show-frame mud-ex-slide-show-{phase}" + (name == null ? "" : $" mud-ex-show-{phase}-{name}");
    }

    private void Schedule()
    {
        _timer?.Cancel();
        _timer = null;
        double? delay = Preview
            ? _running + (_state.FullyBuilt ? 0.5 : 0.1)
            : _state.FullyBuilt && _state.Slide.Transition.AdvanceAfterSeconds is { } after ? _running + after : null;
        if (delay is not { } seconds)
            return;
        var timer = _timer = new CancellationTokenSource();
        _ = AdvanceLaterAsync(seconds, timer.Token);
    }

    private async Task AdvanceLaterAsync(double seconds, CancellationToken token)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds), token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await InvokeAsync(() => token.IsCancellationRequested ? Task.CompletedTask
            : Preview && (_state.FullyBuilt || _state.IsEnd) ? CloseAsync() : MoveAsync(_state.Next, true));
    }

    private async Task CloseAsync()
    {
        if (_closed)
            return;
        _closed = true;
        _timer?.Cancel();
        await Closed.InvokeAsync();
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        _timer?.Cancel();
        await _js.DisposeAsync();
        await base.DisposeAsync();
    }
}
