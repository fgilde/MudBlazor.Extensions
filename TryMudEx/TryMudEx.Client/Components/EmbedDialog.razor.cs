using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Playzor.Core;
using TryMudEx.Client.Models;
using TryMudEx.Client.Services;
using Playzor.Blazor.Editor.Services;

namespace TryMudEx.Client.Components;

/// <summary>
/// Builds the embed snippets for the current code: iframe html, blazor component and plain link.
/// Everything travels inside the url, so nothing has to be saved first.
/// </summary>
public partial class EmbedDialog
{
    [Inject] private IJSRuntime JsRuntime { get; set; }
    [Inject] private ISnackbar Snackbar { get; set; }
    [Inject] private NavigationManager Navigation { get; set; }
    [Inject] private BrandingService Branding { get; set; }
    [Inject] private PlayzorLocalizer L { get; set; }

    /// <summary>Files of the current snippet. Hidden files (package list) are included so packages survive.</summary>
    [Parameter] public IEnumerable<CodeFile> Files { get; set; } = Array.Empty<CodeFile>();

    /// <summary>Set when the snippet was saved — keeps the url short.</summary>
    [Parameter] public string SnippetId { get; set; }

    private EmbedOptions _options = new();
    private string _height = "420px";

    private string Host => Navigation.BaseUri.TrimEnd('/');

    private string BrandName => Branding.Current.Name;

    private string EmbedUrl
    {
        get
        {
            var path = !string.IsNullOrWhiteSpace(SnippetId) ? SnippetId : InlineCode.Encode(Files);
            var query = _options.ToQueryString();
            if (!string.IsNullOrEmpty(Branding.DevBrandOverride))
                query = string.IsNullOrEmpty(query) ? $"brand={Branding.DevBrandOverride}" : $"{query}&brand={Branding.DevBrandOverride}";
            return $"{Host}/embed/{path}{(query.Length > 0 ? "?" + query : string.Empty)}";
        }
    }

    private string IframeSnippet =>
        $"<iframe src=\"{EmbedUrl}\"\n" +
        $"        style=\"width:100%;height:{_height};border:0\" title=\"{BrandName} playground\"\n" +
        "        loading=\"lazy\"></iframe>";

    private string ComponentSnippet
    {
        get
        {
            var attributes = new List<string>();
            if (!string.IsNullOrWhiteSpace(SnippetId)) attributes.Add($"SnippetId=\"{SnippetId}\"");
            else attributes.Add("Code=\"@myCode\"");

            if (_options.View != EmbedView.Split) attributes.Add($"View=\"PlayzorView.{_options.View}\"");
            if (_options.Theme != "auto") attributes.Add($"Theme=\"PlayzorTheme.{char.ToUpperInvariant(_options.Theme[0]) + _options.Theme[1..]}\"");
            if (!string.IsNullOrEmpty(_options.File)) attributes.Add($"File=\"{_options.File}\"");
            if (_options.ReadOnly) attributes.Add("ReadOnly");
            if (!_options.AutoRun) attributes.Add("AutoRun=\"false\"");
            if (_options.HideHeader) attributes.Add("HideHeader");
            attributes.Add($"Height=\"{_height}\"");
            attributes.Add($"Host=\"{Host}\"");

            return $"@* dotnet add package Playzor.Blazor *@\n<PlayzorPlayground {string.Join(" ", attributes)} />";
        }
    }

    /// <summary>
    /// The same embed as a custom element — for pages that are not Blazor apps. The element builds
    /// the url itself, so the code stays readable instead of being an encoded blob.
    /// </summary>
    private string WebComponentSnippet
    {
        get
        {
            var attributes = new List<string>();
            if (!string.IsNullOrWhiteSpace(SnippetId)) attributes.Add($"snippet-id=\"{SnippetId}\"");
            if (_options.View != EmbedView.Split) attributes.Add($"view=\"{_options.View.ToString().ToLowerInvariant()}\"");
            if (_options.Theme != "auto") attributes.Add($"theme=\"{_options.Theme}\"");
            if (!string.IsNullOrEmpty(_options.File)) attributes.Add($"file=\"{_options.File}\"");
            if (_options.ReadOnly) attributes.Add("readonly");
            if (!_options.AutoRun) attributes.Add("autorun=\"false\"");
            if (_options.HideHeader) attributes.Add("hide-header");
            attributes.Add($"height=\"{_height}\"");
            attributes.Add($"host=\"{Host}\"");

            var tag = $"<playzor-playground {string.Join(" ", attributes)}>";
            var body = string.IsNullOrWhiteSpace(SnippetId)
                ? "\n" + (Files?.FirstOrDefault(f => f.Path == CoreConstants.MainComponentFilePath)?.Content ?? string.Empty).Trim() + "\n"
                : string.Empty;

            return $"<script type=\"module\" src=\"{Host}/_content/Playzor.Blazor/playzor-embed.js\"></script>\n\n" +
                   $"{tag}{body}</playzor-playground>";
        }
    }

    private void Update(EmbedOptions options)
    {
        _options = options;
        StateHasChanged();
    }

    private async Task CopyAsync(string text)
    {
        await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
        Snackbar.Add(L["Copied to clipboard"], Severity.Success, o => o.VisibleStateDuration = 1200);
    }
}
