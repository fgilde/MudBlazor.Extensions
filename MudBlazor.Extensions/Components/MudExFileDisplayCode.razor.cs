using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple component to display markdown files with MudExFileDisplay
/// </summary>
public partial class MudExFileDisplayCode: IMudExFileDisplay
{
    [Inject] private MudExFileService fileService { get; set; }
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

    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    /// <summary>
    /// Returns true if its a markdown file and we can handle it
    /// </summary>
    public bool CanHandleFile(IMudExFileDisplayInfos fileDisplayInfos)
    {
        var language = Language = MudExCodeLanguageExtensionsMapping.GetCodeLanguageForFile(fileDisplayInfos?.FileName);
        return language != MudExCodeLanguage.Unknown && language != MudExCodeLanguage.Markdown;
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
                Value = await fileService.ReadAsStringFromFileDisplayInfosAsync(FileDisplayInfos);
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