using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple MessageDialog
/// </summary>
public partial class MudExMessageDialog
{
    [CascadingParameter] MudDialogInstance MudDialog { get; set; }

    [Parameter] public string Style { get; set; } = string.Empty;
    [Parameter] public string Class { get; set; } = MudExCss.Classes.Dialog._Initial;
    [Parameter] public string ClassContent { get; set; } = string.Empty;

    [Parameter] public string Message { get; set; }
    [Parameter] public bool AllowEmptyActions { get; set; }
    [Parameter] public string Icon { get; set; }
    [Parameter] public IEnumerable<string> Details { get; set; }
    [Parameter] public MudExDialogResultAction[] Buttons { get; set; }
    [Parameter] public RenderFragment Content { get; set; }

    [Parameter] public bool ShowProgress { get; set; }
    [Parameter] public int ProgressValue { get; set; }
    [Parameter] public double ProgressMin { get; set; }
    [Parameter] public double ProgressMax { get; set; } = 100.0;

    internal ComponentBase Component { get; set; }


    void Submit(DialogResult result)
    {
        MudDialog.Close(result);
    }

    void Cancel() => MudDialog.Cancel();
}