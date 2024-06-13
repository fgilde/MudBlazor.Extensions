using ExcelDataReader;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Services;
using Nextended.Core;
using Nextended.Core.Extensions;
using System.Data;
using System.Dynamic;


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

        private int ActiveSheetIndex { 
            get => _excelData?.Keys.ToList().IndexOf(_selectedSheet) ?? -1;
            set => _selectedSheet = _excelData?.Keys.ElementAtOrDefault(value);
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

        private Dictionary<string, List<ExpandoObject>> _excelData;        
        private Dictionary<string, Dictionary<string, string>> _cellStyles;
        
        private string _searchString;
        private Func<object, bool> _quickFilter => x =>
        {
            if (string.IsNullOrWhiteSpace(_searchString))
                return true;
            var row = x as IDictionary<string, object>;
            return row != null && row.Values.Any(value => value?.ToString()?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) == true);
        };

        /// <inheritdoc />
        public bool CanHandleFile(IMudExFileDisplayInfos fileDisplayInfos)
        {
            return MimeType.Matches(fileDisplayInfos.ContentType, "application/vnd.ms-excel", "text/csv", "application/vnd.openxmlformats-officedocument.spreadsheetml*", "application/vnd.ms-excel*");
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
                _excelData = ReadExcelFileToExpandoList(stream);
                StateHasChanged();
            }
        }

        private Dictionary<string, List<ExpandoObject>> ReadExcelFileToExpandoList(Stream stream)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var allSheetsData = new Dictionary<string, List<ExpandoObject>>();
            _cellStyles = new Dictionary<string, Dictionary<string, string>>();

            using var reader = ExcelReaderFactory.CreateReader(stream);
            var result = reader.AsDataSet();

            foreach (DataTable table in result.Tables)
            {
                var data = new List<ExpandoObject>();

                foreach (DataRow row in table.Rows)
                {
                    dynamic expando = new ExpandoObject();
                    var dict = (IDictionary<string, object>)expando;
                    foreach (DataColumn col in table.Columns)
                    {
                        dict[col.ColumnName] = row[col];
                        // Placeholder for styles
                        //_cellStyles[$"{row.Table.Rows.IndexOf(row)}_{col.Ordinal}"] = new Dictionary<string, string>
                        //{
                        //    {"font-weight", "bold"},
                        //    {"color", "red"}
                        //};
                    }
                    data.Add(expando);
                }

                allSheetsData[table.TableName] = data;
            }
            _selectedSheet = allSheetsData.Keys.First();
            return allSheetsData;
        }

        private string _selectedSheet;
        private string GetCellValue(IDictionary<string, object> context, string key) => context.TryGetValue(key, out var value) ? value?.ToString() : string.Empty;

        private string GetCellStyle(IDictionary<string, object> row, string key)
        {
            var rowIndex = _excelData[_selectedSheet].IndexOf((ExpandoObject)row) + 2; // Adjust for 1-based index and header row
            var colIndex = row.Keys.ToList().IndexOf(key) + 1; // Adjust for 1-based index

            var cellKey = $"{rowIndex}_{colIndex}";
            return _cellStyles.TryGetValue(cellKey, out var style) ? string.Join(";", style.Select(kv => $"{kv.Key}:{kv.Value}")) : string.Empty;
        }

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
