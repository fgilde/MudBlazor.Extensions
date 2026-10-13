using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A cron expression built from presets (every n minutes, hourly, daily, weekly, monthly) or typed; says in words when it runs.
/// The bound string only changes to valid expressions, an invalid one stays in the text field with an error.
/// </summary>
public partial class MudExCronEdit : MudExBaseComponent<MudExCronEdit>
{
    private static readonly Dictionary<MudExCronFrequency, string> FrequencyNames = new()
    {
        [MudExCronFrequency.Minutes] = "Every few minutes",
        [MudExCronFrequency.Hourly] = "Hourly",
        [MudExCronFrequency.Daily] = "Daily",
        [MudExCronFrequency.Weekly] = "Weekly",
        [MudExCronFrequency.Monthly] = "Monthly",
        [MudExCronFrequency.Custom] = "Custom",
    };

    private MudExCronSchedule _schedule = new(MudExCronFrequency.Custom);
    private string _text;
    private bool _valid = true;
    private string _shown;

    /// <summary>
    /// The cron expression (five fields, or six with seconds first).
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string Value { get; set; }

    /// <summary>
    /// Called with every valid expression the user builds or types.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>
    /// Caption above the editor.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string Label { get; set; }

    /// <summary>
    /// Text below the editor.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string HelperText { get; set; }

    /// <summary>
    /// Set to true to show the value without allowing changes.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Set to true to disable the editor.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool Disabled { get; set; }

    /// <summary>
    /// The variant of the input fields.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public Variant Variant { get; set; } = Variant.Text;

    /// <summary>
    /// The margin of the input fields.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public Margin Margin { get; set; } = Margin.None;

    /// <summary>
    /// Set to true for compact input fields.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public bool Dense { get; set; }

    /// <summary>
    /// Set to true to mark the time as UTC in its label. The descriptions always say UTC, as cron servers mostly run on it.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public bool TimeIsUtc { get; set; } = true;

    private static CultureInfo Culture => CultureInfo.CurrentUICulture;

    private static IEnumerable<DayOfWeek> Week =>
        Enumerable.Range(0, 7).Select(i => (DayOfWeek)(((int)Culture.DateTimeFormat.FirstDayOfWeek + i) % 7));

    private string Classname => new MudExCssBuilder("mud-ex-cron-edit my-2").AddClass(Class).Build();

    private string LabelText => TryLocalize(Label);

    private string TimeLabel => TryLocalize(TimeIsUtc ? "Time (UTC)" : "Time");

    private string Description => _valid ? _schedule.Describe((key, args) => TryLocalize(key, args), Culture) : null;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Value != _shown)
        {
            _shown = Value;
            Show(Value);
        }
    }

    private void Show(string text)
    {
        _text = text;
        _valid = MudExCronSchedule.IsValid(text);
        _schedule = MudExCronSchedule.Parse(text);
    }

    // an invalid expression stays in the field only; the bound value keeps the last valid one
    private async Task SetTextAsync(string text)
    {
        Show(text?.Trim());
        if (_valid)
            await EmitAsync(_text);
    }

    private Task SetFrequencyAsync(MudExCronFrequency frequency) => UpdateAsync(_schedule with { Frequency = frequency });

    private Task SetTimeAsync(TimeSpan? time) =>
        time is { } t ? UpdateAsync(_schedule with { Hour = t.Hours, Minute = t.Minutes }) : Task.CompletedTask;

    private Task SetDaysAsync(IReadOnlyCollection<DayOfWeek> days) =>
        days is { Count: > 0 } ? UpdateAsync(_schedule with { Days = [.. days] }) : Task.CompletedTask;

    private async Task UpdateAsync(MudExCronSchedule schedule)
    {
        _schedule = schedule;
        if (schedule.ToExpression() is { } expression)
        {
            _text = expression;
            _valid = true;
            await EmitAsync(expression);
        }
    }

    private async Task EmitAsync(string expression)
    {
        Value = _shown = expression;
        await ValueChanged.InvokeAsync(expression);
    }
}
