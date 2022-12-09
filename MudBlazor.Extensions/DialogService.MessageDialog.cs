using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions;

public static partial class DialogServiceExt
{

    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        Action<TComponent> componentOptions) where TComponent : ComponentBase, new()
    {

        return dialogService.ShowComponentInDialogAsync(title, message, componentOptions, new DialogParameters(), _ => {});
    }

    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message, string icon,
        Action<TComponent> componentOptions) where TComponent : ComponentBase, new()
    {
        var dialogParameters = new DialogParameters {{nameof(MudExMessageDialog.Icon), icon}};
        return dialogService.ShowComponentInDialogAsync(title, message, componentOptions, dialogParameters, _ => { });
    }

    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title,
        Action<TComponent> componentOptions) where TComponent : ComponentBase, new()
    {
        return dialogService.ShowComponentInDialogAsync(title, null, componentOptions, new DialogParameters(), _ => { });
    }

    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        Action<TComponent> componentOptions,
        Action<MudExMessageDialog> dialogParameters,
        Action<DialogOptionsEx> options = null) where TComponent : ComponentBase, new()
    {
        
        var componentAttributes = componentOptions != null ? DictionaryHelper.GetValuesDictionary(componentOptions, true).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : new Dictionary<string, object>();
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, componentAttributes, dialogParameters, options);
    }

    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        Action<TComponent> componentOptions,
        DialogOptionsEx options) where TComponent : ComponentBase, new()
    {
        var componentAttributes = componentOptions != null ? DictionaryHelper.GetValuesDictionary(componentOptions, true).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : new Dictionary<string, object>();
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, componentAttributes, null as DialogParameters, options);
    }



    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        Dictionary<string, object> componentOptions,
        Action<MudExMessageDialog> dialogParameters,
        DialogOptionsEx options = null) where TComponent : ComponentBase, new()
    {
        var parameters = dialogParameters != null ? DictionaryHelper.GetValuesDictionary(dialogParameters, true).ToDialogParameters() : new DialogParameters();
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, componentOptions, parameters, options);
    }



    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        Action<TComponent> componentOptions,
        Action<MudExMessageDialog> dialogParameters,
        DialogOptionsEx options) where TComponent : ComponentBase, new()
    {
        var parameters = dialogParameters != null ? DictionaryHelper.GetValuesDictionary(dialogParameters, true).ToDialogParameters() : new DialogParameters();
        var componentAttributes = componentOptions != null ? DictionaryHelper.GetValuesDictionary(componentOptions, true).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : new Dictionary<string, object>();
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, componentAttributes, parameters, options);
    }

    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        Action<TComponent> componentOptions,
        DialogParameters dialogParameters,
        Action<DialogOptionsEx> options = null) where TComponent : ComponentBase, new()
    {

        var componentAttributes = componentOptions != null ? DictionaryHelper.GetValuesDictionary(componentOptions, true).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : new Dictionary<string, object>();
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, componentAttributes, dialogParameters, options);
    }

    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        Action<TComponent> componentOptions,
        DialogParameters dialogParameters,
        DialogOptionsEx options) where TComponent : ComponentBase, new()
    {

        var componentAttributes = componentOptions != null ? DictionaryHelper.GetValuesDictionary(componentOptions, true).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : new Dictionary<string, object>();
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, componentAttributes, dialogParameters, options);
    }

    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        Dictionary<string, object> componentOptions,
        Action<MudExMessageDialog> dialogParameters,
        Action<DialogOptionsEx> options = null) where TComponent : ComponentBase, new()
    {
        var parameters = dialogParameters != null ? DictionaryHelper.GetValuesDictionary(dialogParameters, true).ToDialogParameters() : new DialogParameters();
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, componentOptions, parameters, options);
    }

    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        Dictionary<string, object> componentOptions,
        DialogParameters dialogParameters,
        Action<DialogOptionsEx> options) where TComponent : ComponentBase, new()
    {
        var optionsEx = new DialogOptionsEx
        {
            DragMode = MudDialogDragMode.Simple,
            CloseButton = true,
            DisableBackdropClick = false,
            Animations = new[] { AnimationType.FadeIn, AnimationType.FlipX }
        };
        options?.Invoke(optionsEx);
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, componentOptions, dialogParameters, optionsEx);
    }

    public static async Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogOkCancelAsync<TComponent>(
        this IDialogService dialogService, string title, string message, string confirmText, string cancelText,
        Dictionary<string, object> componentOptions,
        DialogParameters dialogParameters,
        DialogOptionsEx options = null) where TComponent : ComponentBase, new()
    {
        var parameters = dialogParameters ?? new DialogParameters();
        parameters.Add(nameof(MudExMessageDialog.Buttons), MudExDialogResultAction.OkCancel(confirmText, cancelText));
        return await dialogService.ShowComponentInDialogAsync<TComponent>(title, message, componentOptions, parameters, options);
    }

    public static async Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        Dictionary<string, object> componentOptions,
        DialogParameters dialogParameters,
        DialogOptionsEx options) where TComponent : ComponentBase, new()
    {
        TComponent component = null;
        var componentAttributes = componentOptions != null ? componentOptions.Where(kvp => ComponentRenderHelper.IsValidParameterAttribute(typeof(TComponent), kvp.Key, kvp.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : new Dictionary<string, object>();
        var parameters = dialogParameters ?? new DialogParameters();
        parameters.Add(nameof(MudExMessageDialog.Message), message);
        //parameters.Add(nameof(MudExMessageDialog.CanConfirmFn), () => canConfirmFn == null || canConfirmFn(component));
        parameters.Add(nameof(MudExMessageDialog.Content), new RenderFragment(builder =>
        {
            builder.OpenComponent(0, typeof(TComponent));
            var idx = 0;
            foreach (var pair in componentAttributes)
                builder.AddAttribute(idx++, pair.Key, pair.Value);
            builder.AddComponentReferenceCapture(idx + 1, r => { component = r as TComponent; });

            builder.CloseComponent();
        }));
        options ??= new DialogOptionsEx
        {
            DragMode = MudDialogDragMode.Simple,
            CloseButton = true,
            DisableBackdropClick = false,
            Animations = new[] { AnimationType.FadeIn, AnimationType.FlipX }
        };

        var dialog = await dialogService.ShowEx<MudExMessageDialog>(title, parameters, options);
        var mudExMessageDialog = ((MudExMessageDialog)dialog.Dialog);
        mudExMessageDialog.Component = component;
        return (await dialog.Result, component);
    }


    public static async Task<bool> ShowConfirmationDialogAsync(this IDialogService dialogService, string title, string message,
        string confirmText = "Confirm",
        string cancelText = "Cancel",
        DialogOptionsEx options = null)
    {
        var actions = MudExDialogResultAction.OkCancel(confirmText, cancelText);
        var parameters = new DialogParameters
            {
                {
                    nameof(MudExMessageDialog.Message), message
                },
                {nameof(MudExMessageDialog.Icon), Icons.Filled.Check},
                {nameof(MudExMessageDialog.Buttons), actions}
            };
        options ??= new DialogOptionsEx
        {
            CloseButton = true,
            DisableBackdropClick = false,
            Animations = new[] { AnimationType.FadeIn, AnimationType.FlipX }
        };
        var dialog = await dialogService.ShowEx<MudExMessageDialog>(title, parameters, options);

        return !(await dialog.Result).Cancelled;
    }

}