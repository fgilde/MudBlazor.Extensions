using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple MessageDialog
/// </summary>
public partial class MudExMessageDialog
{
    [CascadingParameter] MudDialogInstance MudDialog { get; set; }

    /// <summary>
    /// Gets or sets the style of the dialog
    /// </summary>
    [Parameter] public string Style { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the class of the dialog
    /// </summary>
    [Parameter] public string Class { get; set; } = MudExCss.Classes.Dialog._Initial;

    /// <summary>
    /// Gets or sets the class for the content of the dialog
    /// </summary>
    [Parameter] public string ClassContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message of the dialog
    /// </summary>
    [Parameter] public string Message { get; set; }
    
    /// <summary>
    /// Gets or sets whether empty actions are allowed in the dialog
    /// </summary>
    [Parameter] public bool AllowEmptyActions { get; set; }

    /// <summary>
    /// Gets or sets the icon of the dialog
    /// </summary>
    [Parameter] public string Icon { get; set; }

    /// <summary>
    /// Gets or sets the details of the dialog
    /// </summary>
    [Parameter] public IEnumerable<string> Details { get; set; }

    /// <summary>
    /// Gets or sets the buttons of the dialog
    /// </summary>
    [Parameter] public MudExDialogResultAction[] Buttons { get; set; }

    /// <summary>
    /// Gets or sets the content of the dialog
    /// </summary>
    [Parameter] public RenderFragment Content { get; set; }

    /// <summary>
    /// Gets or sets whether progress is shown in the dialog
    /// </summary>
    [Parameter] public bool ShowProgress { get; set; }

    /// <summary>
    /// Gets or sets the value of the progress shown in the dialog
    /// </summary>
    [Parameter] public int ProgressValue { get; set; }

    /// <summary>
    /// Gets or sets the minimum value of the progress shown in the dialog
    /// </summary>
    [Parameter] public double ProgressMin { get; set; }

    /// <summary>
    /// Gets or sets the maximum value of the progress shown in the dialog
    /// </summary>
    [Parameter] public double ProgressMax { get; set; } = 100.0;

    /// <summary>
    /// Gets or sets the component associated with the dialog
    /// </summary>
    internal ComponentBase Component { get; set; }


    void Submit(DialogResult result)
    {
        MudDialog.Close(result);
    }

    /// <summary>
    /// Cancels the dialog
    /// </summary>
    void Cancel() => MudDialog.Cancel();
}