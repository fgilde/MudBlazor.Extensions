using System;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Try.Core;

namespace TryMudEx.Client.Services;

/// <summary>
/// Resolves the active brand for the ui. The server already picked one for the meta tags,
/// so the client derives it from the same host (and honours ?brand= during development).
/// </summary>
public class BrandingService
{
    // ?brand= is a development aid: remember it for the app's lifetime so in-app navigation
    // (which drops the query) keeps the forced brand. Real domains resolve from the host.
    private static string _brandOverride;

    private readonly NavigationManager _navigation;
    private Brand _current;

    public BrandingService(NavigationManager navigation)
    {
        _navigation = navigation;
    }

    public Brand Current => _current ??= Resolve();

    public bool IsPlayzor => Current.Key.StartsWith("playzor", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The forced brand while developing, or null. Embeds append it so an iframe on localhost
    /// shows the same brand as the page around it; on real domains the host decides and this is null.
    /// </summary>
    public string DevBrandOverride
    {
        get
        {
            _ = Current; // make sure the query was read
            return _brandOverride;
        }
    }

    private Brand Resolve()
    {
        var uri = new Uri(_navigation.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);
        if (query.TryGetValue("brand", out var value) && !string.IsNullOrWhiteSpace(value))
            _brandOverride = value.ToString();

        return Brand.FromHost(uri.Host, _brandOverride);
    }
}
