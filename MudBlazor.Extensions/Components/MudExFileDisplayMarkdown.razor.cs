using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple component to display markdown files with MudExFileDisplay
/// </summary>
public partial class MudExFileDisplayMarkdown: IMudExFileDisplay
{
    private RenderFragment Inherited() => builder => base.BuildRenderTree(builder);
    
    [Inject] private MudExFileService FileService { get; set; }
    
    /// <summary>
    /// The name of the component
    /// </summary>
    public string Name => nameof(MudExFileDisplayMarkdown);


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
    public virtual Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService) 
        => Task.FromResult(fileDisplayInfos?.FileName?.EndsWith(".md") == true || fileDisplayInfos?.FileName?.EndsWith(".markdown") == true || fileDisplayInfos?.ContentType == "text/markdown" || fileDisplayInfos?.ContentType == "text/x-markdown");

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
    
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos) => Task.FromResult<IDictionary<string, object>>(null);

}