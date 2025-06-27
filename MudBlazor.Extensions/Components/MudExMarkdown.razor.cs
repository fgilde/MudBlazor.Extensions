using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple component to display markdown
/// </summary>
public partial class MudExMarkdown
{
    private bool _jsReady;

    private CodeBlockTheme? _codeBlockTheme;
    private RenderFragment Inherited() => builder => base.BuildRenderTree(builder);

    [Inject] private MudExFileService FileService { get; set; }
    [Inject] private IMudMarkdownThemeService MudMarkdownThemeService { get; set; }


    /// <summary>
    /// The theme of the code block
    /// </summary>
    [Parameter]
    public CodeBlockTheme? CodeBlockTheme
    {
        get => _codeBlockTheme;
        set
        {
            if (value != null && value != _codeBlockTheme)
            {
                MudMarkdownThemeService.SetCodeBlockTheme((_codeBlockTheme = value).Value);
                InvokeAsync(StateHasChanged);
            }
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JsRuntime.LoadMudMarkdownAsync();
            _jsReady = true;
            if(_codeBlockTheme != null)
                MudMarkdownThemeService.SetCodeBlockTheme(_codeBlockTheme.Value);

            await InvokeAsync(StateHasChanged);
        }

        await base.OnAfterRenderAsync(firstRender);
    }
    
}