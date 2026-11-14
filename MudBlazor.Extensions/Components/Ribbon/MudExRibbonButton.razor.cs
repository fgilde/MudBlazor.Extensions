using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A plain ribbon command.
/// </summary>
public partial class MudExRibbonButton
{
    /// <summary>
    /// Raised when the command is executed.
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>
    /// Pressed state for aria-pressed, null for a button that is not a toggle.
    /// </summary>
    protected virtual bool? Pressed => null;

    /// <inheritdoc />
    public override Task ExecuteAsync() => Disabled ? Task.CompletedTask : OnClick.InvokeAsync();
}
