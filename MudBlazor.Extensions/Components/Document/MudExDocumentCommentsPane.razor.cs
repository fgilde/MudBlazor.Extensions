using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

/// <summary>The read-only comments pane of the document editor.</summary>
public partial class MudExDocumentCommentsPane
{
    /// <summary>The comments of the document.</summary>
    [Parameter] public IReadOnlyList<Comment> Items { get; set; } = [];

    /// <summary>Raised with the id of the anchor paragraph when a comment is clicked.</summary>
    [Parameter] public EventCallback<string> Selected { get; set; }
}
