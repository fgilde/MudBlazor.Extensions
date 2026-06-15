namespace MudBlazor.Extensions.Components;

/// <summary>
/// Snap behavior of a <see cref="MudExDateRangeSlider"/>. Controls the resolution at which the start/end values snap.
/// </summary>
public enum DateRangeSliderMode
{
    /// <summary>Snap to whole days (default).</summary>
    Day,
    /// <summary>Snap to ISO weeks (start = Monday).</summary>
    Week,
    /// <summary>Snap to month boundaries (start = 1st of month).</summary>
    Month,
    /// <summary>Snap to quarter boundaries (Jan/Apr/Jul/Oct 1st).</summary>
    Quarter,
    /// <summary>Snap to year boundaries (Jan 1st).</summary>
    Year
}

/// <summary>
/// Snap behavior of a <see cref="MudExTimeRangeSlider"/>.
/// </summary>
public enum TimeRangeSliderMode
{
    /// <summary>Snap to whole minutes (default).</summary>
    Minute,
    /// <summary>Snap to 5-minute steps.</summary>
    FiveMinute,
    /// <summary>Snap to quarter hours (00, 15, 30, 45).</summary>
    QuarterHour,
    /// <summary>Snap to half hours (00, 30).</summary>
    HalfHour,
    /// <summary>Snap to whole hours.</summary>
    Hour
}

/// <summary>
/// Snap behavior of a <see cref="MudExDateTimeRangeSlider"/>.
/// </summary>
public enum DateTimeRangeSliderMode
{
    /// <summary>Snap to whole minutes.</summary>
    Minute,
    /// <summary>Snap to whole hours (default).</summary>
    Hour,
    /// <summary>Snap to whole days.</summary>
    Day,
    /// <summary>Snap to ISO weeks.</summary>
    Week,
    /// <summary>Snap to month boundaries.</summary>
    Month
}

/// <summary>
/// One tick on a range slider track, produced by the adaptive tick engine.
/// </summary>
/// <typeparam name="T">Slider value type.</typeparam>
public readonly record struct RangeTick<T>(T Value, double Percent, bool IsMajor, string Label) where T : IComparable<T>;
