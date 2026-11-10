using System.ComponentModel;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Virtualized spreadsheet grid for one worksheet of a <see cref="Office.Spreadsheet.Workbook"/>: renders only the visible cells plus a
/// buffer, supports Excel-style selection, keyboard navigation, in-cell editing, clipboard, fill and resizing. All edits go
/// through workbook commands and can be undone. Pointer drags, resizing and filling run in the browser; only their results
/// reach .NET, so the grid stays responsive in Blazor Server.
/// </summary>
public partial class MudExSpreadsheetGrid
{
    private const int RowBuffer = 10;
    private const int ColumnBuffer = 3;
    private const double HeadingHeight = 20;
    private static readonly string[] PaneNames = ["main", "left", "top", "corner"];

    private readonly Dictionary<Worksheet, SpreadsheetSelection> _sheetSelections = new();
    private readonly Dictionary<int, long> _rowStamps = new();
    private readonly Dictionary<int, CellLook> _looks = new();
    private Workbook _workbook;
    private CalcEngine _engine;
    private CalcEngine _engineParameter;
    private CalcEngine _ownedEngine;
    private Worksheet _sheet;
    private SpreadsheetAxis _rows = new(Worksheet.StandardRowHeight, new Dictionary<int, double>(), new HashSet<int>(), 1);
    private SpreadsheetAxis _cols = new(Worksheet.StandardColumnWidth, new Dictionary<int, double>(), new HashSet<int>(), 1);
    private int _rowExtent = 1, _colExtent = 1, _maxRow, _maxCol;
    private double _scrollTop, _scrollLeft, _viewWidth = 1200, _viewHeight = 800;
    private int _firstRow, _lastRow, _firstCol, _lastCol;
    private List<CellRange> _windowMerges = [];
    private SpreadsheetSelection _selection = SpreadsheetSelection.Single(default);
    private SpreadsheetSelection _parameterSelection;
    private double _zoom = double.NaN;
    private bool _headings = true;
    private long _stamp;
    private int _layoutVersion;
    private bool _suppressRender;
    private bool _raiseSelection;
    private bool _scrollRequest;
    private double[] _reveal;

    /// <summary>The workbook to show and edit.</summary>
    [Parameter] public Workbook Workbook { get; set; }

    /// <summary>Index of the shown sheet in <see cref="Office.Spreadsheet.Workbook.Sheets"/>.</summary>
    [Parameter] public int SheetIndex { get; set; }

    /// <summary>Raised when the grid wants to show another sheet, for example after jumping to a defined name on another sheet.</summary>
    [Parameter] public EventCallback<int> SheetIndexChanged { get; set; }

    /// <summary>Whether editing is disabled. Selection, navigation and copying stay available.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Zoom factor, 1 is 100%.</summary>
    [Parameter] public double Zoom { get; set; } = 1;

    /// <summary>Whether gridlines are drawn.</summary>
    [Parameter] public bool ShowGridLines { get; set; } = true;

    /// <summary>Whether row and column headings are shown.</summary>
    [Parameter] public bool ShowHeadings { get; set; } = true;

    /// <summary>The selection. Changes from the host are applied; changes by the user raise <see cref="SelectionChanged"/>.</summary>
    [Parameter] public SpreadsheetSelection Selection { get; set; }

    /// <summary>Raised when the user changes the selection.</summary>
    [Parameter] public EventCallback<SpreadsheetSelection> SelectionChanged { get; set; }

    /// <summary>
    /// The calculation engine attached to <see cref="Workbook"/>. When null, the grid attaches and owns its own engine.
    /// Pass an engine when the host needs it too, because two engines on one workbook calculate twice.
    /// </summary>
    [Parameter] public CalcEngine Engine { get; set; }

    /// <summary>Raised on Ctrl+1 so the host can open its format cells dialog.</summary>
    [Parameter] public EventCallback FormatCellsRequested { get; set; }

    /// <summary>Raised on Ctrl+S so the host can save.</summary>
    [Parameter] public EventCallback SaveKeyPressed { get; set; }

