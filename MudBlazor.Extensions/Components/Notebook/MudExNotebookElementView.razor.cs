using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Shows one element of a notebook page at its position in CSS pixels. It renders again only when the element id, its
/// <see cref="NoteElement.Version"/> or its image URL changes.
/// </summary>
public partial class MudExNotebookElementView
{
    private (string Id, int Version, string Url)? _rendered;
    private string _url;

    /// <summary>The element to show.</summary>
    [Parameter] public NoteElement Element { get; set; }

    /// <summary>The image URLs of the notebook; images show their alternative text if null.</summary>
    [Parameter] public NotebookImageUrls Images { get; set; }

    private string ElementClass => Element switch
    {
        TextElement => "mud-ex-notebook-element mud-ex-notebook-text-element",
        TableElement => "mud-ex-notebook-element mud-ex-notebook-table-element",
        OpaqueElement => "mud-ex-notebook-element mud-ex-notebook-opaque",
        ImageElement when _url == null => "mud-ex-notebook-element mud-ex-notebook-opaque",
        _ => "mud-ex-notebook-element"
    };

    private (string Id, int Version, string Url) Key => (Element?.Id, Element?.Version ?? 0, _url);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _url = Element is ImageElement image ? Images?.UrlFor(image.MediaId) : null;
        _rendered ??= Key;
    }

    /// <inheritdoc />
    protected override bool ShouldRender()
    {
        var key = Key;
        if (key == _rendered) return false;
        _rendered = key;
        return true;
    }

    private static string ColorOr(string color, string fallback) => NoteValues.IsColor(color) ? color : fallback;
}
