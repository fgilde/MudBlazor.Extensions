using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A labelled number field with a spinner, for example "Before: 6 pt". Values are clamped to
/// <see cref="Min"/>..<see cref="Max"/> and snapped to <see cref="Step"/>; typed text commits on Enter or blur.
/// </summary>
public partial class MudExRibbonNumberBox
{
    private int _inputVersion;

    /// <summary>
    /// The current value.
    /// </summary>
    [Parameter] public double Value { get; set; }

    /// <summary>
    /// Raised when the user commits a new value.
    /// </summary>
    [Parameter] public EventCallback<double> ValueChanged { get; set; }

    /// <summary>
    /// Smallest allowed value.
    /// </summary>
    [Parameter] public double Min { get; set; }

    /// <summary>
    /// Largest allowed value.
    /// </summary>
    [Parameter] public double Max { get; set; } = double.MaxValue;

    /// <summary>
    /// Increment of the spinner and the grid values snap to, starting at <see cref="Min"/>.
    /// </summary>
    [Parameter] public double Step { get; set; } = 1;

    /// <summary>
    /// Unit shown after the value, for example "pt". It may be typed along with the value.
    /// </summary>
    [Parameter] public string Unit { get; set; }

    /// <inheritdoc />
    public override bool IsCommand => false;

    /// <inheritdoc />
    protected override object StateKey => (base.StateKey, Value, Min, Max, Step, Unit, _inputVersion);

    private string DisplayText
    {
        get
        {
            var value = Value.ToString(CultureInfo.CurrentCulture);
            if (string.IsNullOrEmpty(Unit))
                return value;
            return char.IsLetter(Unit[0]) ? $"{value} {Unit}" : value + Unit;
        }
    }

    internal double Snap(double value)
    {
        if (Step > 0)
            value = Min + Math.Round((value - Min) / Step, MidpointRounding.AwayFromZero) * Step;
        return Math.Round(Math.Min(Math.Max(value, Min), Max), 10);
    }

    private Task SpinAsync(int direction) => CommitAsync(Value + direction * Step, null);

    private Task OnKeyDownAsync(KeyboardEventArgs args) => args.Key switch
    {
        "ArrowUp" => SpinAsync(1),
        "ArrowDown" => SpinAsync(-1),
        _ => Task.CompletedTask
    };

    private Task OnInputChangedAsync(ChangeEventArgs args)
    {
        var typed = args.Value?.ToString() ?? string.Empty;
        var text = typed.Trim();
        if (!string.IsNullOrEmpty(Unit) && text.EndsWith(Unit, StringComparison.OrdinalIgnoreCase))
            text = text[..^Unit.Length].Trim();
        var parsed = double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var value)
                     || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        return CommitAsync(parsed && double.IsFinite(value) ? value : Value, typed);
    }

    private async Task CommitAsync(double value, string typed)
    {
        if (Disabled)
            return;
        var snapped = Snap(value);
        if (snapped.Equals(Value))
        {
            if (typed != null && typed != DisplayText)
            {
                _inputVersion++;
                NotifyStateChanged();
            }
            return;
        }
        Value = snapped;
        NotifyStateChanged();
        await ValueChanged.InvokeAsync(Value);
    }
}
