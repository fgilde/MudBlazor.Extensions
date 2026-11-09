using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A split button for colors, for example the font color. The face applies the last chosen color, the arrow opens
/// a palette with "More colors…" for any other color.
/// </summary>
public partial class MudExRibbonColorPicker
{
    private static readonly IReadOnlyList<MudExColor> DefaultPalette =
    [
        Color.Primary, Color.Secondary, Color.Tertiary, Color.Info, Color.Success, Color.Warning, Color.Error, Color.Dark,
        "#C00000", "#FF0000", "#FFC000", "#FFFF00", "#92D050", "#00B050", "#00B0F0", "#0070C0", "#002060", "#7030A0"
    ];

    private bool _moreColors;

    /// <summary>
    /// The last chosen color, drawn as a bar below the icon and applied by the face.
    /// </summary>
    [Parameter] public MudExColor Value { get; set; }

    /// <summary>
    /// Raised when the face is clicked or a color is chosen.
    /// </summary>
    [Parameter] public EventCallback<MudExColor> ValueChanged { get; set; }

    /// <summary>
    /// Colors offered in the dropdown. Defaults to the theme colors followed by ten standard colors.
    /// </summary>
    [Parameter] public IReadOnlyList<MudExColor> Palette { get; set; }

    /// <inheritdoc />
    protected override object StateKey => (base.StateKey, Value, Palette, _moreColors);

    /// <inheritdoc />
    public override Task ExecuteAsync() => Disabled ? Task.CompletedTask : ValueChanged.InvokeAsync(Value);

    private void ToggleMoreColors()
    {
        _moreColors = !_moreColors;
        NotifyStateChanged();
    }

    private Task SelectAsync(MudExColor color, bool keepMoreColors)
    {
        if (Disabled)
            return Task.CompletedTask;
        Value = color;
        _moreColors &= keepMoreColors;
        NotifyStateChanged();
        return ValueChanged.InvokeAsync(color);
    }
}
