using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

/// <summary>The indents and tab stops of the paragraph under the caret, in twips.</summary>
/// <param name="Left">Left indent.</param>
/// <param name="Right">Right indent.</param>
/// <param name="FirstLine">First line indent relative to the left indent, negative for hanging.</param>
/// <param name="Tabs">The tab stops.</param>
public sealed record MudExDocumentRulerParagraph(int Left, int Right, int FirstLine, IReadOnlyList<TabStop> Tabs);

/// <summary>A change the user made on the ruler; the position is relative to the left edge of the text area.</summary>
/// <param name="Kind">first, left, right, tab (move or remove a tab stop) or add (a new left tab stop).</param>
/// <param name="Index">The index of the tab stop for tab.</param>
/// <param name="Twips">The new position.</param>
/// <param name="Remove">True when a tab stop was dragged off the ruler.</param>
public sealed record MudExDocumentRulerEdit(string Kind, int Index, int Twips, bool Remove);

/// <summary>The ruler above the sheet of the document editor: page margins, indent markers and tab stops of the current paragraph.</summary>
public partial class MudExDocumentRuler
{
    private readonly JsModule<MudExDocumentRuler> _module = new();
    private ElementReference _root;
    private int _nonce;

    /// <summary>The page geometry of the section under the caret.</summary>
    [Parameter] public PageSetup Page { get; set; }

    /// <summary>The paragraph under the caret, or null to show no markers.</summary>
    [Parameter] public MudExDocumentRulerParagraph Paragraph { get; set; }

    /// <summary>True for centimetre ticks, false for inch ticks.</summary>
    [Parameter] public bool Metric { get; set; } = true;

    /// <summary>False shows the markers but does not let the user change them.</summary>
    [Parameter] public bool Editable { get; set; } = true;

    /// <summary>Raised when the user dropped a marker or clicked to add a tab stop.</summary>
    [Parameter] public EventCallback<MudExDocumentRulerEdit> Edited { get; set; }

    private int Left => Page == null ? 0 : Page.MarginLeft + Page.Gutter;

    private string Pct(int twips) => (twips * 100.0 / Math.Max(1, Page?.Width ?? 1)).ToString("0.###", CultureInfo.InvariantCulture) + "%";

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
            await _module.StartAsync(JsRuntime, this, "MudExDocumentRuler", dotnet => [_root, dotnet]);
    }

    /// <summary>Called by the browser when a marker was dropped.</summary>
    /// <param name="kind">The marker kind.</param>
    /// <param name="index">The tab stop index.</param>
    /// <param name="twips">The position relative to the text area.</param>
    /// <param name="remove">True when dragged off.</param>
    /// <returns>A task that completes when the edit was applied.</returns>
    [JSInvokable]
    public async Task OnEdit(string kind, int index, int twips, bool remove)
    {
        if (!Editable || Page == null || kind is not ("first" or "left" or "right" or "tab" or "add"))
            return;
        await Edited.InvokeAsync(new MudExDocumentRulerEdit(kind, index, Math.Clamp(twips, 0, Page.TextWidth), remove));
        _nonce++;
        StateHasChanged();
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await _module.DisposeAsync();
        await base.DisposeAsync();
    }
}
