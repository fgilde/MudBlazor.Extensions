using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple component to display markdown files with MudExFileDisplay
/// </summary>
public partial class MudExFileDisplayStructuredData : IMudExFileDisplay
{
    [Inject] private MudExFileService FileService { get; set; }
    /// <summary>
    /// The name of the component
    /// </summary>
    public string Name => nameof(MudExStructuredDataEditor);

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
    /// Reference to the parent MudExFileDisplay if the component is used inside a MudExFileDisplay
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    /// <summary>
    /// Returns true if its a markdown file and we can handle it
    /// </summary>
    public bool CanHandleFile(IMudExFileDisplayInfos fileDisplayInfos)
    {
        var language = MudExCodeLanguageExtensionsMapping.GetCodeLanguageForFile(fileDisplayInfos?.FileName);
        return language is MudExCodeLanguage.Json or MudExCodeLanguage.Xml or MudExCodeLanguage.Yaml;
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var updateRequired = (parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var fileDisplayInfos) && FileDisplayInfos != fileDisplayInfos);
        await base.SetParametersAsync(parameters);
        if (updateRequired || Value == null)
        {
            try
            {
                Value = await FileService.ReadAsStringFromFileDisplayInfosAsync(FileDisplayInfos);
            }
            catch (Exception e)
            {
                MudExFileDisplay?.ShowError(e.Message);
                Value = string.Empty;
            }
            StateHasChanged();
        }
    }


}