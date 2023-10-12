using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Dialog to display a file using <see cref="MudExFileDisplay"/> component.
/// </summary>
public partial class MudExFileDisplayDialog
{
    
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
    /// Icon to display in the dialog.
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public string Icon { get; set; }

    /// <summary>
    /// If true icons are colored
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public MudExColor DialogIconColor { get; set; } = Color.Error;

    /// <summary>
    /// Action buttons to display in the dialog.
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public MudExDialogResultAction[] Buttons { get; set; }


    /// <inheritdoc />
    protected override void OnParametersSet()
    {        
        base.OnParametersSet();
        FileName ??=MudDialog.Title;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Class ??= MudExCss.Get(MudExCss.Classes.Dialog.FullHeightContent, "overflow-hidden", MudExCss.Classes.Dialog._Initial);
    }

    public static Task<IMudExDialogReference<MudExFileDisplayDialog>> Show(IDialogService dialogService, string url, string fileName, string contentType, Action<DialogOptionsEx> options = null) => dialogService.ShowFileDisplayDialog(url, fileName, contentType, options);
    public static Task<IMudExDialogReference<MudExFileDisplayDialog>> Show(IDialogService dialogService, IBrowserFile browserFile, Action<DialogOptionsEx> options = null) => dialogService.ShowFileDisplayDialog(browserFile, options);
    public static Task<IMudExDialogReference<MudExFileDisplayDialog>> Show(IDialogService dialogService, Stream stream, string fileName, string contentType, Action<DialogOptionsEx> options = null) => dialogService.ShowFileDisplayDialog(stream, fileName, contentType, options);

    void Submit(DialogResult result) => MudDialog.Close(result);
    void Cancel() => MudDialog.Cancel();
}