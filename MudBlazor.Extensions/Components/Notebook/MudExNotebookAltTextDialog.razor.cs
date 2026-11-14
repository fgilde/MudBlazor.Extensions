using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>The alternative text dialog of <see cref="MudExNotebookEditor"/> for a picture. Closes with the trimmed text as a <see cref="string"/>.</summary>
public partial class MudExNotebookAltTextDialog
{
    private string _text;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The current alternative text.</summary>
    [Parameter] public string Text { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _text = Text;
    }

    private void Ok() => MudDialog.Close(DialogResult.Ok((_text ?? "").Trim()));

    private void Cancel() => MudDialog.Cancel();
}
