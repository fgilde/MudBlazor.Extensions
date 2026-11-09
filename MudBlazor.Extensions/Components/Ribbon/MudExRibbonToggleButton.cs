using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A ribbon command that stays pressed while <see cref="Checked"/> is true.
/// </summary>
public class MudExRibbonToggleButton : MudExRibbonButton
{
    /// <summary>
    /// Whether the button is pressed.
    /// </summary>
    [Parameter] public bool Checked { get; set; }

    /// <summary>
    /// Raised when the user toggles the button.
    /// </summary>
    [Parameter] public EventCallback<bool> CheckedChanged { get; set; }

    /// <inheritdoc />
    protected override bool? Pressed => Checked;

    /// <inheritdoc />
    protected override object StateKey => (base.StateKey, Checked);

    /// <inheritdoc />
    public override async Task ExecuteAsync()
    {
        if (Disabled)
            return;
        Checked = !Checked;
        NotifyStateChanged();
        await CheckedChanged.InvokeAsync(Checked);
        await base.ExecuteAsync();
    }
}
