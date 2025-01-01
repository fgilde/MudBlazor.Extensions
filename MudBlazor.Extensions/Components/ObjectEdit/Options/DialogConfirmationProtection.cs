using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <summary>
/// Base class for a boolean input protection instance, providing a set of properties and methods to configure the boolean input protection.
/// </summary>
public class DialogConfirmationProtection : ToggleButtonConfirmationProtection
{
    private readonly IDialogService _dialogService;
    private readonly string _confirmationWord;
    private readonly string _helperText;
    private readonly DialogParameters _dialogParameters = new();

    public string DialogMessage { 
        get => _dialogParameters[nameof(MudExMessageDialog.Message)]?.ToString();
        set => _dialogParameters[nameof(MudExMessageDialog.Message)] = value;
    }

    public string OkText { get; set; } = "Unlock";
    public string CancelText { get; set; } = "Cancel";
    public string DialogTitle { get; set; } = "Confirmation";

    public DialogOptionsEx DialogOptions { get; set; }


    public DialogConfirmationProtection(
        IDialogService dialogService,
        string title,
        string message = null, 
        Action<MudToggleIconButton> configure = null,
        Action<MudExMessageDialog> dialogConfigure = null) : base(message, configure)
    {
        DialogTitle = title;
        DialogMessage = message;
        _dialogService = dialogService;
        if(dialogConfigure != null)        
            _dialogParameters = PropertyHelper.ValuesDictionary(dialogConfigure, true).ToDialogParameters();        
    }

    public DialogConfirmationProtection(
        IDialogService dialogService,
        string title,
        string confirmationWord,
        string message = null,
        string helperText = "Please type {0} to confirm",
        Action<MudToggleIconButton> configure = null,
        Action<MudExPromptDialog> dialogConfigure = null) : base(message, configure)
    {
        DialogTitle = title;
        DialogMessage = message;
        _dialogService = dialogService;
        _confirmationWord = confirmationWord;
        _helperText = helperText;
        if (dialogConfigure != null)
            _dialogParameters = PropertyHelper.ValuesDictionary(dialogConfigure, true).ToDialogParameters();
    }

    /// <inheritdoc />
    protected override void OnButtonSet(MudToggleIconButton cb)
    {
        base.OnButtonSet(cb);
        Button.ToggledChanged = EventCallback.Factory.Create<bool>(new object(), async b =>
        {
            if(!b) // User wants to lock again so no dialog needed
                Button.Toggled = false;
            else
                Button.Toggled = await ShowConfirmationDialog();
            ConfirmationCallback?.Invoke(Button.Toggled);
        });
    }
 
    private Task<bool> ShowConfirmationDialog() 
        => !string.IsNullOrWhiteSpace(_confirmationWord) ? Prompt() : MessageBox();
    
    private async Task<bool> MessageBox()
    {
        var parameters = new DialogParameters
        {            
            { nameof(MudExMessageDialog.Buttons), MudExDialogResultAction.OkCancel(OkText, CancelText) },
            { nameof(MudExMessageDialog.Icon), Button.ToggledIcon }
        };        
        return await _dialogService.ShowConfirmationDialogAsync(DialogTitle, _dialogParameters.MergeWith(parameters), DialogOptions );        
    }

    private async Task<bool> Prompt()
    {
        var parameters = new DialogParameters
        {            
            {nameof(MudExPromptDialog.CancelText), CancelText},
            {nameof(MudExPromptDialog.HelperText), string.Format(_helperText, _confirmationWord)},
            {nameof(MudExPromptDialog.OkText), OkText}            
        };
        var res = await _dialogService.PromptAsync(DialogTitle, DialogMessage, Button.ToggledIcon, s => s == _confirmationWord, _dialogParameters.MergeWith(parameters), DialogOptions);
        return res == _confirmationWord;
    }

}