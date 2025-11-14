using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.FileProviders;
using Microsoft.JSInterop;
using Nextended.Core.Extensions;
using System.Reflection;
using BlazorJS;

using System.Text;

namespace MainSample.WebAssembly.Shared;

public partial class Disqus
{
    internal const string DisqusNamespace = "DISQUS";
    internal const string DisqusSrc = "https://mudblazor-extensions.disqus.com/embed.js";
    internal const string DisqusAutoLoadStorageKey = "__disqusAllowed";
    
    private const string PageUrl = "https://www.mudex.org";
    private string _id = $"disqus_{Guid.NewGuid().ToFormattedId()}";
    private bool _loaded = false;
    private string _pageIdentifier = "";
    private bool _rememberChoice;
    //private string _stylesFromResource;
    
    [Parameter] public bool AutoLoad { get; set; }

    protected override async Task OnInitializedAsync()
    {
        //_stylesFromResource ??= await GetStyleResourceAsync();
        await base.OnInitializedAsync();
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await CheckPageIdChange();
        if (firstRender && (AutoLoad || (await LocalStorageService.GetItemAsync<string>(DisqusAutoLoadStorageKey) == "true")))
            await LoadDisqusIfNotLoadedAsync();

        if (firstRender)
            NavigationManager.LocationChanged += LocationChanged;

        await base.OnAfterRenderAsync(firstRender);
    }

    private void LocationChanged(object? sender, LocationChangedEventArgs e) => _ = CheckPageIdChange();

    private async Task<string> GetStyleResourceAsync()
    {
        var assembly = Assembly.GetAssembly(typeof(Disqus));
        if (assembly == null)
            return string.Empty;
        var typeName = typeof(Disqus).FullName?[((assembly.GetName().Name?.Length ?? 0) + 1)..];
        var resourceName = $"{typeName}.razor.css";

        var embeddedProvider = new EmbeddedFileProvider(assembly);

        var fileInfo = embeddedProvider.GetFileInfo(resourceName);
        await using var resourceStream = fileInfo.CreateReadStream();
        using var reader = new StreamReader(resourceStream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private async Task CheckPageIdChange(bool forceUpdate = false)
    {
        var pageId = GetPageIdentifier();
        if (pageId != _pageIdentifier || forceUpdate)
        {
            _pageIdentifier = pageId;
            await UpdateDisqusAsync();
        }
    }

    private string GetPageIdentifier()
    {
        var pageId = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        if (pageId == "/" || string.IsNullOrEmpty(pageId))
            pageId = "/index";
        return pageId.EnsureStartsWith("/").EnsureEndsWith("/");
    }

    private async Task UpdateDisqusAsync()
    {
        if (await JsRuntime.IsNamespaceAvailableAsync(DisqusNamespace))
        {
            string script = $@"
                DISQUS.reset({{
                    reload: true,
                    config: function () {{
                        //this.page.url = '{PageUrl}{_pageIdentifier}';
                        this.page.identifier = '{_pageIdentifier}';
                        this.disable_ads = true;
                    }}
                }});            
            ";
            await JsRuntime.InvokeVoidAsync("eval", script);
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (MainLayout.Instance != null)
            MainLayout.Instance.LanguageChanged += (_, _) => InvokeAsync(StateHasChanged);
    }

    public async Task LoadDisqusIfNotLoadedAsync()
    {
        if (_loaded)
            return;
        if (!await JsRuntime.IsNamespaceAvailableAsync(DisqusNamespace))
            await JsRuntime.LoadFilesAsync(DisqusSrc);

        _loaded = true;
        await InvokeAsync(StateHasChanged);
        await CheckPageIdChange(true);
    }

    private async Task EnableDisqus(MouseEventArgs arg)
    {
        if (_rememberChoice)
            await LocalStorageService.SetItemAsync(DisqusAutoLoadStorageKey, "true");
        await LoadDisqusIfNotLoadedAsync();
    }

}