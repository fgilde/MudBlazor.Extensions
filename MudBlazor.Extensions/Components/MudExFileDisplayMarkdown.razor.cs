using System.Windows.Input;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple component to display markdown files with MudExFileDisplay
/// </summary>
public partial class MudExFileDisplayMarkdown: IMudExFileDisplay
{
    private bool _requiredJsLoaded;

    [Inject] private MudExFileService fileService { get; set; }
    /// <summary>
    /// The name of the component
    /// </summary>
    public string Name => nameof(MudExFileDisplayMarkdown);

    /// <summary>
    /// The Current markdown string provided from file
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// The theme of the code block
    /// </summary>
    [Parameter] public CodeBlockTheme CodeBlockTheme { get; set; } = CodeBlockTheme.AtomOneDark;

    /// <summary>
    /// LinkCommand
    /// </summary>
    [Parameter] public ICommand LinkCommand { get; set; }

    /// <summary>
    /// TableCellMinWidth
    /// </summary>
    [Parameter] public int? TableCellMinWidth { get; set; }

    /// <summary>
    /// Styling
    /// </summary>
    [Parameter] public MudMarkdownStyling Styling { get; set; }

    /// <summary>
    /// OverrideLinkUrl
    /// </summary>
    [Parameter] public Func<LinkInline, string> OverrideLinkUrl { get; set; }

    /// <summary>
    /// OverrideHeaderTypo
    /// </summary>
    [Parameter] public Func<Typo, Typo> OverrideHeaderTypo { get; set; }

    /// <summary>
    /// The file display infos
    /// </summary>
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Returns true if its a markdown file and we can handle it
    /// </summary>
    public bool CanHandleFile(IMudExFileDisplayInfos fileDisplayInfos) 
        => fileDisplayInfos?.FileName?.EndsWith(".md") == true || fileDisplayInfos?.FileName?.EndsWith(".markdown") == true || fileDisplayInfos?.ContentType == "text/markdown" || fileDisplayInfos?.ContentType == "text/x-markdown";

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var updateRequired = (parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var fileDisplayInfos) && FileDisplayInfos != fileDisplayInfos);
        await base.SetParametersAsync(parameters);
        if (updateRequired || Value == null)
        {
            Value = await fileService.ReadFromFileDisplayInfosAsync(FileDisplayInfos);
            StateHasChanged();
        }
    }

    private void OnSourceLoaded(string file)
    {
        if (file.EndsWith(".js"))
            _requiredJsLoaded = true;
    }

}