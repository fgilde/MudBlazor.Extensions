using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Search field in the header of a <see cref="MudExRibbon"/> that finds commands by text, tooltip or description and runs the chosen one.
/// Rendered by the ribbon, not meant to be used directly.
/// </summary>
public sealed partial class MudExRibbonCommandSearch
{
    private MudAutocomplete<MudExRibbonItemBase> _autocomplete;
    private bool _expanded;
    private bool _open;
    private bool _focusing;

    /// <summary>
    /// The ribbon whose commands are searched.
    /// </summary>
    [Parameter] public MudExRibbon Ribbon { get; set; }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_focusing)
            return;
        try
        {
            await _autocomplete.FocusAsync();
        }
        catch (Exception e) when (e is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException)
        {
        }
        finally
        {
            _focusing = false;
        }
    }

    private void Expand() => _expanded = _focusing = true;

    private void OnFocusOut()
    {
        if (!_focusing && !_open)
            _expanded = false;
    }

    private void OnOpenChanged(bool open)
    {
        _open = open;
        if (!open && !_focusing)
            _expanded = false;
    }

    private Task<IEnumerable<MudExRibbonItemBase>> SearchAsync(string text, CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<MudExRibbonItemBase>>(Ribbon.SearchCommands(text));

    private async Task ExecuteAsync(MudExRibbonItemBase item)
    {
        if (item == null)
            return;
        _expanded = false;
        await item.ExecuteCommandAsync();
        await _autocomplete.ClearAsync();
    }
}
