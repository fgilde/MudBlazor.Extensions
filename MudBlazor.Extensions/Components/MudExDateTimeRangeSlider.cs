using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Contracts;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Range slider specialized for <see cref="DateTime"/> with theme-aware defaults and a <see cref="DateTimeRangeSliderMode"/>
/// that controls snap behavior (Minute, Hour, Day, Week, Month). Inherits everything from <see cref="MudExRangeSlider{T}"/> –
/// any property of the base class can be set on this component as well.
/// </summary>
public class MudExDateTimeRangeSlider : MudExRangeSlider<DateTime>
{
    private bool _userTrackTemplate;
    private bool _userSelectionTemplate;
    private bool _userThumbStartTemplate;
    private bool _userThumbEndTemplate;
    private bool _userStepResolver;
    private bool _userStartFormatter;
    private bool _userEndFormatter;

    private DateTimeRangeSliderMode? _cachedTemplateMode;
    private RenderFragment<MudExRangeSliderContext<DateTime>>? _cachedTrackTemplate;
    private RenderFragment<MudExRangeSliderContext<DateTime>>? _cachedSelectionTemplate;
    private RenderFragment<MudExRangeSliderThumbContext<DateTime>>? _cachedThumbTemplate;

    /// <summary>
    /// Snap resolution for start/end values. Switching this also adjusts the default labels and step resolver.
    /// </summary>
    [Parameter] public DateTimeRangeSliderMode Mode { get; set; } = DateTimeRangeSliderMode.Hour;

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
            ToStartStringFunc = r => Format(r.Start, Mode);
        if (!_userEndFormatter)
            ToEndStringFunc = r => Format(r.End, Mode);
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

    private void EnsureTemplateCache(DateTimeRangeSliderMode mode)
    {
        if (_cachedTemplateMode == mode && _cachedTrackTemplate != null) return;
        _cachedTemplateMode = mode;
        _cachedTrackTemplate = MudExRangeSliderDefaults.DateTimeTrack(mode);
        _cachedSelectionTemplate = MudExRangeSliderDefaults.DateTimeSelection(mode);
        _cachedThumbTemplate = MudExRangeSliderDefaults.Thumb<DateTime>();
    }

    private static Func<DateTime, IRange<DateTime>, RangeLength<DateTime>, int, SnapPolicy, DateTime>? ResolverFor(DateTimeRangeSliderMode mode) => mode switch
    {
        DateTimeRangeSliderMode.Month   => StepResolvers.DateTime.Monthly(),
        DateTimeRangeSliderMode.Week    => StepResolvers.DateTime.Weekly(),
        DateTimeRangeSliderMode.Day     => StepResolvers.DateTime.Daily(),
        DateTimeRangeSliderMode.Hour    => StepResolvers.DateTime.Hourly(),
        _                               => StepResolvers.DateTime.Minutely()
    };

    private static string Format(DateTime d, DateTimeRangeSliderMode mode) => mode switch
    {
        DateTimeRangeSliderMode.Month  => d.ToString("MMM yyyy", CultureInfo.CurrentCulture),
        DateTimeRangeSliderMode.Week   => $"KW{ISOWeek.GetWeekOfYear(d)} / {d.Year}",
        DateTimeRangeSliderMode.Day    => d.ToString("d", CultureInfo.CurrentCulture),
        DateTimeRangeSliderMode.Hour   => d.ToString("dd.MM HH:00", CultureInfo.CurrentCulture),
        _                              => d.ToString("g", CultureInfo.CurrentCulture)
    };
}
