using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Playzor.Blazor;

public enum PlayzorView
{
    /// <summary>Code and preview side by side (default).</summary>
    Split,
    Code,
    Preview,
}

public enum PlayzorTheme
{
    /// <summary>Follows the playground's own theme preference.</summary>
    Auto,
    Light,
    Dark,
}

/// <summary>
/// Embeds a live, editable Blazor playground. Either pass <see cref="Code"/> / <see cref="Files"/>
/// (travels inside the url, nothing is stored) or a <see cref="SnippetId"/> of a saved snippet.
/// </summary>
public partial class PlayzorPlayground : IAsyncDisposable
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    /// <summary>Content of the main razor file. Ignored when <see cref="Files"/> or <see cref="SnippetId"/> is set.</summary>
    [Parameter] public string? Code { get; set; }

    /// <summary>Multi file snippet: path (may contain folders) to content.</summary>
    [Parameter] public IDictionary<string, string>? Files { get; set; }

    /// <summary>Id of a snippet saved on the playground — keeps the url short.</summary>
    [Parameter] public string? SnippetId { get; set; }

    /// <summary>Playground host. Defaults to https://playzor.net.</summary>
    [Parameter] public string Host { get; set; } = "https://playzor.net";

    [Parameter] public PlayzorView View { get; set; } = PlayzorView.Split;

    [Parameter] public PlayzorTheme Theme { get; set; } = PlayzorTheme.Auto;

    /// <summary>File shown first, e.g. "Components/Card.razor".</summary>
    [Parameter] public string? File { get; set; }

    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Compile and run as soon as the embed loads. Default true.</summary>
    [Parameter] public bool AutoRun { get; set; } = true;

    /// <summary>Hides the embed's own tab bar and buttons.</summary>
    [Parameter] public bool HideHeader { get; set; }

    /// <summary>CSS height of the iframe. Ignored while <see cref="AutoHeight"/> is on.</summary>
    [Parameter] public string Height { get; set; } = "500px";

    /// <summary>Let the embed report its content height and size the iframe accordingly.</summary>
    [Parameter] public bool AutoHeight { get; set; }

    [Parameter] public string Title { get; set; } = "Playzor playground";

    [Parameter] public string? Class { get; set; }

    [Parameter] public string? Style { get; set; }

    private ElementReference _iframe;
    private IJSObjectReference? _module;

    private string IframeStyle =>
        $"width:100%;border:0;display:block;{(AutoHeight ? string.Empty : $"height:{Height};")}{Style}";

    internal string EmbedUrl
    {
        get
        {
            var host = Host.TrimEnd('/');
            var path = !string.IsNullOrWhiteSpace(SnippetId)
                ? SnippetId
                : PlayzorCode.Encode(BuildFiles());

            var query = BuildQuery();
            return $"{host}/embed/{path}{(query.Length > 0 ? "?" + query : string.Empty)}";
        }
    }

    private IEnumerable<KeyValuePair<string, string>> BuildFiles()
    {
        if (Files?.Count > 0) return Files;
        return new[] { new KeyValuePair<string, string>("__Main.razor", Code ?? "<h1>Hello Playzor</h1>") };
    }

    private string BuildQuery()
    {
        var parts = new List<string>();
        if (View != PlayzorView.Split) parts.Add("view=" + View.ToString().ToLowerInvariant());
        if (!string.IsNullOrEmpty(File)) parts.Add("file=" + Uri.EscapeDataString(File));
        if (ReadOnly) parts.Add("readonly=true");
        if (!AutoRun) parts.Add("autorun=false");
        if (Theme != PlayzorTheme.Auto) parts.Add("theme=" + Theme.ToString().ToLowerInvariant());
        if (HideHeader) parts.Add("hideheader=true");
        return string.Join("&", parts);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || !AutoHeight) return;

        _module = await JsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Playzor.Blazor/playzor.js");
        await _module.InvokeVoidAsync("observeHeight", _iframe);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;
        try
        {
            await _module.InvokeVoidAsync("stopObserving", _iframe);
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException) { /* circuit already gone */ }
    }
}
