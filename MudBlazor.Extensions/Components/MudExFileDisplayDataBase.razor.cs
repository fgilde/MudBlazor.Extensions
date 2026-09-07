using Microsoft.AspNetCore.Components;
using Microsoft.Data.Sqlite;
using MudBlazor;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Services;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A viewer for embedded database files (SQLite/.db/.db3/.sqlite/.sqlite3/...).
/// </summary>
public partial class MudExFileDisplayDataBase : IMudExFileDisplay
{
    private static readonly string[] SupportedExtensions = { ".sqlite", ".sqlite3", ".db", ".db3", ".s3db", ".sl3", ".db2" };

    [Inject] private MudExFileService FileService { get; set; }

    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplayDataBase);

    /// <inheritdoc />
    public bool StartsActive => true;

    /// <inheritdoc />
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Reference to the parent MudExFileDisplay if the component is used inside a MudExFileDisplay
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    private MudExFileDisplayDataBaseTable[] _tables = Array.Empty<MudExFileDisplayDataBaseTable>();
    private MudExFileDisplayDataBaseTable _selectedTable;
    private List<MudExFileDisplayDataBaseColumn> _columns = new();
    private string _errorMessage;
    private string _tableMessage;
    private string _searchString;
    private bool _loading;
    private bool _showQuery;
    private string _customQuery = "SELECT * FROM ";
    private string _customQueryError;
    private List<string> _customQueryColumns;
    private List<MudExFileDisplayDataBaseRow> _customQueryResult;
    private MudDataGrid<MudExFileDisplayDataBaseRow> _grid;
    private CancellationTokenSource _cts = new();
    private string _sortColumn;
    private bool _sortDescending;

    // Native (real SQLite engine) mode - preferred, gives full SQL power. Requires a native sqlite3 binary,
    // which is generally only available outside of Blazor WebAssembly.
    private bool _nativeMode;
    private SqliteConnection _connection;
    private string _tempFilePath;

    // Pure managed fallback mode - works everywhere (incl. WASM) but has no SQL engine, so we cache and
    // filter/sort/page the decoded rows of the selected table ourselves, in memory.
    private MudExSqlitePureReader _pureReader;
    private List<Dictionary<string, object>> _pureRowsCache;
    private string _pureRowsCacheTable;

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
    {
        var ext = Path.GetExtension(fileDisplayInfos?.FileName ?? string.Empty);
        var isExtensionMatch = !string.IsNullOrEmpty(ext) && SupportedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
        var isContentTypeMatch = MimeType.Matches(fileDisplayInfos?.ContentType, "application/vnd.sqlite3", "application/x-sqlite3", "application/x-sqlite", "application/db", "application/octet-stream+sqlite");
        return Task.FromResult(isExtensionMatch || isContentTypeMatch);
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            { "Engine", _nativeMode ? "Native SQLite" : "Built-in client-side reader" },
            { "Tables", _tables.Count(t => !t.IsView) },
            { "Views", _tables.Count(t => t.IsView) },
            { "Selected table", _selectedTable?.Name },
            { "Rows in selected table", _selectedTable?.RowCount ?? 0 }
        });
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var infos);
        var hasSource = infos != null && (!string.IsNullOrEmpty(infos.Url) || infos.ContentStream is { Length: > 0 });
        var notYetLoaded = _connection == null && _pureReader == null;

        await base.SetParametersAsync(parameters);

        if (hasSource && notYetLoaded)
            await LoadDatabaseAsync();
    }

    private async Task LoadDatabaseAsync()
    {
        _loading = true;
        _errorMessage = null;
        StateHasChanged();

        Cleanup();

        try
        {
            if (FileDisplayInfos == null)
                throw new ArgumentException("No file information available");

            var ct = _cts?.Token ?? CancellationToken.None;
            await using var stream = await FileService.ReadStreamAsync(FileDisplayInfos, ct);
            var bytes = stream.ToByteArray();

            if (!MudExSqlitePureReader.LooksLikeSqliteFile(bytes))
                throw new InvalidDataException(TryLocalize("This does not look like a valid SQLite database file."));

            // Prefer a real, native SQLite engine when available (full SQL power). This generally only works
            // outside of Blazor WebAssembly since it needs a native sqlite3 binary.
            _nativeMode = !MudExResource.IsClientSide && TryOpenNative(bytes);

            if (_nativeMode)
                await LoadTablesNativeAsync(ct);
            else
                LoadTablesPure(bytes);
        }
        catch (Exception e)
        {
            _errorMessage = e.Message;
            MudExFileDisplay?.ShowError(e.Message);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    #region Native (Microsoft.Data.Sqlite) mode

    private bool TryOpenNative(byte[] bytes)
    {
        try
        {
            _tempFilePath = Path.Combine(Path.GetTempPath(), $"mudex-db-{Guid.NewGuid():N}.sqlite3");
            File.WriteAllBytes(_tempFilePath, bytes);

            var csb = new SqliteConnectionStringBuilder { DataSource = _tempFilePath, Mode = SqliteOpenMode.ReadOnly };
            var connection = new SqliteConnection(csb.ToString());
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT count(*) FROM sqlite_master"; // sanity check that the native engine can actually read this file
                cmd.ExecuteScalar();
            }

            _connection = connection;
            return true;
        }
        catch
        {
            try { _connection?.Dispose(); } catch { /* ignore */ }
            _connection = null;
            if (!string.IsNullOrEmpty(_tempFilePath))
            {
                try { if (File.Exists(_tempFilePath)) File.Delete(_tempFilePath); } catch { /* ignore */ }
                _tempFilePath = null;
            }
            return false;
        }
    }

    private async Task LoadTablesNativeAsync(CancellationToken ct)
    {
        var tables = new List<MudExFileDisplayDataBaseTable>();

        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "SELECT name, type FROM sqlite_master WHERE type IN ('table','view') AND name NOT LIKE 'sqlite_%' ORDER BY type, name";
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                tables.Add(new MudExFileDisplayDataBaseTable(reader.GetString(0), reader.GetString(1) == "view"));
        }

        foreach (var table in tables)
        {
            try { table.RowCount = await GetRowCountNativeAsync(table.Name, ct); }
            catch { table.RowCount = 0; }
        }

        _tables = tables.ToArray();
        await SelectTableAsync(_tables.FirstOrDefault());
    }

    private async Task<long> GetRowCountNativeAsync(string tableName, CancellationToken ct)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {QuoteIdentifier(tableName)}";
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result);
    }

    private async Task<List<MudExFileDisplayDataBaseColumn>> LoadColumnsNativeAsync(string tableName)
    {
        var columns = new List<MudExFileDisplayDataBaseColumn>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({QuoteIdentifier(tableName)})";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(new MudExFileDisplayDataBaseColumn(
                reader.GetString(reader.GetOrdinal("name")),
                reader.IsDBNull(reader.GetOrdinal("type")) ? string.Empty : reader.GetString(reader.GetOrdinal("type")),
                reader.GetInt64(reader.GetOrdinal("pk")) > 0));
        }
        return columns;
    }

    private async Task<GridData<MudExFileDisplayDataBaseRow>> ServerReloadNativeAsync(GridState<MudExFileDisplayDataBaseRow> state, CancellationToken ct)
    {
        var whereClause = BuildSearchWhereClause();
        var total = await GetFilteredRowCountNativeAsync(whereClause, ct);

        var sql = $"SELECT * FROM {QuoteIdentifier(_selectedTable.Name)}";
        if (!string.IsNullOrEmpty(whereClause))
            sql += $" WHERE {whereClause}";
        if (!string.IsNullOrEmpty(_sortColumn) && _columns.Any(c => c.Name == _sortColumn))
            sql += $" ORDER BY {QuoteIdentifier(_sortColumn)} {(_sortDescending ? "DESC" : "ASC")}";
        sql += " LIMIT @limit OFFSET @offset";

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        AddSearchParameter(cmd);
        cmd.Parameters.AddWithValue("@limit", state.PageSize <= 0 ? 100 : state.PageSize);
        cmd.Parameters.AddWithValue("@offset", state.Page * state.PageSize);

        var rows = new List<MudExFileDisplayDataBaseRow>();
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
                rows.Add(new MudExFileDisplayDataBaseRow(ReadCurrentRow(reader)));
        }

        return new GridData<MudExFileDisplayDataBaseRow> { TotalItems = (int)total, Items = rows };
    }

    private static Dictionary<string, object> ReadCurrentRow(SqliteDataReader reader)
    {
        var dict = new Dictionary<string, object>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < reader.FieldCount; i++)
            dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        return dict;
    }

    private async Task<long> GetFilteredRowCountNativeAsync(string whereClause, CancellationToken ct)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {QuoteIdentifier(_selectedTable.Name)}" + (string.IsNullOrEmpty(whereClause) ? "" : $" WHERE {whereClause}");
        AddSearchParameter(cmd);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result);
    }

    private string BuildSearchWhereClause()
    {
        if (string.IsNullOrWhiteSpace(_searchString) || _columns.Count == 0)
            return string.Empty;
        return string.Join(" OR ", _columns.Select(c => $"CAST({QuoteIdentifier(c.Name)} AS TEXT) LIKE @search"));
    }

    private void AddSearchParameter(SqliteCommand cmd)
    {
        if (!string.IsNullOrWhiteSpace(_searchString))
            cmd.Parameters.AddWithValue("@search", $"%{_searchString}%");
    }

    private async Task RunCustomQueryAsync()
    {
        _customQueryError = null;
        _customQueryResult = null;
        _customQueryColumns = null;

        if (!_nativeMode)
        {
            _customQueryError = TryLocalize("Running raw SQL queries requires a native SQLite engine, which is not available in this hosting environment (e.g. WebAssembly). Browsing, searching and sorting tables still works fully client-side though.");
            StateHasChanged();
            return;
        }

        if (_connection == null || string.IsNullOrWhiteSpace(_customQuery))
            return;

        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = _customQuery;
            await using var reader = await cmd.ExecuteReaderAsync();
            var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
            var rows = new List<MudExFileDisplayDataBaseRow>();
            while (await reader.ReadAsync())
                rows.Add(new MudExFileDisplayDataBaseRow(ReadCurrentRow(reader)));
            _customQueryColumns = columns;
            _customQueryResult = rows;
        }
        catch (Exception e)
        {
            _customQueryError = e.Message;
        }

        StateHasChanged();
    }

    #endregion

    #region Pure (client-side, dependency-free) mode

    private void LoadTablesPure(byte[] bytes)
    {
        _pureReader = new MudExSqlitePureReader(bytes);
        var tables = _pureReader.Tables
            .Select(t => new MudExFileDisplayDataBaseTable(t.Name, t.IsView) { RowCount = t.IsView ? 0 : _pureReader.CountRows(t.Name) })
            .OrderBy(t => t.IsView).ThenBy(t => t.Name)
            .ToArray();

        _tables = tables;
        _ = SelectTableAsync(_tables.FirstOrDefault());
    }

    private void EnsurePureRowsCached()
    {
        if (_selectedTable == null || _pureReader == null)
            return;
        if (_pureRowsCacheTable == _selectedTable.Name && _pureRowsCache != null)
            return;

        _pureRowsCacheTable = _selectedTable.Name;
        _tableMessage = null;

        if (!_pureReader.CanBrowse(_selectedTable.Name))
        {
            _pureRowsCache = new List<Dictionary<string, object>>();
            _tableMessage = _selectedTable.IsView
                ? TryLocalize("Views are not supported by the built-in client-side reader (no native SQL engine available in this hosting environment).")
                : TryLocalize("This table uses WITHOUT ROWID and is not supported by the built-in client-side reader.");
            return;
        }

        try
        {
            _pureRowsCache = _pureReader.GetRows(_selectedTable.Name, out var truncated);
            if (truncated)
                _tableMessage = TryLocalize("This table is very large - only the first {0} rows are shown in client-side mode.", MudExSqlitePureReader.MaxRowsPerTable);
        }
        catch (Exception e)
        {
            _pureRowsCache = new List<Dictionary<string, object>>();
            _tableMessage = e.Message;
        }
    }

    private Task<GridData<MudExFileDisplayDataBaseRow>> ServerReloadPureAsync(GridState<MudExFileDisplayDataBaseRow> state)
    {
        EnsurePureRowsCached();
        IEnumerable<Dictionary<string, object>> query = _pureRowsCache ?? Enumerable.Empty<Dictionary<string, object>>();

        if (!string.IsNullOrWhiteSpace(_searchString))
        {
            query = query.Where(row => row.Values.Any(v => v != null && v.ToString().Contains(_searchString, StringComparison.OrdinalIgnoreCase)));
        }

        var filtered = query.ToList();

        if (!string.IsNullOrEmpty(_sortColumn))
        {
            filtered = _sortDescending
                ? filtered.OrderByDescending(row => row.TryGetValue(_sortColumn, out var v) ? v : null, PureValueComparer.Instance).ToList()
                : filtered.OrderBy(row => row.TryGetValue(_sortColumn, out var v) ? v : null, PureValueComparer.Instance).ToList();
        }

        var pageSize = state.PageSize <= 0 ? 100 : state.PageSize;
        var page = filtered.Skip(state.Page * pageSize).Take(pageSize)
            .Select(row => new MudExFileDisplayDataBaseRow(row))
            .ToList();

        return Task.FromResult(new GridData<MudExFileDisplayDataBaseRow> { TotalItems = filtered.Count, Items = page });
    }

    private sealed class PureValueComparer : IComparer<object>
    {
        public static readonly PureValueComparer Instance = new();

        public int Compare(object x, object y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            if (x is IComparable cx && x.GetType() == y.GetType()) return cx.CompareTo(y);
            return string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }

    #endregion

    #region Shared UI logic (mode agnostic)

    private async Task SelectTableAsync(MudExFileDisplayDataBaseTable table)
    {
        if (table == null)
            return;

        _selectedTable = table;
        _searchString = null;
        _sortColumn = null;
        _sortDescending = false;
        _tableMessage = null;

        _columns = _nativeMode ? await LoadColumnsNativeAsync(table.Name) : _pureReader.GetColumns(table.Name);

        if (!_nativeMode)
            EnsurePureRowsCached();

        StateHasChanged();
        if (_grid != null)
            await _grid.ReloadServerData();
    }

    private Task<GridData<MudExFileDisplayDataBaseRow>> ServerReload(GridState<MudExFileDisplayDataBaseRow> state, CancellationToken ct)
    {
        if (_selectedTable == null || _columns.Count == 0)
            return Task.FromResult(new GridData<MudExFileDisplayDataBaseRow> { TotalItems = 0, Items = Array.Empty<MudExFileDisplayDataBaseRow>() });

        try
        {
            return _nativeMode ? ServerReloadNativeAsync(state, ct) : ServerReloadPureAsync(state);
        }
        catch (Exception e)
        {
            _errorMessage = e.Message;
            return Task.FromResult(new GridData<MudExFileDisplayDataBaseRow> { TotalItems = 0, Items = Array.Empty<MudExFileDisplayDataBaseRow>() });
        }
    }

    private async Task OnSearchChangedAsync(string value)
    {
        _searchString = value;
        if (_grid != null)
            await _grid.ReloadServerData();
    }

    private async Task OnSortChangedAsync(string columnName)
    {
        if (_sortColumn == columnName)
            _sortDescending = !_sortDescending;
        else
        {
            _sortColumn = columnName;
            _sortDescending = false;
        }

        if (_grid != null)
            await _grid.ReloadServerData();
    }

    private static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

    private async Task RefreshAsync() => await LoadDatabaseAsync();

    private void Cleanup()
    {
        try { _connection?.Close(); } catch { /* ignore */ }
        try { _connection?.Dispose(); } catch { /* ignore */ }
        _connection = null;

        if (!string.IsNullOrEmpty(_tempFilePath))
        {
            try { if (File.Exists(_tempFilePath)) File.Delete(_tempFilePath); }
            catch { /* file may still be locked briefly, ignore */ }
        }
        _tempFilePath = null;

        _pureReader = null;
        _pureRowsCache = null;
        _pureRowsCacheTable = null;

        _tables = Array.Empty<MudExFileDisplayDataBaseTable>();
        _selectedTable = null;
        _columns = new List<MudExFileDisplayDataBaseColumn>();
        _tableMessage = null;
        _customQueryResult = null;
        _customQueryColumns = null;
        _customQueryError = null;
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        try { _cts?.Cancel(); }
        catch { /* token may already be disposed */ }
        _cts?.Dispose();
        _cts = null;

        await base.DisposeAsync();
        Cleanup();
    }

    #endregion
}

