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

    // MudBlazor.Markdown 9.10 ignores CodeBlockTheme and colours code only through the --mud-markdown-codehightlight-* variables of
    // its MudMarkdownThemeProvider. Setting them here from the closest built-in palette keeps CodeBlockTheme working; Default leaves them to that provider.
    private string CodeHighlightStyle()
    {
        if (CodeHighlightPalette(_codeBlockTheme) is not { } p)
            return null;
        const string v = "--mud-markdown-codehightlight";
        return $"{v}-background: {p.Background}; {v}-text: {p.Text}; {v}-keyword: {p.Keyword}; {v}-string: {p.String}; {v}-comment: {p.Comment}; " +
               $"{v}-function: {p.Function}; {v}-type: {p.Type}; {v}-number: {p.Numbers}; {v}-meta: {p.Meta};";
    }

    private static MudMarkdownTheme.CodeHighlight CodeHighlightPalette(CodeBlockTheme? theme) => theme switch
    {
        null or MudBlazor.CodeBlockTheme.Default => null,
        MudBlazor.CodeBlockTheme.AtomOneDark or MudBlazor.CodeBlockTheme.AtomOneDarkReasonable => MudCodeHighlightThemes.Dark.OneDark,
        MudBlazor.CodeBlockTheme.AtomOneLight => MudCodeHighlightThemes.Light.OneLight,
        MudBlazor.CodeBlockTheme.Github or MudBlazor.CodeBlockTheme.StackoverflowLight => MudCodeHighlightThemes.Light.GitHub,
        MudBlazor.CodeBlockTheme.GithubDark or MudBlazor.CodeBlockTheme.GithubDarkDimmed or MudBlazor.CodeBlockTheme.StackoverflowDark => MudCodeHighlightThemes.Dark.GitHub,
        MudBlazor.CodeBlockTheme.Monokai or MudBlazor.CodeBlockTheme.MonokaiSublime => MudCodeHighlightThemes.Dark.Monokai,
        MudBlazor.CodeBlockTheme.Nord => MudCodeHighlightThemes.Dark.Nord,
        MudBlazor.CodeBlockTheme.NightOwl => MudCodeHighlightThemes.Dark.NightOwl,
        MudBlazor.CodeBlockTheme.Xcode => MudCodeHighlightThemes.Light.Xcode,
        MudBlazor.CodeBlockTheme.Vs or MudBlazor.CodeBlockTheme.Idea => MudCodeHighlightThemes.Light.LightPlus,
        MudBlazor.CodeBlockTheme.TomorrowNightBlue or MudBlazor.CodeBlockTheme.TomorrowNightBright => MudCodeHighlightThemes.Dark.TokyoNight,
        _ => IsDarkCodeBlockTheme(theme.Value) ? MudCodeHighlightThemes.Dark.DarkPlus : MudCodeHighlightThemes.Light.LightPlus
    };

    private static bool IsDarkCodeBlockTheme(CodeBlockTheme theme)
        => theme.ToString().Contains("Dark") || theme is MudBlazor.CodeBlockTheme.Agate or MudBlazor.CodeBlockTheme.AnOldHope or MudBlazor.CodeBlockTheme.Androidstudio
            or MudBlazor.CodeBlockTheme.Arta or MudBlazor.CodeBlockTheme.Devibeans or MudBlazor.CodeBlockTheme.Far or MudBlazor.CodeBlockTheme.Hybrid or MudBlazor.CodeBlockTheme.IrBlack
            or MudBlazor.CodeBlockTheme.Lioshi or MudBlazor.CodeBlockTheme.Obsidian or MudBlazor.CodeBlockTheme.Pojoaque or MudBlazor.CodeBlockTheme.Rainbow
            or MudBlazor.CodeBlockTheme.ShadesOfPurple or MudBlazor.CodeBlockTheme.Srcery or MudBlazor.CodeBlockTheme.Sunburst or MudBlazor.CodeBlockTheme.Vs2015 or MudBlazor.CodeBlockTheme.Xt256;
    
}