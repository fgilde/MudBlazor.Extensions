using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

internal sealed class SlideShowState
{
    private readonly IReadOnlyList<Slide> _slides;
    private IReadOnlyList<BuildStep> _steps = [];

    public SlideShowState(IReadOnlyList<Slide> slides, int start)
    {
        _slides = slides;
        var index = Visible(Math.Max(0, start), 1) ?? Visible(Math.Min(start, slides.Count - 1), -1);
        if (index is { } i)
            Enter(i, false);
        else
            IsEnd = true;
    }

    public int SlideIndex { get; private set; }

    public int Step { get; private set; }

    public bool IsEnd { get; private set; }

    public Slide Slide => IsEnd ? null : _slides[SlideIndex];

    public IReadOnlyList<BuildStep> Steps => _steps;

    public bool FullyBuilt => !IsEnd && Step >= _steps.Count;

    public int SlideCount => _slides.Count;

    public void Next()
    {
        if (IsEnd)
            return;
        if (Step < _steps.Count)
            Step++;
        else if (Visible(SlideIndex + 1, 1) is { } next)
            Enter(next, false);
        else
            IsEnd = true;
    }

    public void Previous()
    {
        if (IsEnd)
        {
            if (Visible(_slides.Count - 1, -1) is { } last)
                Enter(last, true);
            return;
        }

        if (Step > InitialStep(_steps))
            Step--;
        else if (Visible(SlideIndex - 1, -1) is { } previous)
            Enter(previous, true);
    }

    public void GoTo(int slide)
    {
        if (slide >= 0 && slide < _slides.Count)
            Enter(slide, false);
    }

    public void First()
    {
        if (Visible(0, 1) is { } first)
            Enter(first, false);
    }

    public void Last()
    {
        if (Visible(_slides.Count - 1, -1) is { } last)
            Enter(last, false);
    }

    public (Slide Slide, int Step) Peek()
    {
        if (IsEnd)
            return (null, 0);
        if (Step < _steps.Count)
            return (Slide, Step + 1);
        return Visible(SlideIndex + 1, 1) is { } next ? (_slides[next], InitialStep(SlideBuildSteps.For(_slides[next]))) : (null, 0);
    }

    internal static int InitialStep(IReadOnlyList<BuildStep> steps) => steps.Count > 0 && steps[0].Automatic ? 1 : 0;

    private void Enter(int index, bool built)
    {
        IsEnd = false;
        SlideIndex = index;
        _steps = SlideBuildSteps.For(_slides[index]);
        Step = built ? _steps.Count : InitialStep(_steps);
    }

    private int? Visible(int from, int direction)
    {
        for (var i = from; i >= 0 && i < _slides.Count; i += direction)
            if (!_slides[i].Hidden)
                return i;
        return null;
    }
}
