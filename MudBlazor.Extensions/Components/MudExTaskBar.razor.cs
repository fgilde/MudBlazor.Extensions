using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// MudExTaskBar inherits the MudExSlideBar and provides a tabbar to select non modal dialogs. Just provide <MudExTaskBar/> somewhere in your Layout
/// </summary>
[Beta("This component and the dialog no modal behaviour is still in development and not ready for production use. Please report any issues you find on GitHub.")]
public partial class MudExTaskBar: MudExSlideBar
{
    private List<DialogTaskBarInfo> _taskbarItems = new();

    /// <summary>
    /// When true the taskbar is only visible if a window is open
    /// </summary>
    [Parameter, SafeCategory("Behavior")] 
    public bool OnlyVisibleWithWindows { get; set; } = true;
    
    /// <summary>
    /// Is true if the taskbar contains items
    /// </summary>
    public bool HasItems => _taskbarItems?.Any() == true;


    /// <inheritdoc />
    protected override void OnInitialized()
    {
        DialogService.DialogInstanceAddedAsync += DialogService_OnDialogInstanceAdded;
        DialogService.OnDialogCloseRequested += DialogService_OnDialogCloseRequested;
    }

    private void DialogService_OnDialogCloseRequested(IDialogReference arg1, DialogResult arg2)
    {
        
    }

    private async Task DialogService_OnDialogInstanceAdded(IDialogReference obj)
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
        await JsRuntime.InvokeVoidAsync("MudBlazorExtensions.showDialog", taskBarItem.DialogId);
    }
}

/// <summary>
/// Data for a dialog. This data is provided by the dialog itself and used in DialogTaskBarInfo
/// </summary>
public class DialogData
{
    /// <summary>
    /// The title of the dialog
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Icon for dialog
    /// </summary>
    public string Icon { get; set; }
}

/// <summary>
/// DialogTaskBarInfo an item in the task bar
/// </summary>
public class DialogTaskBarInfo
{
    /// <summary>
    /// Creates a new DialogTaskBarInfo for given dialog
    /// </summary>
    public DialogTaskBarInfo(IDialogReference dialogReference, DialogData data)
    {
        DialogData = data;
        DialogId = dialogReference.GetDialogId();
        DialogReference = dialogReference;
    }

    /// <summary>
    /// Data for the dialog
    /// </summary>
    public DialogData DialogData { get; set; }

    /// <summary>
    /// Id of the dialog
    /// </summary>
    public string DialogId { get; set; }

    /// <summary>
    /// Reference to dialog
    /// </summary>
    public IDialogReference DialogReference { get; set; }
}