    /// <summary>Raised when a user action could not be applied to the workbook. Without a handler the error is shown in a dialog.</summary>
    [Parameter] public EventCallback<Exception> OnError { get; set; }

    /// <summary>The shown worksheet, or null without a workbook.</summary>
    public Worksheet ActiveSheet => _sheet;

    /// <summary>True while a cell is being edited.</summary>
    public bool IsEditing => _editing;

    /// <summary>The text of the cell being edited.</summary>
    public string EditText => _editing ? _editText : null;

    /// <summary>Width of the visible grid area in pixels, as last reported by the browser.</summary>
    public double ViewportWidth => _viewWidth;

    /// <summary>Height of the visible grid area in pixels, as last reported by the browser.</summary>
    public double ViewportHeight => _viewHeight;

    internal event Action EditChanged;

    internal bool CanEdit => !ReadOnly && _sheet != null;

    private int FrozenRows => Math.Min(_sheet?.FreezeAt?.Row ?? 0, _rowExtent - 1);

    private int FrozenColumns => Math.Min(_sheet?.FreezeAt?.Column ?? 0, _colExtent - 1);

    internal double HeaderWidth => _headings ? Math.Round((Math.Max(3, _rowExtent.ToString(CultureInfo.InvariantCulture).Length) * 7 + 12) * _zoom) : 0;

    internal double HeaderHeight => _headings ? Math.Round(HeadingHeight * _zoom) : 0;

    internal double X(int col) => HeaderWidth + Math.Round(_cols.Offset(col) * _zoom);

    internal double Y(int row) => HeaderHeight + Math.Round(_rows.Offset(row) * _zoom);

    private double W(int col) => X(col + 1) - X(col);

    private double H(int row) => Y(row + 1) - Y(row);

    internal static string Px(double value) => value.ToString("0.##", CultureInfo.InvariantCulture) + "px";

