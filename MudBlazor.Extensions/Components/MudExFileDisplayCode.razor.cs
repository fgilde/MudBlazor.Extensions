using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;
using System.Text.Json.Nodes;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple component to display markdown files with MudExFileDisplay
/// </summary>
public partial class MudExFileDisplayCode: IMudExFileDisplay
{
    [Inject] private MudExFileService FileService { get; set; }
    /// <summary>
    /// The name of the component
    /// </summary>
    public string Name => nameof(MudExFileDisplayCode);

    /// <summary>
    /// The Current code string provided from file
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// Language
    /// </summary>
    public MudExCodeLanguage Language { get; private set; }

    /// <summary>
    /// The theme of the code block
    /// </summary>
    [Parameter] public CodeBlockTheme Theme { get; set; } = CodeBlockTheme.AtomOneDark;


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
    /// Returns true if it's a markdown file and we can handle it
    /// </summary>
    public bool CanHandleFile(IMudExFileDisplayInfos fileDisplayInfos)
    {
        var language = Language = MudExCodeLanguageExtensionsMapping.GetCodeLanguageForFile(fileDisplayInfos?.FileName);
        return language != MudExCodeLanguage.Unknown && language != MudExCodeLanguage.Markdown;
    }

    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>()
        {
            {"Lines", Value.GetLines().Count()},
            {"Language", Language.ToString()}
        });
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
                Language = MudExCodeLanguageExtensionsMapping.GetCodeLanguageForFile(fileDisplayInfos?.FileName);
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