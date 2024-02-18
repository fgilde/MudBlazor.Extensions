using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Action button for the MudExDialog
/// </summary>
public partial class MudExDialogActionButton
{
    /// <summary>
    /// Action to invoke when the button is clicked
    /// </summary>
    [Parameter] 
    public MudExDialogResultAction Action { get; set; }

    /// <summary>
    /// Disabled state of the button
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Label of the button
    /// </summary>
    [Parameter]
    public string Label { get; set; }

    /// <summary>
    /// Callback when the button is clicked
    /// </summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

    private async Task InvokeActionClick()
    {
        if (Action?.OnClick is not null)
            Action.OnClick.Invoke();
        else
            await OnClick.InvokeAsync();
    }

    /// <summary>
    /// Set the disabled state of the button
    /// </summary>
    public void SetDisabled(bool disabled)
    {
        Disabled = disabled;
        StateHasChanged();
    }
}

