using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Services;
using MudBlazor.Extensions.Core;
using Nextended.Core.Extensions;
using Nextended.Core;

namespace MudBlazor.Extensions.Components
{
    /// <summary>
    /// Simple Excel file viewer
    /// </summary>
    public partial class MudExFileDisplayExcel : IMudExFileDisplay
    {
        [Inject] private MudExFileService FileService { get; set; }
        
        /// <summary>
        /// The name of the component
        /// </summary>
        public string Name => nameof(MudExFileDisplayExcel);

        private int ActiveSheetIndex
        {
            get => _excelFile?.Sheets.IndexOf(_selectedSheet) ?? -1;
            set => _selectedSheet = _excelFile?.Sheets.ElementAtOrDefault(value);
        }

        /// <summary>
        /// The Current code string provided from file
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// The file display infos
        /// </summary>
        [Parameter]
        public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

        /// <summary>
        /// Src url of xlsx file
        /// </summary>
        [Parameter]
        public string Src { get; set; }

        /// <summary>
        /// Reference to the parent MudExFileDisplay if the component is used inside a MudExFileDisplay
        /// </summary>
        [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

        private ExcelFile _excelFile;
        private ExcelSheet _selectedSheet;

        private string _searchString;
        private Func<ExcelRow, bool> _quickFilter => x =>
        {
            if (string.IsNullOrWhiteSpace(_searchString))
                return true;
            return x.Cells.Values.Any(value => value?.ToString()?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) == true);
        };

        /// <inheritdoc />
        public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
        {
            return Task.FromResult(MimeType.Matches(fileDisplayInfos.ContentType, "application/vnd.ms-excel", "text/csv", "application/vnd.openxmlformats-officedocument.spreadsheetml*", "application/vnd.ms-excel*"));
        }

        public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
        {
            return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>()
            {
                {"Sheets", _excelFile?.Sheets.Count ?? 0},
                {"Rows", _excelFile?.Sheets.Sum(s => s.Rows.Count) ?? 0},
                {"Columns", _selectedSheet?.Rows.FirstOrDefault()?.Cells.Keys.Count ?? 0}
            });
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var fileInfosUpdated = parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var fileDisplayInfos) && FileDisplayInfos != fileDisplayInfos;
            var srcUpdated = parameters.TryGetValue<string>(nameof(Src), out var src) && src != Src && !string.IsNullOrEmpty(src);
            await base.SetParametersAsync(parameters);

            try
            {
                if (srcUpdated)
                {
                    await LoadXlsxAsync();
                }
                if (fileInfosUpdated || Src == null)
                {
                    Src = fileDisplayInfos?.Url ?? await FileService.CreateDataUrlAsync(fileDisplayInfos?.ContentStream?.ToByteArray() ?? throw new ArgumentException("No stream and no url available"), fileDisplayInfos.ContentType, MudExFileDisplay == null || MudExFileDisplay.StreamUrlHandling == StreamUrlHandling.BlobUrl);
                    await LoadXlsxAsync();
                }
            }
            catch (Exception e)
            {
                MudExFileDisplay?.ShowError(e.Message);
                Console.WriteLine(e);
                Value = string.Empty;
            }
        }

        private async Task LoadXlsxAsync()
        {
            if (!string.IsNullOrEmpty(Src))
            {
                var stream = await FileService.ReadStreamAsync(Src);
                _excelFile = FileService.ReadExcelFile(stream, FileDisplayInfos?.ContentType);
                _selectedSheet = _excelFile?.Sheets.FirstOrDefault();
                StateHasChanged();
            }
        }

        private string GetCellValue(ExcelRow row, string key) => row.Cells.TryGetValue(key, out var value) ? value?.ToString() : string.Empty;

    }
}



internal class DynamicComparer : IComparer<object>
{
    public int Compare(object x, object y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        if (x is IComparable comparableX && y is IComparable comparableY)
        {
            return comparableX.CompareTo(comparableY);
        }

        return string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
