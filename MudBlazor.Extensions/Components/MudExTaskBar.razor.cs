using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Components;

[Obsolete("Not finished yet. Will be implemented later")]
public partial class MudExTaskBar: MudExSlideBar
{
    [Parameter] public bool OnlyVisibleWithWindows { get; set; } = true;
    
    private List<DialogTaskBarInfo> _taskbarItems = new();
    protected override void OnInitialized()
    {
        DialogService.OnDialogInstanceAdded += DialogService_OnDialogInstanceAdded;
    }
    
    private async void DialogService_OnDialogInstanceAdded(IDialogReference obj)
    {
        await obj.GetDialogAsync<ComponentBase>(); // Wait until dialog is rendered
        var data = await Js.InvokeAsync<DialogData>("MudBlazorExtensions.attachDialog", obj?.GetDialogId());
        if (data != null)
        {
            _taskbarItems.Add(new DialogTaskBarInfo(obj, data));
            StateHasChanged();
            if (obj is {Result: { }})
            {
                await obj.Result;
                _taskbarItems.Remove(_taskbarItems.FirstOrDefault(x => x.DialogReference == obj));
                StateHasChanged();
            }
        }
    }
    
    private void CloseWindow(MudTabPanel panel)
    {
        var dialog = _taskbarItems.FirstOrDefault(x => x.DialogId == (string)panel.Tag);
        CloseDialog(dialog);
    }

    private void CloseDialog(DialogTaskBarInfo dialog)
    {
        if (dialog != null)
        {
            dialog.DialogReference.Close();
            _taskbarItems.Remove(dialog);
        }
    }

    private Task ShowWindow(DialogTaskBarInfo taskBarItem)
    {
        return Task.CompletedTask;
    }
}

public class DialogData
{
    public string Title { get; set; }
    public string Icon { get; set; }
}

public class DialogTaskBarInfo
{
    public DialogTaskBarInfo(IDialogReference dialogReference, DialogData data)
    {
        DialogData = data;
        DialogId = dialogReference.GetDialogId();
        DialogReference = dialogReference;
    }
    public DialogData DialogData { get; set; }
    public string DialogId { get; set; }
    public IDialogReference DialogReference { get; set; }
}