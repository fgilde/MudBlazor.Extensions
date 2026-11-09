using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The sheet tabs of a spreadsheet: a click selects a sheet, a double click renames it, the context menu inserts, deletes, moves,
/// colors, hides and unhides sheets, and dragging a tab reorders the sheets. All changes are undoable workbook commands.
/// </summary>
public partial class MudExSpreadsheetSheetTabs
{
    internal static readonly (string Name, string Color)[] TabColors =
    [
        ("Red", "#C00000"), ("Orange", "#ED7D31"), ("Yellow", "#FFC000"), ("Green", "#70AD47"), ("Dark Green", "#217346"),
        ("Blue", "#4472C4"), ("Dark Blue", "#002060"), ("Purple", "#7030A0"), ("Gray", "#A5A5A5")
    ];

    private static readonly Regex HexColor = new("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant);

    private ElementReference _renameInput;
    private int _renaming = -1;
    private string _renameText;
    private bool _focusRename;
    private int _dragIndex = -1;
    private readonly Dictionary<int, ElementReference> _tabElements = new();
    private bool _focusActive;

    /// <summary>The workbook whose sheets are shown.</summary>
    [Parameter] public Workbook Workbook { get; set; }

    /// <summary>Index of the active sheet in <see cref="Office.Spreadsheet.Workbook.Sheets"/>.</summary>
    [Parameter] public int SheetIndex { get; set; }

    /// <summary>Raised when the user selects another sheet or a new sheet becomes active.</summary>
    [Parameter] public EventCallback<int> SheetIndexChanged { get; set; }

    /// <summary>Whether sheets can only be selected, not changed.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    internal static string NextSheetName(Workbook wb)
    {
        var n = wb.Sheets.Count + 1;
        while (wb.FindSheet("Sheet" + n) != null) n++;
        return "Sheet" + n;
    }

    internal static int NearestVisible(Workbook wb, int index)
    {
        for (var d = 0; d < wb.Sheets.Count; d++)
        {
            if (index + d < wb.Sheets.Count && !wb.Sheets[index + d].Hidden) return index + d;
            if (index - d >= 0 && !wb.Sheets[index - d].Hidden) return index - d;
        }

        return Math.Clamp(index, 0, Math.Max(0, wb.Sheets.Count - 1));
    }

    internal static string SafeColor(string color) => color != null && HexColor.IsMatch(color) ? color : null;

    /// <summary>Adds a new sheet after the active sheet and selects it.</summary>
    /// <returns>A task that completes when the sheet is added.</returns>
    public Task AddSheetAsync() => InsertSheetAsync(SheetIndex + 1);

    /// <summary>Inserts a new sheet at a position and selects it.</summary>
    /// <param name="index">Position of the new sheet.</param>
    /// <returns>A task that completes when the sheet is added.</returns>
    public async Task InsertSheetAsync(int index)
    {
        if (await RunAsync(new AddSheetCommand(NextSheetName(Workbook), index)))
            await SheetIndexChanged.InvokeAsync(Math.Clamp(index, 0, Workbook.Sheets.Count - 1));
    }

    /// <summary>Renames a sheet. Invalid or duplicate names are reported and leave the sheet unchanged.</summary>
    /// <param name="index">Index of the sheet.</param>
    /// <param name="name">The new name.</param>
    /// <returns>A task that completes when the sheet is renamed.</returns>
    public async Task RenameSheetAsync(int index, string name)
    {
        var sheet = Workbook?.Sheets.ElementAtOrDefault(index);
        name = name?.Trim();
        if (sheet != null && !string.IsNullOrEmpty(name) && name != sheet.Name)
            await RunAsync(new RenameSheetCommand(sheet, name));
    }

    /// <summary>Deletes a sheet. The last visible sheet cannot be deleted.</summary>
    /// <param name="index">Index of the sheet.</param>
    /// <returns>A task that completes when the sheet is deleted.</returns>
    public async Task DeleteSheetAsync(int index)
    {
        if (Workbook?.Sheets.ElementAtOrDefault(index) is not { } sheet)
            return;
        var active = Workbook.Sheets.ElementAtOrDefault(SheetIndex);
        if (!await RunAsync(new DeleteSheetCommand(sheet)))
            return;
        var position = Workbook.Sheets.ToList().IndexOf(active);
        await SheetIndexChanged.InvokeAsync(position >= 0 ? position : NearestVisible(Workbook, Math.Min(index, Workbook.Sheets.Count - 1)));
    }

    /// <summary>Moves a sheet to another position.</summary>
    /// <param name="index">Index of the sheet.</param>
    /// <param name="newIndex">Its new position.</param>
    /// <returns>A task that completes when the sheet is moved.</returns>
    public async Task MoveSheetAsync(int index, int newIndex)
    {
        if (Workbook?.Sheets.ElementAtOrDefault(index) is not { } sheet || newIndex < 0 || newIndex == index)
            return;
        var active = Workbook.Sheets.ElementAtOrDefault(SheetIndex);
        if (await RunAsync(new MoveSheetCommand(sheet, newIndex)))
            await SheetIndexChanged.InvokeAsync(Workbook.Sheets.ToList().IndexOf(active));
    }

    /// <summary>Shows the inline editor to rename a sheet.</summary>
    /// <param name="index">Index of the sheet.</param>
    public void BeginRename(int index)
    {
        if (ReadOnly || Workbook?.Sheets.ElementAtOrDefault(index) is not { } sheet)
            return;
        _renaming = index;
        _renameText = sheet.Name;
        _focusRename = true;
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (_focusRename)
        {
            _focusRename = false;
            await TryFocusAsync(_renameInput);
        }

        if (_focusActive && _tabElements.TryGetValue(SheetIndex, out var tab))
        {
            _focusActive = false;
            await TryFocusAsync(tab);
        }
    }

    private static async Task TryFocusAsync(ElementReference element)
    {
        try
        {
            await element.FocusAsync();
        }
        catch (Exception e) when (e is InvalidOperationException or Microsoft.JSInterop.JSException)
        {
        }
    }

    private async Task TabKeyDownAsync(KeyboardEventArgs e, int index)
    {
        var target = e.Key switch
        {
            "ArrowLeft" => PreviousVisible(index),
            "ArrowRight" => NextVisible(index),
            "Home" => NearestVisible(Workbook, 0),
            "End" => NearestVisible(Workbook, Workbook.Sheets.Count - 1),
            "Enter" or " " => index,
            _ => -1
        };
        if (target < 0)
            return;
        _focusActive = true;
        await SelectAsync(target);
    }

    private async Task SelectAsync(int index)
    {
        if (index != SheetIndex)
            await SheetIndexChanged.InvokeAsync(index);
    }

    private async Task RenameKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await CommitRenameAsync();
        else if (e.Key == "Escape")
            _renaming = -1;
    }

