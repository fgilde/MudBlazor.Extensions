using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper.Internal;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The upload dialog of <see cref="MudExFileManager"/>: a <see cref="MudExUploadEdit{T}"/> plus an ok button.
/// </summary>
/// <remarks>
/// Reusing the upload edit rather than building a picker means restrictions, archive content inspection,
/// folder upload, the drop zone and the external providers are all there and all configurable - see
/// <see cref="MudExFileManager.UploadEditConfigure"/>.
/// </remarks>
public partial class MudExFileManagerUploadDialog : MudExBaseComponent<MudExFileManagerUploadDialog>
{
    private MudExUploadEdit<UploadableFile> _uploadEdit;
    private IList<UploadableFile> _requests = new List<UploadableFile>();
    private string _error;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The rules the file manager applies, put on the upload edit before <see cref="Configure"/>.</summary>
    [Parameter] public MudExFileRestrictions Restrictions { get; set; }

    /// <summary>Applied to the upload edit, so the host controls every one of its options.</summary>
    [Parameter] public Action<MudExUploadEdit<UploadableFile>> Configure { get; set; }

    /// <summary>The directory the files are meant for. Shown in the title, not used for anything else.</summary>
    [Parameter] public string TargetName { get; set; }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (!firstRender || _uploadEdit == null)
            return;

        if (Restrictions != null)
        {
            _uploadEdit.MimeTypes = Restrictions.MimeTypes;
            _uploadEdit.MimeRestrictionType = Restrictions.MimeRestrictionType;
            _uploadEdit.Extensions = Restrictions.Extensions;
            _uploadEdit.ExtensionRestrictionType = Restrictions.ExtensionRestrictionType;
            _uploadEdit.MaxFileSize = Restrictions.MaxFileSize;
        }

        // Last, so the host can override anything the restrictions set.
        Configure?.Invoke(_uploadEdit);
    }

    private void Cancel() => MudDialog?.Cancel();

    private void Confirm()
    {
        var files = (_requests ?? new List<UploadableFile>())
            .Where(r => r?.Data is { Length: > 0 } || !string.IsNullOrEmpty(r?.Url))
            .Select(r => (IBrowserFile)new UploadableBrowserFile(r))
            .ToList();

        if (files.Count == 0)
        {
            _error = TryLocalize("Nothing to upload.");
            return;
        }

        MudDialog?.Close(DialogResult.Ok<IReadOnlyList<IBrowserFile>>(files));
    }
}
