using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Helper;
using MudBlazor.Utilities;
using Newtonsoft.Json;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions.Components.ObjectEdit;

/// <summary>
/// A powerful editor that can handle json, xml or yaml content
/// </summary>
public partial class MudExStructuredDataEditor
{
    private string _data;
    private KeyValuePair<string, MudColor>[] _cssVars;
    private bool _readOnly;
    private bool _isConvertedFromArray;
    private const string _tempProperty = "Items"; // Data strings that only represent an array or list will be converted to an object with this property name

    /// <summary>
    /// Input type of your data string
    /// </summary>
    [Parameter] public StructuredDataType? DataType { get; set; }

    /// <summary>
    /// If true and type for data is json, the editor will format the json string
    /// </summary>
    [Parameter] public bool FormatJson { get; set; }

    /// <summary>
    /// Error Message that is shown when data is invalid for the given DataType
    /// </summary>
    [Parameter] public string InvalidDataMessage { get; set; } = "Data is not a valid {0} data string";

    /// <summary>
    /// The data string to edit
    /// </summary>
    [Parameter]
    public string Data
    {
        get => _data;
        set
        {
            if (_data == value) return;
            _data = value;
            DataChanged.InvokeAsync(_data);
        }
    }

    [Parameter]
    public bool ReadOnly
    {
        get => _readOnly;
        set
        {
            _readOnly = value;
            ShowCancelButton = ShowSaveButton = !_readOnly;
        }
    }

    /// <summary>
    /// Event when data changed
    /// </summary>
    [Parameter] public EventCallback<string> DataChanged { get; set; }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var updateRequired = (parameters.TryGetValue<string>(nameof(Data), out var data) && Data != data)
            || (parameters.TryGetValue<StructuredDataType?>(nameof(DataType), out var inputType) && DataType != inputType);

        await base.SetParametersAsync(parameters);

        if (updateRequired)
        {
            if ((StructuredDataTypeValidator.TryDetectInputType(Data, out var dataType) || DataType.HasValue) && !StructuredDataTypeValidator.IsValidData(Data, DataType ?? dataType))
            {
                ErrorMessage = TryLocalize(InvalidDataMessage, (DataType ?? dataType).ToString());
                return;
            }

            ErrorMessage = null;
            MetaInformation = null;
            Value = DataType.HasValue
                        ? ReflectionHelper.CreateTypeAndDeserialize(Prepare(Data), DataType.Value, nameof(MudExStructuredDataEditor), true)
                        : ReflectionHelper.CreateTypeAndDeserialize(Prepare(Data), nameof(MudExStructuredDataEditor), true);
        }
    }

    private string Prepare(string data)
    {
        if (data.Trim().StartsWith("["))
        {
            _isConvertedFromArray = true;
            return $$$"""{"{{{_tempProperty}}}": """ +data+ " }";
        }

        if (data.Trim().StartsWith("-"))
        {
            _isConvertedFromArray = true;
            return $"{_tempProperty}: " + Environment.NewLine + MudExCodeView.RemoveEmptyLines(data.Replace("---", ""));
        }

        return data;
    }

    private string Unprepare(string data)
    {
        if (_isConvertedFromArray)
        {
            if (DataType == StructuredDataType.Json)
            {
                var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(data);
                var items = jsonObject[_tempProperty]?.ToString();
                return items ?? string.Empty;
            }

            if (DataType == StructuredDataType.Yaml)
            {
                return data.Replace($"{_tempProperty}:", "---");
            }
        }
        return data;
    }


    /// <inheritdoc />
    protected override async Task OnPropertyChange(ObjectEditPropertyMeta property)
    {
        await base.OnPropertyChange(property);
        var dataType = DataType ?? StructuredDataTypeValidator.DetectInputType(Data);
        var dataString = Value.ToString();
        if(dataType == StructuredDataType.Json && FormatJson)                  
            dataString = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(dataString), Formatting.Indented);
        
        Data = Unprepare(dataString);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)        
            _cssVars = await JsRuntime.GetCssColorVariablesAsync();        
        await base.OnAfterRenderAsync(firstRender);
    }
    
    protected override ObjectEditMeta<IStructuredDataObject> ConfigureMetaBase(ObjectEditMeta<IStructuredDataObject> meta)
    {
        RenderDataDefaults.ColorFromStringOptions<IStructuredDataObject>(_cssVars)(meta);
        if(ReadOnly)
            meta.AllProperties.AsReadOnly();
        return base.ConfigureMetaBase(meta);
    }
       
}