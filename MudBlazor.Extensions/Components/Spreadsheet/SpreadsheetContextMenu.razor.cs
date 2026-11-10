using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The context menu of <see cref="MudExSpreadsheetEditor"/> for cells, row headings and column headings. It is opened at the pointer by
/// the grid's right click and runs the editor's commands. Content is only built while the menu is open.
/// </summary>
public partial class SpreadsheetContextMenu
{
    private MudMenu _menu;
    private SpreadsheetContextMenuKind? _kind;
    private bool _render;

    /// <summary>The editor whose commands the items run.</summary>
    [Parameter] public MudExSpreadsheetEditor Editor { get; set; }

    /// <summary>Additional items, for example for notes and links, rendered before Format Cells.</summary>
    [Parameter] public RenderFragment<SpreadsheetContextMenuKind> Items { get; set; }

    /// <summary>The variant of the open menu, or null while it is closed.</summary>
    public SpreadsheetContextMenuKind? Kind => _kind;

    /// <summary>Opens the menu at a pointer position.</summary>
    /// <param name="args">Where and on what the menu was requested.</param>
    /// <returns>A task that completes when the menu is open.</returns>
    public async Task OpenAsync(SpreadsheetContextMenuEventArgs args)
    {
        if (_menu == null || Editor == null) return;
        _kind = args.Kind;
        _render = true;
        StateHasChanged();
        await _menu.OpenMenuAsync(new MouseEventArgs { ClientX = args.ClientX, ClientY = args.ClientY });
    }

    /// <inheritdoc />
    protected override bool ShouldRender()
    {
        var render = _render;
        _render = false;
        return render;
    }

    private void OnOpenChanged(bool open)
    {
        if (open) return;
        _kind = null;
        _render = true;
    }
}
