using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>A heading of the document.</summary>
/// <param name="Id">The id of the paragraph.</param>
/// <param name="Text">The heading text.</param>
/// <param name="Level">The outline level, 0 for the top level.</param>
public sealed record MudExDocumentHeading(string Id, string Text, int Level);

/// <summary>The navigation pane of the document editor: the headings as an indented list.</summary>
public partial class MudExDocumentNavigation
{
    /// <summary>The headings in document order.</summary>
    [Parameter] public IReadOnlyList<MudExDocumentHeading> Items { get; set; } = [];

    /// <summary>The id of the heading the caret is under.</summary>
    [Parameter] public string CurrentId { get; set; }

    /// <summary>Raised with the paragraph id when a heading is clicked.</summary>
    [Parameter] public EventCallback<string> Selected { get; set; }
}
