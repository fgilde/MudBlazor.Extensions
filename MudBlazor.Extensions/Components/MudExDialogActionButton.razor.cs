using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

public partial class MudExDialogActionButton
{
    [Parameter] 
    public MudExDialogResultAction Action { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string Label { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }

    private async Task InvokeActionClick()
    {
        if (Action?.OnClick is not null)
            Action.OnClick.Invoke();
        else
            await OnClick.InvokeAsync();
    }

    public void SetDisabled(bool disabled)
    {
        Disabled = disabled;
        StateHasChanged();
    }
}

