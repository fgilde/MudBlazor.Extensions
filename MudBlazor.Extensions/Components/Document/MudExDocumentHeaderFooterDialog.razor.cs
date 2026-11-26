using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The header and footer dialog of <see cref="MudExDocumentEditor"/>: a plain text editor with one paragraph per line, an alignment and a button that inserts a
/// page number placeholder. Closes with <c>(Text, Alignment, Remove)</c>.
/// </summary>
public partial class MudExDocumentHeaderFooterDialog
{
    private string _text = "";
    private ParagraphAlignment _alignment;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>Whether a header or a footer is edited.</summary>
    [Parameter] public HeaderFooterKind Kind { get; set; }

    /// <summary>The current text, one paragraph per line; <c>{PAGE}</c> and <c>{NUMPAGES}</c> stand for page number fields.</summary>
    [Parameter] public string Text { get; set; } = "";

    /// <summary>The current alignment.</summary>
    [Parameter] public ParagraphAlignment Alignment { get; set; }

    /// <summary>True if the header or footer exists and can be removed.</summary>
    [Parameter] public bool Exists { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        (_text, _alignment) = (Text ?? "", Alignment);
    }

    private void InsertPageNumber() => _text += SetHeaderFooterTextCommand.PageToken;

    private void Ok() => MudDialog.Close(DialogResult.Ok((_text, _alignment, false)));

    private void Remove() => MudDialog.Close(DialogResult.Ok((_text, _alignment, true)));

    private void Cancel() => MudDialog.Cancel();
}
