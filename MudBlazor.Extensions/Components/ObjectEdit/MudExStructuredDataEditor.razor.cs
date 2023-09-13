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
                        ? ReflectionHelper.CreateTypeAndDeserialize(Data, DataType.Value, nameof(MudExStructuredDataEditor), true)
                        : ReflectionHelper.CreateTypeAndDeserialize(Data, nameof(MudExStructuredDataEditor), true);
        }
    }

    /// <inheritdoc />
    protected override async Task OnPropertyChange(ObjectEditPropertyMeta property)
    {
        await base.OnPropertyChange(property);
        var dataType = DataType ?? StructuredDataTypeValidator.DetectInputType(Data);
        var dataString = Value.ToString();
        if(dataType == StructuredDataType.Json && FormatJson)                  
            dataString = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(dataString), Formatting.Indented);
        
        Data = dataString;
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
        return base.ConfigureMetaBase(meta);
    }
       
}