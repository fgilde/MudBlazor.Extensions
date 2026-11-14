using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The link dialog of <see cref="MudExNotebookEditor"/>: the text to show and the address. Only <c>http</c>, <c>https</c> and <c>mailto</c>
/// addresses are accepted. Closes with the text and the trimmed address as a <c>(string Text, string Address)</c> tuple.
/// </summary>
public partial class MudExNotebookLinkDialog
{
    private string _text;
    private string _address;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The text to show, usually the selected text.</summary>
    [Parameter] public string Text { get; set; }

    /// <summary>The current address, or null for a new link.</summary>
    [Parameter] public string Address { get; set; }

    private bool Unsafe => !string.IsNullOrWhiteSpace(_address) && !IsAllowed(_address);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        (_text, _address) = (Text, Address);
    }

    internal static bool IsAllowed(string address) => NoteValues.IsSafeLink(address?.Trim());

    private void Ok()
    {
        if (IsAllowed(_address))
            MudDialog.Close(DialogResult.Ok((_text ?? "", _address.Trim())));
    }

    private void Cancel() => MudDialog.Cancel();
}
