using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Contracts;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Range slider specialized for <see cref="DateOnly"/> with theme-aware defaults and a <see cref="DateRangeSliderMode"/>
/// that controls snap behavior (Day, Week, Month, Quarter, Year). Inherits everything from <see cref="MudExRangeSlider{T}"/> –
/// any property of the base class (SizeRange, Value, MinLength, …) can be set on this component as well.
/// </summary>
public class MudExDateRangeSlider : MudExRangeSlider<DateOnly>
{
    private bool _userTrackTemplate;
    private bool _userSelectionTemplate;
    private bool _userThumbStartTemplate;
    private bool _userThumbEndTemplate;
    private bool _userStepResolver;
    private bool _userStartFormatter;
    private bool _userEndFormatter;

    private DateRangeSliderMode? _cachedTemplateMode;
    private RenderFragment<MudExRangeSliderContext<DateOnly>>? _cachedTrackTemplate;
    private RenderFragment<MudExRangeSliderContext<DateOnly>>? _cachedSelectionTemplate;
    private RenderFragment<MudExRangeSliderThumbContext<DateOnly>>? _cachedThumbTemplate;

    /// <summary>
    /// Snap resolution for start/end values. Switching this also adjusts the default labels and step resolver.
    /// </summary>
    [Parameter] public DateRangeSliderMode Mode { get; set; } = DateRangeSliderMode.Day;

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

    private void EnsureTemplateCache(DateRangeSliderMode mode)
    {
        if (_cachedTemplateMode == mode && _cachedTrackTemplate != null) return;
        _cachedTemplateMode = mode;
        _cachedTrackTemplate = MudExRangeSliderDefaults.DateTrack(mode);
        _cachedSelectionTemplate = MudExRangeSliderDefaults.DateSelection(mode);
        _cachedThumbTemplate = MudExRangeSliderDefaults.Thumb<DateOnly>();
    }

    private static Func<DateOnly, IRange<DateOnly>, RangeLength<DateOnly>, int, SnapPolicy, DateOnly>? ResolverFor(DateRangeSliderMode mode) => mode switch
    {
        DateRangeSliderMode.Week    => StepResolvers.DateOnly.Weekly(),
        DateRangeSliderMode.Month   => StepResolvers.DateOnly.Monthly(),
        DateRangeSliderMode.Quarter => StepResolvers.DateOnly.Quarterly(),
        DateRangeSliderMode.Year    => StepResolvers.DateOnly.Yearly(),
        _                           => null
    };

    private static string Format(DateOnly d, DateRangeSliderMode mode) => mode switch
    {
        DateRangeSliderMode.Year    => d.ToString("yyyy", CultureInfo.CurrentCulture),
        DateRangeSliderMode.Quarter => $"Q{((d.Month - 1) / 3) + 1} {d.Year}",
        DateRangeSliderMode.Month   => d.ToString("MMM yyyy", CultureInfo.CurrentCulture),
        DateRangeSliderMode.Week    => $"KW{ISOWeek.GetWeekOfYear(d.ToDateTime(TimeOnly.MinValue))} / {d.Year}",
        _                           => d.ToString("d", CultureInfo.CurrentCulture)
    };
}
