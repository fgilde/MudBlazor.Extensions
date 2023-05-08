using BlazorJS;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Security.Cryptography.X509Certificates;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// MudExTaskBar inherits the MudExSlideBar and provides a tabbar to select non modal dialogs. Just provide <MudExTaskBar/> somewhere in your Layout
/// </summary>
[Obsolete("Not finished yet. Will be implemented later")]
public partial class MudExTaskBar: MudExSlideBar
{
    [Parameter] public bool OnlyVisibleWithWindows { get; set; } = true;
    public bool HasItems => _taskbarItems?.Any() == true;

    private List<DialogTaskBarInfo> _taskbarItems = new();
    protected override void OnInitialized()
    {
        DialogService.OnDialogInstanceAdded += DialogService_OnDialogInstanceAdded;
        DialogService.OnDialogCloseRequested += DialogService_OnDialogCloseRequested;
    }

    private void DialogService_OnDialogCloseRequested(IDialogReference arg1, DialogResult arg2)
    {
        
    }

    private async void DialogService_OnDialogInstanceAdded(IDialogReference obj)
    {
        await obj.GetDialogAsync<ComponentBase>(); // Wait until dialog is rendered
        var data = await JsRuntime.InvokeAsync<DialogData>("MudBlazorExtensions.attachDialog", obj?.GetDialogId());
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

    private async Task ShowWindow(DialogTaskBarInfo taskBarItem)
    {
        await JsRuntime.DInvokeVoidAsync((window, dialogId) => window.document.getElementById(dialogId).style.visibility = "visible", taskBarItem.DialogId);
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