using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Search field in the header of a <see cref="MudExRibbon"/> that finds commands by text, tooltip or description and runs the chosen one.
/// Rendered by the ribbon, not meant to be used directly.
/// </summary>
public sealed partial class MudExRibbonCommandSearch
{
    private MudAutocomplete<MudExRibbonItemBase> _autocomplete;

    /// <summary>
    /// The ribbon whose commands are searched.
    /// </summary>
    [Parameter] public MudExRibbon Ribbon { get; set; }

    private Task<IEnumerable<MudExRibbonItemBase>> SearchAsync(string text, CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<MudExRibbonItemBase>>(Ribbon.SearchCommands(text));

    private async Task ExecuteAsync(MudExRibbonItemBase item)
    {
        if (item == null)
            return;
        await item.ExecuteCommandAsync();
        await _autocomplete.ClearAsync();
    }
}
