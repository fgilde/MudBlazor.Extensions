using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.WebUtilities;

namespace TryMudEx.Client.Models;

public enum EmbedView
{
    Split,
    Code,
    Preview,
}

/// <summary>Query options of the embed page (/embed/...). Parsed once, no Uri.Contains guessing.</summary>
public record EmbedOptions
{
    public EmbedView View { get; init; } = EmbedView.Split;
    public string File { get; init; }
    public bool ReadOnly { get; init; }
    public bool AutoRun { get; init; } = true;
    public string Theme { get; init; } = "auto";
    public bool HideHeader { get; init; }

    public static EmbedOptions Parse(string uri)
    {
        var query = QueryHelpers.ParseQuery(new Uri(uri).Query);

        string Get(string key) => query.TryGetValue(key, out var v) ? v.ToString() : null;

        bool Flag(string key, bool defaultValue = false)
        {
            if (!query.TryGetValue(key, out var value)) return defaultValue;
            var raw = value.ToString();
            // presence without value means true ("?readonly")
            return string.IsNullOrEmpty(raw) || !bool.TryParse(raw, out var parsed) || parsed;
        }

        return new EmbedOptions
        {
            View = Get("view")?.ToLowerInvariant() switch
            {
                "code" => EmbedView.Code,
                "preview" => EmbedView.Preview,
                _ => EmbedView.Split,
            },
            File = Get("file"),
            ReadOnly = Flag("readonly"),
            AutoRun = Flag("autorun", true),
            Theme = Get("theme")?.ToLowerInvariant() switch
            {
                "dark" => "dark",
                "light" => "light",
                _ => "auto",
            },
            HideHeader = Flag("hideheader"),
        };
    }

    /// <summary>Builds the query part of an embed url (without leading '?').</summary>
    public string ToQueryString()
    {
        var parts = new List<string>();
        if (View != EmbedView.Split) parts.Add("view=" + View.ToString().ToLowerInvariant());
        if (!string.IsNullOrEmpty(File)) parts.Add("file=" + Uri.EscapeDataString(File));
        if (ReadOnly) parts.Add("readonly=true");
        if (!AutoRun) parts.Add("autorun=false");
        if (Theme != "auto") parts.Add("theme=" + Theme);
        if (HideHeader) parts.Add("hideheader=true");
        return string.Join("&", parts);
    }
}
