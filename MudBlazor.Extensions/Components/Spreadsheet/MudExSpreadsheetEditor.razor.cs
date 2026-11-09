using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office.Spreadsheet;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Spreadsheet editor in the style of Excel with ribbon, formula bar, virtualized grid, sheet tabs and status bar.
/// Opens xlsx, xlsm, xltx, legacy xls (saved as xlsx), csv and tsv files and is the editor for these files in
/// <see cref="MudExFileDisplay"/>. Without a file it edits the <see cref="Workbook"/> parameter.
/// </summary>
public partial class MudExSpreadsheetEditor : IMudExFileEditor
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private static readonly MudExFileFormat Xlsx = new("Excel Workbook", "xlsx", XlsxContentType);
    private static readonly MudExFileFormat Xlsm = new("Excel Macro-Enabled Workbook", "xlsm", "application/vnd.ms-excel.sheet.macroEnabled.12");
    private static readonly MudExFileFormat Xltx = new("Excel Template", "xltx", "application/vnd.openxmlformats-officedocument.spreadsheetml.template");
    private static readonly MudExFileFormat Xls = new("Excel 97-2003 Workbook", "xls", "application/vnd.ms-excel");
    private static readonly MudExFileFormat Csv = new("CSV (Comma delimited)", "csv", "text/csv");
    private static readonly MudExFileFormat Tsv = new("Text (Tab delimited)", "tsv", "text/tab-separated-values");
    private static readonly MudExFileFormat[] Readable = [Xlsx, Xlsm, Xltx, Xls, Csv, Tsv];

    private MudExSpreadsheetGrid _grid;
    private Workbook _workbook;
    private Workbook _workbookParameter;
    private CalcEngine _engine;
    private Worksheet _activeSheet;
    private int _sheetIndex;
    private SpreadsheetSelection _selection;
    private SpreadsheetSelection _selectionParameter;
    private MudExFileFormat _format;
    private char _delimiter = ',';
    private double _zoom = 1;
    private bool _gridLines = true;
    private bool _headings = true;
    private bool _formulaBar = true;
    private bool _formulaBarParameter = true;
    private bool _collapsed;
    private bool _collapsedParameter;
    private MudExRibbonMode _mode;
    private MudExRibbonMode _modeParameter;
    private bool _gridRendered;
    private MudExSpreadsheetGrid _observedGrid;
    private MudExSpreadsheetStatusBar _statusBar;
    private readonly EventCallback<SpreadsheetSelection> _selectionChanged;
    private int _version;
    private bool _csvWarned;
    private bool _unshiftedWarned;

    /// <summary>Creates the editor.</summary>
    public MudExSpreadsheetEditor() => _selectionChanged = new EventCallback<SpreadsheetSelection>(null, (Func<SpreadsheetSelection, Task>)OnSelectionChangedAsync);

    /// <summary>The workbook to edit when no file is given. Changes by the user raise <see cref="WorkbookChanged"/>.</summary>
    [Parameter] public Workbook Workbook { get; set; }

    /// <summary>Raised after every change of the workbook and when the editor replaces it (New, Open).</summary>
    [Parameter] public EventCallback<Workbook> WorkbookChanged { get; set; }

    /// <summary>Layout of the ribbon.</summary>
    [Parameter] public MudExRibbonMode RibbonMode { get; set; } = MudExRibbonMode.Classic;

    /// <summary>Whether the ribbon is shown.</summary>
    [Parameter] public bool ShowRibbon { get; set; } = true;

    /// <summary>Color of the ribbon header. Defaults to the Excel green.</summary>
    [Parameter] public MudExColor RibbonColor { get; set; } = "#217346";

    /// <summary>Whether the ribbon starts collapsed to its tab row.</summary>
    [Parameter] public bool RibbonCollapsed { get; set; }

    /// <summary>Additional ribbon tabs rendered after the built-in tabs.</summary>
    [Parameter] public RenderFragment RibbonContent { get; set; }

    /// <summary>Whether the formula bar is shown. The user can toggle it on the View tab.</summary>
    [Parameter] public bool ShowFormulaBar { get; set; } = true;

    /// <summary>Whether the sheet tabs are shown.</summary>
    [Parameter] public bool ShowSheetTabs { get; set; } = true;

    /// <summary>Whether the status bar with the selection summary and the zoom slider is shown.</summary>
    [Parameter] public bool ShowStatusBar { get; set; } = true;

    /// <summary>The selection on the active sheet. Changes from the host are applied.</summary>
    [Parameter] public SpreadsheetSelection Selection { get; set; }

    /// <summary>Raised when the user changes the selection.</summary>
    [Parameter] public EventCallback<SpreadsheetSelection> SelectionChanged { get; set; }

    /// <summary>The workbook currently shown, loaded from a file, created by New or given by <see cref="Workbook"/>.</summary>
    public Workbook CurrentWorkbook => _workbook;

    /// <summary>The grid of the active sheet, or null before the first render.</summary>
    public MudExSpreadsheetGrid Grid => _grid;

    /// <summary>The shown sheet.</summary>
    public Worksheet ActiveSheet => _workbook?.Sheets.ElementAtOrDefault(_sheetIndex);

    /// <inheritdoc />
    public override string Name => nameof(MudExSpreadsheetEditor);

    /// <summary>Wins over the older Excel viewers.</summary>
    public int RenderPriority => 10;

    /// <inheritdoc />
    public override IReadOnlyList<MudExFileFormat> SaveFormats => _format switch
    {
        _ when _format == Xls => [Xlsx, Csv],
        _ when _format == Xlsm || _format == Xltx => [_format, Csv],
        _ when _format == Csv => [Csv, Xlsx],
        _ when _format == Tsv => [Tsv, Xlsx, Csv],
        _ => [Xlsx, Csv]
    };

    /// <inheritdoc />
    protected override IReadOnlyList<MudExFileFormat> OpenFormats => Readable;

    private bool IsReadOnly => ReadOnly || LoadFailed || _workbook == null;

    private SpreadsheetSelection CurrentSelection => _selection ?? SpreadsheetSelection.Single(default);

    private CellStyle ActiveStyle => ActiveSheet is { } sheet ? _workbook.Styles[sheet.EffectiveStyleId(CurrentSelection.Active)] : CellStyle.Default;

    /// <inheritdoc />
    public override Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
        => Task.FromResult(fileDisplayInfos != null && FormatOf(fileDisplayInfos.FileName, fileDisplayInfos.ContentType) != null);

    /// <inheritdoc />
    public override Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
        => Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            { "Sheets", _workbook?.Sheets.Count ?? 0 }
        });

    private static MudExFileFormat FormatOf(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).TrimStart('.');
        return Readable.FirstOrDefault(f => string.Equals(f.Extension, extension, StringComparison.OrdinalIgnoreCase))
               ?? Readable.FirstOrDefault(f => string.Equals(f.ContentType, contentType, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!ReferenceEquals(Workbook, _workbookParameter))
        {
            _workbookParameter = Workbook;
            if (Workbook != null)
                Attach(Workbook);
        }

        if (_workbook == null && FileDisplayInfos == null)
            Attach(Office.Spreadsheet.Workbook.Create());
        if (!Equals(Selection, _selectionParameter))
            _selection = _selectionParameter = Selection;
        if (ShowFormulaBar != _formulaBarParameter)
            _formulaBar = _formulaBarParameter = ShowFormulaBar;
        if (RibbonCollapsed != _collapsedParameter)
            _collapsed = _collapsedParameter = RibbonCollapsed;
        if (RibbonMode != _modeParameter || !IsInitialized)
            _mode = _modeParameter = RibbonMode;
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (!ReferenceEquals(_grid, _observedGrid))
        {
            if (_observedGrid != null)
                _observedGrid.EditChanged -= OnGridEditChanged;
            _observedGrid = _grid;
            if (_grid != null)
                _grid.EditChanged += OnGridEditChanged;
        }

        if (_grid == null || _gridRendered)
            return;
        _gridRendered = true;
        StateHasChanged();
    }

    private void OnGridEditChanged()
    {
        if (!IsDirty && _grid?.HasPendingEdit == true)
            _ = InvokeAsync(MarkDirtyAsync);
    }

    /// <inheritdoc />
    protected override MudExFileContent CreateNew()
    {
        var stream = new MemoryStream();
        XlsxWriter.Write(Office.Spreadsheet.Workbook.Create(), stream);
        stream.Position = 0;
        return new() { FileName = "Book1.xlsx", ContentType = XlsxContentType, Stream = stream };
    }

    /// <inheritdoc />
    protected override async Task LoadContentAsync(MudExFileContent content)
    {
        Attach(null);
        using var buffer = new MemoryStream();
        await content.Stream.CopyToAsync(buffer);
        buffer.Position = 0;
        var format = FormatOf(content.FileName, content.ContentType) ?? Xlsx;
        var delimiter = format == Tsv ? '\t' : ',';
        Workbook wb;
        if (format == Csv || format == Tsv)
        {
            wb = CsvFormat.Read(buffer, format == Tsv ? '\t' : null, out delimiter);
            NameAfterFile(wb.Sheets[0], content.FileName);
        }
        else if (buffer.Length == 0)
            throw new InvalidDataException(TryLocalize("The file is empty."));
        else if (format == Xls)
            wb = XlsReader.Read(buffer);
        else
            wb = XlsxReader.Read(buffer);

        _format = format;
        _delimiter = delimiter;
        Attach(wb);
        await WorkbookChanged.InvokeAsync(wb);
    }

    private static void NameAfterFile(Worksheet sheet, string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        if (name.Length > 31)
            name = name[..31];
        try
        {
            Office.Spreadsheet.Workbook.ValidateSheetName(name, sheet, sheet.Workbook);
            sheet.Name = name;
        }
        catch (ArgumentException)
        {
        }
    }

    /// <inheritdoc />
    public override async Task<MudExFileContent> ExportAsync(MudExFileFormat format = null, CancellationToken cancellationToken = default)
    {
        if (_grid?.IsEditing == true)
            await _grid.CommitEditAsync();
        format ??= SaveFormats[0];
        var stream = new MemoryStream();
        if (format.Extension is "csv" or "tsv")
        {
            if (_workbook.Sheets.Count > 1 && !_csvWarned)
            {
                _csvWarned = true;
                await ShowNoticeAsync(TryLocalize("Only the active sheet \"{0}\" is saved. {1} files cannot hold several sheets.", ActiveSheet.Name, format.Extension.ToUpperInvariant()));
            }

            CsvFormat.Write(ActiveSheet, stream, format.Extension == "tsv" ? '\t' : _format == Csv ? _delimiter : ListSeparator);
        }
        else
            XlsxWriter.Write(_workbook, stream);
        stream.Position = 0;
        return new MudExFileContent
        {
            FileName = Path.ChangeExtension(string.IsNullOrEmpty(FileName) ? "Book1.xlsx" : FileName, format.Extension),
            ContentType = format.ContentType,
            Stream = stream
        };
    }

    private static char ListSeparator => CultureInfo.CurrentCulture.TextInfo.ListSeparator is [var c] ? c : ',';

    private async Task ShowNoticeAsync(string message)
    {
        if (DialogService != null)
            await DialogService.ShowInformationAsync(TryLocalize("Note"), message, Icons.Material.Filled.Info);
    }

    private void Attach(Workbook wb)
    {
        if (ReferenceEquals(wb, _workbook))
            return;
        if (_workbook != null)
            _workbook.Changed -= OnWorkbookChanged;
        _engine?.Dispose();
        _engine = null;
        _workbook = wb;
        _selection = null;
        _version++;
        _csvWarned = _unshiftedWarned = false;
        if (wb == null)
        {
            _activeSheet = null;
            _sheetIndex = 0;
            return;
        }

        _engine = new CalcEngine(wb);
        wb.Changed += OnWorkbookChanged;
        _activeSheet = wb.Sheets.FirstOrDefault(s => !s.Hidden) ?? wb.Sheets.FirstOrDefault();
        _sheetIndex = Math.Max(0, wb.Sheets.ToList().IndexOf(_activeSheet));
    }

    private void OnWorkbookChanged(object sender, WorkbookChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, _workbook))
            return;
        _version++;
        var sheets = _workbook.Sheets.ToList();
        var index = sheets.IndexOf(_activeSheet);
        if (index < 0 || _activeSheet.Hidden)
        {
            index = Math.Clamp(_sheetIndex, 0, sheets.Count - 1);
            index = MudExSpreadsheetSheetTabs.NearestVisible(_workbook, index);
            _activeSheet = sheets[index];
        }

        _sheetIndex = index;
        var warn = !_unshiftedWarned && _workbook.HasUnshiftedReferences && e.Structure.Any(c => c.Kind is StructureChangeKind.RowsInserted
            or StructureChangeKind.RowsDeleted or StructureChangeKind.ColumnsInserted or StructureChangeKind.ColumnsDeleted or StructureChangeKind.SheetRenamed);
        _unshiftedWarned |= warn;
        _ = InvokeAsync(async () =>
        {
            try
            {
                if (warn)
                    await ShowNoticeAsync(TryLocalize("This workbook contains content such as conditional formatting, tables, charts or pivot tables whose references are not updated when rows, columns or sheet names change. Check them before you save."));
                await MarkDirtyAsync();
                await WorkbookChanged.InvokeAsync(_workbook);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(ex.Message);
            }

            StateHasChanged();
        });
    }

    private async Task OnSheetIndexChangedAsync(int index)
    {
        if (_workbook == null || index < 0 || index >= _workbook.Sheets.Count)
            return;
        if (_grid?.IsEditing == true)
            await _grid.CommitEditAsync();
        _sheetIndex = index;
        _activeSheet = _workbook.Sheets[index];
        StateHasChanged();
    }

    private async Task OnSelectionChangedAsync(SpreadsheetSelection selection)
    {
        var style = ActiveStyle;
        _selection = selection;
        if (ShowRibbon && style != ActiveStyle)
            StateHasChanged();
        else
            _statusBar?.ShowSelection(CurrentSelection);
        await SelectionChanged.InvokeAsync(selection);
    }

    private void OnZoomChanged(double zoom) => _zoom = Math.Clamp(Math.Round(zoom, 2), 0.1, 4);

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (_workbook != null)
            _workbook.Changed -= OnWorkbookChanged;
        if (_observedGrid != null)
            _observedGrid.EditChanged -= OnGridEditChanged;
        _engine?.Dispose();
        _engine = null;
        await base.DisposeAsync();
    }
}
