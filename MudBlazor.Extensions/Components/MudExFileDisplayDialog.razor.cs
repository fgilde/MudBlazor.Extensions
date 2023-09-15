using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Dialog to display a file using <see cref="MudExFileDisplay"/> component.
/// </summary>
public partial class MudExFileDisplayDialog
{
    /// <summary>
    /// If true, compact vertical padding will be applied to items.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool Dense { get; set; }
    
    /// <summary>
    /// The MudDialog instance
    /// </summary>
    [CascadingParameter] MudDialogInstance MudDialog { get; set; }
    
    /// <summary>
    /// CSS classes applied to the content of the dialog.
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public string ClassContent { get; set; } = "full-height";

    /// <summary>
    /// Whether to show content error if content cannot be displayed.
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public bool ShowContentError { get; set; } = true;

    /// <summary>
    /// Icon to display in the dialog.
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public string Icon { get; set; }

    /// <summary>
    /// URL to access the file or resource.
    /// </summary>
    [Parameter]
    [SafeCategory("Data")]
    public string Url { get; set; }

    /// <summary>
    /// Content type of the loaded file or resource.
    /// </summary>
    [Parameter]
    [SafeCategory("Data")]
    public string ContentType { get; set; }

    /// <summary>
    /// Stream of the file or resource content.
    /// Note: This stream should not be closed or disposed.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public Stream ContentStream { get; set; }

    /// <summary>
    /// Action buttons to display in the dialog.
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public MudExDialogResultAction[] Buttons { get; set; }

    /// <summary>
    /// Function to handle content error. Return true if the error is handled, false to show the error message.
    /// </summary>
    [Parameter]
    [SafeCategory("Validation")]
    public Func<IMudExFileDisplayInfos, Task<MudExFileDisplayContentErrorResult>> HandleContentErrorFunc { get; set; }

    /// <summary>
    /// Custom message to show in case of content error.
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public string CustomContentErrorMessage { get; set; }

    /// <summary>
    /// Whether to use fallback to iframe if file is not binary.
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public bool FallBackInIframe { get; set; }

    /// <summary>
    /// Whether to use specific tags for certain content types or to always use iframe/object tag.
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public bool ViewDependsOnContentType { get; set; } = true;

    /// <summary>
    /// Whether to use the image as background image instead of img tag.
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public bool ImageAsBackgroundImage { get; set; } = false;

    /// <summary>
    /// Whether to use sandbox mode on iframes to disallow dangerous JS invocation.
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public bool SandBoxIframes { get; set; } = true;

    /// <summary>
    /// Whether to allow user to download the loaded file.
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public bool AllowDownload { get; set; } = true;


    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Class ??= MudExCss.Get(MudExCss.Classes.Dialog.FullHeightContent, "overflow-hidden", MudExCss.Classes.Dialog._Initial);
    }

    public static Task<IDialogReference> Show(IDialogService dialogService, string url, string fileName, string contentType, Action<DialogOptionsEx> options = null) => dialogService.ShowFileDisplayDialog(url, fileName, contentType, options);
    public static Task<IDialogReference> Show(IDialogService dialogService, IBrowserFile browserFile, Action<DialogOptionsEx> options = null) => dialogService.ShowFileDisplayDialog(browserFile, options);
    public static Task<IDialogReference> Show(IDialogService dialogService, Stream stream, string fileName, string contentType, Action<DialogOptionsEx> options = null) => dialogService.ShowFileDisplayDialog(stream, fileName, contentType, options);

    void Submit(DialogResult result) => MudDialog.Close(result);
    void Cancel() => MudDialog.Cancel();
}