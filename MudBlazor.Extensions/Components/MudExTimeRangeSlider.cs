using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Contracts;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Range slider specialized for <see cref="TimeOnly"/> with theme-aware defaults and a <see cref="TimeRangeSliderMode"/>
/// that controls snap behavior (Minute, FiveMinute, QuarterHour, HalfHour, Hour). Inherits everything from
/// <see cref="MudExRangeSlider{T}"/> – any property of the base class can be set on this component as well.
/// </summary>
public class MudExTimeRangeSlider : MudExRangeSlider<TimeOnly>
{
    private bool _userTrackTemplate;
    private bool _userSelectionTemplate;
    private bool _userThumbStartTemplate;
    private bool _userThumbEndTemplate;
    private bool _userStepResolver;
    private bool _userStartFormatter;
    private bool _userEndFormatter;

    private TimeRangeSliderMode? _cachedTemplateMode;
    private RenderFragment<MudExRangeSliderContext<TimeOnly>>? _cachedTrackTemplate;
    private RenderFragment<MudExRangeSliderContext<TimeOnly>>? _cachedSelectionTemplate;
    private RenderFragment<MudExRangeSliderThumbContext<TimeOnly>>? _cachedThumbTemplate;

    /// <summary>
    /// Snap resolution for start/end values. Switching this also adjusts the default tick density and step resolver.
    /// </summary>
    [Parameter] public TimeRangeSliderMode Mode { get; set; } = TimeRangeSliderMode.Minute;

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        foreach (var p in parameters)
        {
            switch (p.Name)
            {
                case nameof(TrackTemplate):       _userTrackTemplate = true; break;
                case nameof(SelectionTemplate):   _userSelectionTemplate = true; break;
                case nameof(ThumbStartTemplate):  _userThumbStartTemplate = true; break;
                case nameof(ThumbEndTemplate):    _userThumbEndTemplate = true; break;
                case nameof(StepResolver):        _userStepResolver = true; break;
                case nameof(ToStartStringFunc):   _userStartFormatter = true; break;
                case nameof(ToEndStringFunc):     _userEndFormatter = true; break;
            }
        }
        await base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        EnsureTemplateCache(Mode);

        if (!_userStepResolver)
            StepResolver = ResolverFor(Mode);
        if (!_userStartFormatter)
            ToStartStringFunc = r => r.Start.ToString("HH\\:mm", CultureInfo.CurrentCulture);
        if (!_userEndFormatter)
            ToEndStringFunc = r => r.End.ToString("HH\\:mm", CultureInfo.CurrentCulture);
        if (!_userTrackTemplate)
            TrackTemplate = _cachedTrackTemplate;
        if (!_userSelectionTemplate)
            SelectionTemplate = _cachedSelectionTemplate;
        if (!_userThumbStartTemplate)
            ThumbStartTemplate = _cachedThumbTemplate;
        if (!_userThumbEndTemplate)
            ThumbEndTemplate = _cachedThumbTemplate;

        base.OnParametersSet();
    }

    private void EnsureTemplateCache(TimeRangeSliderMode mode)
    {
        if (_cachedTemplateMode == mode && _cachedTrackTemplate != null) return;
        _cachedTemplateMode = mode;
        _cachedTrackTemplate = MudExRangeSliderDefaults.TimeTrack(mode);
        _cachedSelectionTemplate = MudExRangeSliderDefaults.TimeSelection();
        _cachedThumbTemplate = MudExRangeSliderDefaults.Thumb<TimeOnly>();
    }

    private static Func<TimeOnly, IRange<TimeOnly>, RangeLength<TimeOnly>, int, SnapPolicy, TimeOnly> ResolverFor(TimeRangeSliderMode mode) => mode switch
    {
        TimeRangeSliderMode.Hour        => StepResolvers.TimeOnly.Hourly(),
        TimeRangeSliderMode.HalfHour    => StepResolvers.TimeOnly.Minutely(30),
        TimeRangeSliderMode.QuarterHour => StepResolvers.TimeOnly.Minutely(15),
        TimeRangeSliderMode.FiveMinute  => StepResolvers.TimeOnly.Minutely(5),
        _                               => StepResolvers.TimeOnly.Minutely()
    };
}