/// <summary>
/// Represents a table or view inside a database file, shown in the left navigation of <see cref="MudExFileDisplayDataBase"/>
/// </summary>
public class MudExFileDisplayDataBaseTable
{
    public MudExFileDisplayDataBaseTable(string name, bool isView)
    {
        Name = name;
        IsView = isView;
    }

    public string Name { get; }
    public bool IsView { get; }
    public long RowCount { get; set; }
}

/// <summary>
/// Represents a column of a table or view inside a database file
/// </summary>
public class MudExFileDisplayDataBaseColumn
{
    public MudExFileDisplayDataBaseColumn(string name, string type, bool isPrimaryKey)
    {
        Name = name;
        Type = type;
        IsPrimaryKey = isPrimaryKey;
    }

    public string Name { get; }
    public string Type { get; }
    public bool IsPrimaryKey { get; }
}

/// <summary>
/// A single row of data shown in <see cref="MudExFileDisplayDataBase"/>, works uniformly for both the native
/// SQLite engine and the pure client-side reader.
/// </summary>
public class MudExFileDisplayDataBaseRow
{
    public MudExFileDisplayDataBaseRow(IReadOnlyDictionary<string, object> values) => Values = values;

    public IReadOnlyDictionary<string, object> Values { get; }

    public object this[string column] => Values.TryGetValue(column, out var v) ? v : null;
}
