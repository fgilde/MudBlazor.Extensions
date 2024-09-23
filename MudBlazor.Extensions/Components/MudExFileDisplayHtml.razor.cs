using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Services;
using Nextended.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple component to display Html files with MudExFileDisplay
/// </summary>
public partial class MudExFileDisplayHtml: IMudExFileDisplay
{
    [Inject] private MudExFileService FileService { get; set; }
    /// <summary>
    /// The name of the component
    /// </summary>
    public string Name => nameof(MudExFileDisplayHtml);

    /// <summary>
    /// The Current code string provided from file
    /// </summary>
    public string Value { get; private set; }


    /// <summary>
    /// The file display infos
    /// </summary>
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    [Parameter] 
    public bool Sandbox { get; set; } = true;

    /// <summary>
    /// Reference to the parent MudExFileDisplay if the component is used inside a MudExFileDisplay
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    /// <summary>
    /// Returns true if it's a markdown file and we can handle it
    /// </summary>
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
    {
        return Task.FromResult(MimeType.Matches(fileDisplayInfos.ContentType, "text/html", "application/xhtml+xml", "application/vnd.wap.xhtml+xml"));
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
                Console.WriteLine(e);
                Value = string.Empty;
            }
            StateHasChanged();
        }
    }

    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos) => Task.FromResult<IDictionary<string, object>>(null);
}