    private async Task CommitRenameAsync()
    {
        if (_renaming < 0)
            return;
        var index = _renaming;
        _renaming = -1;
        await RenameSheetAsync(index, _renameText);
    }

    private async Task DropAsync(int index)
    {
        var from = _dragIndex;
        _dragIndex = -1;
        if (!ReadOnly && from >= 0)
            await MoveSheetAsync(from, index);
    }

    private async Task UnhideAsync(Worksheet sheet)
    {
        if (await RunAsync(new SetSheetHiddenCommand(sheet, false)))
            await SheetIndexChanged.InvokeAsync(Workbook.Sheets.ToList().IndexOf(sheet));
    }

    private int PreviousVisible(int index)
    {
        for (var i = index - 1; i >= 0; i--)
            if (!Workbook.Sheets[i].Hidden) return i;
        return -1;
    }

    private int NextVisible(int index)
    {
        for (var i = index + 1; i < Workbook.Sheets.Count; i++)
            if (!Workbook.Sheets[i].Hidden) return i;
        return -1;
    }

    private async Task<bool> RunAsync(IWorkbookCommand command)
    {
        if (ReadOnly || Workbook == null)
            return false;
        try
        {
            Workbook.Execute(command);
            return true;
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            await DialogService.ShowInformationAsync(TryLocalize("Error"), e.Message, Icons.Material.Filled.Error);
            return false;
        }
    }
}