    private static CultureInfo Culture => CultureInfo.CurrentCulture;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!ReferenceEquals(Workbook, _workbook) || !ReferenceEquals(Engine, _engineParameter)) Attach();
        var layout = ResolveSheet();
        var zoom = Zoom > 0 ? Zoom : 1;
        if (zoom != _zoom || ShowHeadings != _headings)
        {
            _zoom = zoom;
            _headings = ShowHeadings;
            ClearLooks();
            layout = true;
        }

        if (layout) Relayout();
        if (!Equals(Selection, _parameterSelection))
        {
            _parameterSelection = Selection;
            if (Selection is { Ranges.Count: > 0 } && _sheet != null && !Selection.Equals(_selection))
            {
                _selection = Selection;
                Reveal(_selection.Active);
            }
        }

        Selection = _selection;
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (!_raiseSelection) return;
        _raiseSelection = false;
        await SelectionChanged.InvokeAsync(_selection);
    }

    /// <inheritdoc />
    protected override bool ShouldRender()
    {
        if (!_suppressRender) return base.ShouldRender();
        _suppressRender = false;
        return false;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (_scrollRequest)
        {
            _scrollRequest = false;
            await Js("scrollTo", _scrollTop, _scrollLeft);
        }

        if (_reveal is { } reveal)
        {
            _reveal = null;
            await Js("ensureVisible", reveal.Cast<object>().ToArray());
        }
    }

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        Detach();
        return base.DisposeAsync();
    }

    /// <summary>Moves the keyboard focus to the grid.</summary>
    /// <returns>A task that completes when the focus moved.</returns>
    public Task FocusAsync() => Js("focus");

    /// <summary>Called by the browser when the visible area left the rendered window or changed size.</summary>
    /// <param name="scrollTop">Vertical scroll offset in pixels.</param>
    /// <param name="scrollLeft">Horizontal scroll offset in pixels.</param>
    /// <param name="width">Visible width in pixels.</param>
    /// <param name="height">Visible height in pixels.</param>
    /// <returns>A task that completes when the new window is rendered.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnViewportChanged(double scrollTop, double scrollLeft, double width, double height)
    {
        if (_sheet == null) return Task.CompletedTask;
        var before = (_firstRow, _lastRow, _firstCol, _lastCol, _rowExtent, _colExtent, _viewWidth, _viewHeight);
        _scrollTop = Math.Max(0, scrollTop);
        _scrollLeft = Math.Max(0, scrollLeft);
        if (width > 0) _viewWidth = width;
        if (height > 0) _viewHeight = height;
        UpdateWindow();
        return before == (_firstRow, _lastRow, _firstCol, _lastCol, _rowExtent, _colExtent, _viewWidth, _viewHeight)
            ? Task.CompletedTask
            : InvokeAsync(StateHasChanged);
    }

    private void Attach()
    {
        Detach();
        _workbook = Workbook;
        _engineParameter = Engine;
        _sheet = null;
        _sheetSelections.Clear();
        ClearLooks();
        _clipboard = null;
        if (_workbook == null) return;
        _workbook.Changed += OnWorkbookChanged;
        _engine = Engine ?? (_ownedEngine = new CalcEngine(_workbook));
        _engine.Calculated += OnCalculated;
    }

    private void Detach()
    {
        if (_workbook != null) _workbook.Changed -= OnWorkbookChanged;
        if (_engine != null) _engine.Calculated -= OnCalculated;
        _ownedEngine?.Dispose();
        _ownedEngine = null;
        DisposeConditional();
        _engine = null;
        _workbook = null;
        _editing = false;
    }

    private bool ResolveSheet()
    {
        var sheets = _workbook?.Sheets;
        var sheet = sheets is { Count: > 0 } ? sheets[Math.Clamp(SheetIndex, 0, sheets.Count - 1)] : null;
        if (ReferenceEquals(sheet, _sheet)) return false;
        if (_sheet != null) _sheetSelections[_sheet] = _selection;
        _editing = false;
        _filterOpen = null;
        _listOpen = false;
        _noteHover = _noteEdit = null;
        _selectedChart = null;
        _sheet = sheet;
        var previous = _selection;
        _selection = sheet != null && _sheetSelections.TryGetValue(sheet, out var s) ? s : SpreadsheetSelection.Single(default);
        _raiseSelection |= !_selection.Equals(previous);
        _scrollTop = _scrollLeft = 0;
        _maxRow = _maxCol = 0;
        _scrollRequest = true;
        return true;
    }

    private void Relayout()
    {
        if (_sheet == null) return;
        var used = _sheet.UsedRange;
        var pageRows = (int)(_viewHeight / Math.Max(1, _sheet.DefaultRowHeight * _zoom)) + 1;
        var pageCols = (int)(_viewWidth / Math.Max(1, _sheet.DefaultColumnWidth * _zoom)) + 1;
        var freeze = _sheet.FreezeAt ?? default;
        _rowExtent = (int)Math.Min(CellRef.MaxRows, Math.Max(100L, Math.Max(Math.Max(used.Bottom, _maxRow), Math.Max(freeze.Row, _selection.Active.Row)) + 1L + 2L * pageRows));
        _colExtent = (int)Math.Min(CellRef.MaxColumns, Math.Max(30L, Math.Max(Math.Max(used.Right, _maxCol), Math.Max(freeze.Column, _selection.Active.Column)) + 1L + 2L * pageCols));
        _rows = new SpreadsheetAxis(_sheet.DefaultRowHeight, _sheet.RowHeights, _sheet.HiddenRows, _rowExtent);
        _cols = new SpreadsheetAxis(_sheet.DefaultColumnWidth, _sheet.ColumnWidths, _sheet.HiddenColumns, _colExtent);
        _layoutVersion++;
        UpdateWindow();
    }

    private void UpdateWindow()
    {
        if (_sheet == null) return;
        var fr = FrozenRows;
        var fc = FrozenColumns;
        var firstRow = Math.Max(fr, RowAt(_scrollTop + Y(fr)));
        var lastRow = Math.Max(firstRow, RowAt(_scrollTop + _viewHeight));
        var firstCol = Math.Max(fc, ColumnAt(_scrollLeft + X(fc)));
        var lastCol = Math.Max(firstCol, ColumnAt(_scrollLeft + _viewWidth));
        if ((lastRow + RowBuffer >= _rowExtent && _rowExtent < CellRef.MaxRows) || (lastCol + ColumnBuffer >= _colExtent && _colExtent < CellRef.MaxColumns))
        {
            _maxRow = Math.Max(_maxRow, Math.Min(CellRef.MaxRows - 1, lastRow + RowBuffer));
            _maxCol = Math.Max(_maxCol, Math.Min(CellRef.MaxColumns - 1, lastCol + ColumnBuffer));
            Relayout();
            return;
        }

        _firstRow = Math.Max(fr, firstRow - RowBuffer);
        _lastRow = Math.Min(_rowExtent - 1, lastRow + RowBuffer);
        _firstCol = Math.Max(fc, firstCol - ColumnBuffer);
        _lastCol = Math.Min(_colExtent - 1, lastCol + ColumnBuffer);
        var window = CellRange.FromBounds(fr > 0 ? 0 : _firstRow, fc > 0 ? 0 : _firstCol, _lastRow, _lastCol);
        _windowMerges = _sheet.Merges.Where(m => m.Intersects(window)).ToList();
    }

    private int RowAt(double canvasY) => _rows.IndexAt(Math.Max(0, (canvasY - HeaderHeight) / _zoom));

    private int ColumnAt(double canvasX) => _cols.IndexAt(Math.Max(0, (canvasX - HeaderWidth) / _zoom));

    private void Reveal(CellRef cell)
    {
        if (_sheet == null) return;
        if (cell.Row >= _rowExtent - RowBuffer || cell.Column >= _colExtent - ColumnBuffer)
        {
            _maxRow = Math.Max(_maxRow, cell.Row);
            _maxCol = Math.Max(_maxCol, cell.Column);
            Relayout();
        }

        var stickyTop = Y(FrozenRows);
        var stickyLeft = X(FrozenColumns);
        var range = MergeAt(cell) ?? new CellRange(cell, cell);
        double x = -1, y = -1;
        if (range.Top >= FrozenRows)
        {
            y = Y(range.Top);
            var bottom = Y(range.Bottom + 1);
            if (y - _scrollTop < stickyTop) _scrollTop = Math.Max(0, y - stickyTop);
            else if (bottom - _scrollTop > _viewHeight) _scrollTop = Math.Min(y - stickyTop, bottom - _viewHeight);
        }

        if (range.Left >= FrozenColumns)
        {
            x = X(range.Left);
            var right = X(range.Right + 1);
            if (x - _scrollLeft < stickyLeft) _scrollLeft = Math.Max(0, x - stickyLeft);
            else if (right - _scrollLeft > _viewWidth) _scrollLeft = Math.Min(x - stickyLeft, right - _viewWidth);
        }

        UpdateWindow();
        _reveal = [x, y, X(range.Right + 1) - X(range.Left), Y(range.Bottom + 1) - Y(range.Top), stickyLeft, stickyTop];
    }

    private void OnWorkbookChanged(object sender, WorkbookChangedEventArgs e)
    {
        if (_sheet == null) return;
        InvalidateCharts(e);
        var layout = false;
        foreach (var s in e.Structure)
            layout |= s.Sheet == null || s.Sheet == _sheet || s.Kind is StructureChangeKind.SheetRemoved or StructureChangeKind.SheetAdded or StructureChangeKind.SheetMoved;
        foreach (var f in e.FormattedRanges)
        {
            if (f.Sheet != _sheet) continue;
            if (f.Range.RowCount > 1000) layout = true;
            else
                for (var r = f.Range.Top; r <= f.Range.Bottom; r++) Bump(r);
        }

        var bottom = 0;
        var right = 0;
        foreach (var c in e.SingleCells)
        {
            if (c.Sheet != _sheet) continue;
            Bump(c.Cell.Row);
            bottom = Math.Max(bottom, c.Cell.Row);
            right = Math.Max(right, c.Cell.Column);
        }

        foreach (var (sheet, range) in e.ValueRanges)
        {
            if (sheet != _sheet) continue;
            if (range.RowCount > 1000) layout = true;
            else
                for (var r = range.Top; r <= range.Bottom; r++) Bump(r);
            bottom = Math.Max(bottom, range.Bottom);
            right = Math.Max(right, range.Right);
        }

        if (bottom >= _rowExtent - RowBuffer || right >= _colExtent - ColumnBuffer)
        {
            _maxRow = Math.Max(_maxRow, bottom);
            _maxCol = Math.Max(_maxCol, right);
            layout = true;
        }

        if (layout)
        {
            ResolveSheet();
            Relayout();
        }
        else if (HasRules && (e.SingleCells.Any(c => c.Sheet == _sheet) || e.ValueRanges.Any(v => v.Sheet == _sheet) || e.FormattedRanges.Any(f => f.Sheet == _sheet)))
            _layoutVersion++;

        RefreshFromModel();
    }

    private void OnCalculated(object sender, CalculatedEventArgs e)
    {
        if (_sheet == null) return;
        InvalidateCharts(e);
        if (e.Cells.Count > 5000 || (HasRules && e.Cells.Any(c => c.Sheet == _sheet))) _layoutVersion++;
        else
            foreach (var c in e.Cells)
                if (c.Sheet == _sheet) Bump(c.Cell.Row);
        RefreshFromModel();
    }

    private void RefreshFromModel()
    {
        NotifyEdit();
        _ = InvokeAsync(StateHasChanged);
    }

    private void Bump(int row) => _rowStamps[row] = ++_stamp;

    private long RowVersion(int row) => ((long)_layoutVersion << 32) | (uint)_rowStamps.GetValueOrDefault(row);

    private async Task Js(string method, params object[] args)
    {
        if (JsReference is not { } js) return;
        try
        {
            await js.InvokeVoidAsync(method, args);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (TaskCanceledException)
        {
        }
    }

    internal CellRange? MergeAt(CellRef cell)
    {
        if (_sheet == null) return null;
        foreach (var m in _sheet.Merges)
            if (m.Contains(cell)) return m.Normalized;
        return null;
    }

    private bool InWindowMerge(CellRef cell)
    {
        foreach (var m in _windowMerges)
            if (m.Contains(cell)) return true;
        return false;
    }

    private IEnumerable<GridPane> Panes()
    {
        var fr = FrozenRows;
        var fc = FrozenColumns;
        yield return new GridPane(0, _firstRow, _lastRow, _firstCol, _lastCol);
        yield return new GridPane(1, _firstRow, _lastRow, 0, fc - 1);
        yield return new GridPane(2, 0, fr - 1, _firstCol, _lastCol);
        yield return new GridPane(3, 0, fr - 1, 0, fc - 1);
    }

    private int PaneOf(CellRef cell) => (cell.Row < FrozenRows ? 2 : 0) + (cell.Column < FrozenColumns ? 1 : 0);

    private string RootStyle => $"--mud-ex-ssg-zoom:{_zoom.ToString(CultureInfo.InvariantCulture)};{Style}";

    private string CanvasStyle => $"width:{Px(X(_colExtent))};height:{Px(Y(_rowExtent))}";

    private string WindowBounds
    {
        get
        {
            var fr = FrozenRows;
            var fc = FrozenColumns;
            var minTop = _firstRow > fr ? Y(_firstRow) - Y(fr) : -1;
            var maxTop = _lastRow < _rowExtent - 1 ? Y(_lastRow + 1) - _viewHeight : 1e12;
            var minLeft = _firstCol > fc ? X(_firstCol) - X(fc) : -1;
            var maxLeft = _lastCol < _colExtent - 1 ? X(_lastCol + 1) - _viewWidth : 1e12;
            return string.Join(",", new[] { minTop, maxTop, minLeft, maxLeft, _viewWidth, _viewHeight }.Select(v => v.ToString("0.##", CultureInfo.InvariantCulture)));
        }
    }

    private string StickyBounds => $"{X(FrozenColumns).ToString(CultureInfo.InvariantCulture)},{Y(FrozenRows).ToString(CultureInfo.InvariantCulture)}";

    private string PaneBackground(GridPane p) => p.Kind switch
    {
        1 => $"width:{Px(X(FrozenColumns))};height:{Px(Y(_rowExtent))}",
        2 => $"width:{Px(X(_colExtent))};height:{Px(Y(FrozenRows))}",
        _ => $"width:{Px(X(FrozenColumns))};height:{Px(Y(FrozenRows))}"
    };

    private string HorizontalLine(GridPane p, int row) =>
        $"top:{Px(Y(row + 1) - 1)};left:{Px(X(p.C0))};width:{Px(X(p.C1 + 1) - X(p.C0))}";

    private string VerticalLine(GridPane p, int col) =>
        $"left:{Px(X(col + 1) - 1)};top:{Px(Y(p.R0))};height:{Px(Y(p.R1 + 1) - Y(p.R0))}";

    private string ColumnHeaderStyle(int col) => $"left:{Px(X(col))};width:{Px(W(col))};height:{Px(HeaderHeight)}";

    private string RowHeaderStyle(int row) => $"top:{Px(Y(row))};height:{Px(H(row))};width:{Px(HeaderWidth)}";

    private string ColumnHeaderClass(int col)
    {
        var cls = "mud-ex-ssg-ch";
        foreach (var r in _selection.Ranges)
            if (col >= r.Left && col <= r.Right) cls = r.IsWholeColumns ? "mud-ex-ssg-ch mud-ex-ssg-full" : "mud-ex-ssg-ch mud-ex-ssg-sel";
        return cls;
    }

    private string RowHeaderClass(int row)
    {
        var cls = "mud-ex-ssg-rh";
        foreach (var r in _selection.Ranges)
            if (row >= r.Top && row <= r.Bottom) cls = r.IsWholeRows ? "mud-ex-ssg-rh mud-ex-ssg-full" : "mud-ex-ssg-rh mud-ex-ssg-sel";
        return cls;
    }

    private IEnumerable<CellRange> MergesIn(GridPane p) =>
        p.R1 < p.R0 || p.C1 < p.C0 ? [] : _windowMerges.Where(m => m.Intersects(CellRange.FromBounds(p.R0, p.C0, p.R1, p.C1)));

    private IEnumerable<GridOverlay> Overlays(GridPane p)
    {
        if (p.R1 < p.R0 || p.C1 < p.C0) yield break;
        var multi = !_selection.IsSingleCell && !(_selection.Ranges.Count == 1 && MergeAt(_selection.Active) == _selection.Range.Normalized);
        if (multi)
            foreach (var range in _selection.Ranges)
                if (Clip(range.Normalized, p, "mud-ex-ssg-range") is { } o)
                    yield return o;
        var active = MergeAt(_selection.Active) ?? new CellRange(_selection.Active, _selection.Active);
        if (Clip(active, p, "mud-ex-ssg-active") is { } a) yield return a;
        if (_clipboard is { } clip && clip.Sheet == _sheet && Clip(clip.Range, p, "mud-ex-ssg-ants") is { } ants) yield return ants;
        var last = _selection.Range.Normalized;
        if (CanEdit && _selection.Ranges.Count == 1 && !last.IsWholeColumns && !last.IsWholeRows
            && last.Bottom >= p.R0 && last.Bottom <= p.R1 && last.Right >= p.C0 && last.Right <= p.C1)
            yield return new GridOverlay("mud-ex-ssg-fill", $"left:{Px(X(last.Right + 1) - 4)};top:{Px(Y(last.Bottom + 1) - 4)}",
                $"{last.Top},{last.Left},{last.Bottom},{last.Right}");
    }

    private GridOverlay Clip(CellRange r, GridPane p, string cls)
    {
        int top = Math.Max(r.Top, p.R0), bottom = Math.Min(r.Bottom, p.R1), left = Math.Max(r.Left, p.C0), right = Math.Min(r.Right, p.C1);
        if (top > bottom || left > right) return null;
        var sides = (r.Top < p.R0 ? " nt" : "") + (r.Bottom > p.R1 ? " nb" : "") + (r.Left < p.C0 ? " nl" : "") + (r.Right > p.C1 ? " nr" : "");
        return new GridOverlay(cls + sides,
            $"left:{Px(X(left) - 1)};top:{Px(Y(top) - 1)};width:{Px(X(right + 1) - X(left) + 1)};height:{Px(Y(bottom + 1) - Y(top) + 1)}", null);
    }

    private string EditorStyle
    {
        get
        {
            var r = MergeAt(_selection.Active) ?? new CellRange(_selection.Active, _selection.Active);
            var look = Look(_sheet.EffectiveStyleId(r.Start));
            return $"left:{Px(X(r.Left) - 1)};top:{Px(Y(r.Top) - 1)};min-width:{Px(X(r.Right + 1) - X(r.Left) + 1)};min-height:{Px(Y(r.Bottom + 1) - Y(r.Top) + 1)};{look.Text}";
        }
    }

    private string AssistHostStyle
    {
        get
        {
            var r = MergeAt(_editCell) ?? new CellRange(_editCell, _editCell);
            return $"left:{Px(X(r.Left) - 1)};top:{Px(Y(r.Bottom + 1) + 1)}";
        }
    }

    private void BuildRowComponent(RenderTreeBuilder b, int row, GridPane p)
    {
        b.OpenComponent<SpreadsheetGridRow>(0);
        b.SetKey(row);
        b.AddComponentParameter(1, nameof(SpreadsheetGridRow.Row), row);
        b.AddComponentParameter(2, nameof(SpreadsheetGridRow.FirstColumn), p.C0);
        b.AddComponentParameter(3, nameof(SpreadsheetGridRow.LastColumn), p.C1);
        b.AddComponentParameter(4, nameof(SpreadsheetGridRow.Version), RowVersion(row));
        b.CloseComponent();
    }

    internal void BuildRow(RenderTreeBuilder b, int row, int c0, int c1)
    {
        if (_sheet == null) return;
        var y = Y(row);
        var h = Y(row + 1) - y;
        if (h <= 0) return;
        for (var c = c0; c <= c1; c++)
        {
            var at = new CellRef(row, c);
            var x = X(c);
            var w = X(c + 1) - x;
            if (w <= 0 || InWindowMerge(at)) continue;
            var cell = _sheet[at];
            var content = cell?.HasContent == true;
            var look = LookAt(at, _sheet.EffectiveStyleId(at), out var cf);
            if (!content && look.Box.Length == 0 && cf == null && NoteAt(at) == null) continue;
            double extendLeft = 0, extendRight = 0;
            if (content && cell.Value.Kind == CellValueKind.Text && !look.Wrap)
            {
                if (look.Horizontal is CellHorizontalAlignment.General or CellHorizontalAlignment.Left)
                    for (var n = c + 1; n <= c1 && IsFree(row, n); n++) extendRight += W(n);
                else if (look.Horizontal == CellHorizontalAlignment.Right)
                    for (var n = c - 1; n >= c0 && IsFree(row, n); n--) extendLeft += W(n);
            }

            BuildCell(b, c, at, x, y, w, h, extendLeft, extendRight, false, look, cf);
        }
    }

    private bool IsFree(int row, int col)
    {
        var at = new CellRef(row, col);
        return _sheet[at]?.HasContent != true && !InWindowMerge(at);
    }

    private void BuildMerge(RenderTreeBuilder b, CellRange m)
    {
        var look = LookAt(m.Start, _sheet.EffectiveStyleId(m.Start), out var cf);
        BuildCell(b, m.ToA1(), m.Start, X(m.Left), Y(m.Top), X(m.Right + 1) - X(m.Left), Y(m.Bottom + 1) - Y(m.Top), 0, 0, true, look, cf);
    }

    private void BuildCell(RenderTreeBuilder b, object key, CellRef at, double x, double y, double w, double h, double extendLeft, double extendRight, bool merged,
        CellLook look, ConditionalResult cf)
    {
        var cell = _sheet[at];
        string text = null, color = null;
        var horizontal = look.Horizontal;
        if (cell?.HasContent == true && cf?.HideValue != true)
        {
            text = NumberFormat.Format(cell.Value, look.Format, Culture, out var named);
            color = NamedColor(named);
            if (horizontal == CellHorizontalAlignment.General)
                horizontal = cell.Value.Kind switch
                {
                    CellValueKind.Number => CellHorizontalAlignment.Right,
                    CellValueKind.Boolean or CellValueKind.Error => CellHorizontalAlignment.Center,
                    _ => CellHorizontalAlignment.Left
                };
        }

        var cls = new StringBuilder("mud-ex-ssg-cell");
        if (merged) cls.Append(" mud-ex-ssg-merged");
        if (horizontal == CellHorizontalAlignment.Right) cls.Append(" mud-ex-ssg-ha-right");
        else if (horizontal == CellHorizontalAlignment.Center) cls.Append(" mud-ex-ssg-ha-center");
        if (look.Vertical == CellVerticalAlignment.Top) cls.Append(" mud-ex-ssg-va-top");
        else if (look.Vertical == CellVerticalAlignment.Center) cls.Append(" mud-ex-ssg-va-center");
        if (look.Wrap) cls.Append(" mud-ex-ssg-wrap");
        var overflow = extendLeft > 0 || extendRight > 0;
        if (overflow) cls.Append(" mud-ex-ssg-ovf");
        var link = text != null ? LinkFor(at) : null;
        if (link?.IsSafe == true) cls.Append(" mud-ex-ssg-link");
        var note = NoteAt(at);
        if (note != null) cls.Append(" mud-ex-ssg-noted");

        b.OpenElement(0, "div");
        b.SetKey(key);
        b.AddAttribute(1, "class", cls.ToString());
        b.AddAttribute(2, "data-r", at.Row);
        b.AddAttribute(3, "data-c", at.Column);
        b.AddAttribute(4, "style", $"left:{Px(x - 1)};top:{Px(y - 1)};width:{Px(w + 1)};height:{Px(h + 1)};{look.Box}{ConditionalBox(cf)}{look.Text}{(color != null ? "color:" + color + ";" : "")}");
        if (link?.IsSafe == true) b.AddAttribute(10, "title", link.Tooltip ?? TryLocalize("Ctrl+click to follow the link."));
        if (note != null)
        {
            b.OpenElement(11, "span");
            b.AddAttribute(12, "class", "mud-ex-ssg-note-mark");
            b.CloseElement();
        }

        if (cf?.Icon is { } icon)
        {
            b.OpenRegion(9);
            BuildIcon(b, icon);
            b.CloseRegion();
        }

        if (overflow)
        {
            b.OpenElement(5, "span");
            b.AddAttribute(6, "style", extendRight > 0 ? $"left:0;width:{Px(w + 1 + extendRight)}" : $"right:0;width:{Px(w + 1 + extendLeft)}");
            b.AddContent(7, text);
            b.CloseElement();
        }
        else
        {
            b.AddContent(8, text);
        }

        b.CloseElement();
    }

    private readonly record struct GridPane(int Kind, int R0, int R1, int C0, int C1)
    {
        public bool LeftSticky => Kind is 1 or 3;

        public bool TopSticky => Kind is 2 or 3;
    }

    private sealed record GridOverlay(string Class, string Style, string Range);
}
