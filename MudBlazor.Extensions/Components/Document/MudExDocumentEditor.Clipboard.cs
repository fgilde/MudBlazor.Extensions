namespace MudBlazor.Extensions.Components;

public partial class MudExDocumentEditor
{
    private Task ClipboardPasteAsync() => IsReadOnly || _surface == null ? Task.CompletedTask : _surface.PasteAsync();

    private Task ClipboardCopyAsync(bool cut) => cut && IsReadOnly || _surface == null ? Task.CompletedTask : _surface.CopyAsync(cut);
}
