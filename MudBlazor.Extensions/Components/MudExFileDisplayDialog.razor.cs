using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Dialog to display a file using <see cref="MudExFileDisplay"/> component.
/// </summary>
public partial class MudExFileDisplayDialog
{
    [CascadingParameter] MudDialogInstance MudDialog { get; set; }

    [Parameter] public string Style { get; set; } = string.Empty;
    [Parameter] public string Class { get; set; } = MudExCss.Get(MudExCss.Classes.Dialog.FullHeightContent, "overflow-hidden", MudExCss.Classes.Dialog._Initial);
    [Parameter] public string ClassContent { get; set; } = "full-height";
    [Parameter] public bool ShowContentError { get; set; } = true;
    [Parameter] public string Icon { get; set; }
    [Parameter] public string Url { get; set; }
    [Parameter] public string ContentType { get; set; }
    [Parameter] public Stream ContentStream { get; set; }
    [Parameter] public MudExDialogResultAction[] Buttons { get; set; }
    /**
     * A function to handle content error. Return true if you have handled the error and false if you want to show the error message
     * For example you can reset Url here to create a proxy fallback or display own not supported image or what ever.
     * If you reset Url or Data here you need also to reset ContentType
     */
    [Parameter] public Func<IMudExFileDisplayInfos, Task<MudExFileDisplayContentErrorResult>> HandleContentErrorFunc { get; set; }
    [Parameter] public string CustomContentErrorMessage { get; set; }

    /**
     * Should be true if file is not a binary one
     */
    [Parameter]
    public bool FallBackInIframe { get; set; }

    /// <summary>
    /// Set this to false to show everything in iframe/object tag otherwise zip, images audio and video will displayed in correct tags
    /// </summary>
    [Parameter]
    public bool ViewDependsOnContentType { get; set; } = true;

    [Parameter] public bool ImageAsBackgroundImage { get; set; } = false;
    [Parameter] public bool SandBoxIframes { get; set; } = true;
    [Parameter] public bool AllowDownload { get; set; } = true;

    public static Task<IDialogReference> Show(IDialogService dialogService, string url, string fileName, string contentType, Action<DialogOptionsEx> options = null) => dialogService.ShowFileDisplayDialog(url, fileName, contentType, options);
    public static Task<IDialogReference> Show(IDialogService dialogService, IBrowserFile browserFile, Action<DialogOptionsEx> options = null) => dialogService.ShowFileDisplayDialog(browserFile, options);
    public static Task<IDialogReference> Show(IDialogService dialogService, Stream stream, string fileName, string contentType, Action<DialogOptionsEx> options = null) => dialogService.ShowFileDisplayDialog(stream, fileName, contentType, options);

    void Submit(DialogResult result) => MudDialog.Close(result);
    void Cancel() => MudDialog.Cancel();
}