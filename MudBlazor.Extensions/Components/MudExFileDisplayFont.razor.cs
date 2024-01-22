using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Services;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple component to display markdown files with MudExFileDisplay
/// </summary>
public partial class MudExFileDisplayFont: IMudExFileDisplay
{
    private int minFontSize = 8;
    private int maxFontSize = 28;
    string[] classes = { "bold", "italic", "underline" };
    List<string> combinations;

    [Inject] private MudExFileService fileService { get; set; }

    [Parameter] public string SampleText { get; set; } = "The quick brown fox jumps over the lazy dog 0123456789";

    [Parameter] public MudExColor HeaderColor { get; set; } = MudExColor.Primary;

    [Parameter] public MudExColor DividerColor { get; set; } = MudExColor.Primary;
    
    [Parameter] public string Url { get; set; }

    /// <summary>
    /// The name of the component
    /// </summary>
    public string Name => nameof(MudExFileDisplayFont);

    /// <summary>
    /// The Current markdown string provided from file
    /// </summary>
    public string Value { get; private set; }

  
    /// <summary>
    /// The file display infos
    /// </summary>
    [Parameter] public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    /// <summary>
    /// Returns true if it's a font file and we can handle it
    /// </summary>
    public bool CanHandleFile(IMudExFileDisplayInfos fileDisplayInfos) => MimeType.Matches(fileDisplayInfos.ContentType, "application/font-*", "font/*", "application/vnd.ms-fontobject");

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var updateRequired = (parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var fileDisplayInfos) && FileDisplayInfos != fileDisplayInfos);
        await base.SetParametersAsync(parameters);
        if (updateRequired || Value == null)
        {
            try
            {
                Url = fileDisplayInfos?.Url;
                if (fileDisplayInfos != null && string.IsNullOrEmpty(Url) && fileDisplayInfos.ContentStream != null)
                {
                    Url = await fileService.CreateDataUrlAsync(fileDisplayInfos.ContentStream.ToByteArray(), fileDisplayInfos.ContentType, false);
                }
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