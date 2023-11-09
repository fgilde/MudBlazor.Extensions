using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Options;
using Nextended.Blazor.Helper;

namespace MudBlazor.Extensions;

public static partial class DialogServiceExt
{
    /// <summary>
    /// Shows a component in a dialog with a title and message. Dialog parameters are passed as a new DialogParameters object instance.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to be shown.</typeparam>
    /// <param name="dialogService">The IDialogService instance.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message to be shown in the dialog.</param>
    /// <param name="componentOptions">The action that configures the component.</param>
    /// <returns>A Task whose result is a value tuple of DialogResult and TComponent representing the result of the operation.</returns>

    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        Action<TComponent> componentOptions) where TComponent : ComponentBase, new()
    {
        return dialogService.ShowComponentInDialogAsync(title, message, componentOptions, new DialogParameters(), _ => {});
    }

    /// <summary>
    /// Shows a component in a dialog with a title, message, and icon. Dialog parameters are passed as a new DialogParameters object instance.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to be shown.</typeparam>
    /// <param name="dialogService">The IDialogService instance.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message to be shown in the dialog.</param>
    /// <param name="icon">The icon to be shown in the dialog.</param>
    /// <param name="componentOptions">The action that configures the component.</param>
    /// <returns>A Task whose result is a value tuple of DialogResult and TComponent representing the result of the operation.</returns>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message, string icon,
        Action<TComponent> componentOptions) where TComponent : ComponentBase, new()
    {
        var dialogParameters = new DialogParameters {{nameof(MudExMessageDialog.Icon), icon}};
        return dialogService.ShowComponentInDialogAsync(title, message, componentOptions, dialogParameters, _ => { });
    }

    /// <summary>
    /// Shows a component in a dialog with a title. Dialog parameters are passed as a new DialogParameters object instance.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to be shown.</typeparam>
    /// <param name="dialogService">The IDialogService instance.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="componentOptions">The action that configures the component.</param>
    /// <returns>A Task whose result is a value tuple of DialogResult and TComponent representing the result of the operation.</returns>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title,
        Action<TComponent> componentOptions) where TComponent : ComponentBase, new()
    {
        return dialogService.ShowComponentInDialogAsync(title, null, componentOptions, new DialogParameters(), _ => { });
    }

    /// <summary>
    /// Shows a component in a dialog with a title. Dialog parameters are passed as a new DialogParameters object instance.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to be shown.</typeparam>
    /// <returns>A Task whose result is a value tuple of DialogResult and TComponent representing the result of the operation.</returns>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title,
        string message, string icon) where TComponent : ComponentBase, new()
    {
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, _ => { }, dialog => {dialog.Icon = icon;});
    }

    /// <summary>
    /// Shows a component in a dialog with a title. Dialog parameters are passed as a new DialogParameters object instance.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to be shown.</typeparam>
    /// <returns>A Task whose result is a value tuple of DialogResult and TComponent representing the result of the operation.</returns>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title,
        string message, string icon, DialogOptionsEx options) where TComponent : ComponentBase, new()
    {
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, _ => { }, dialog => { dialog.Icon = icon; }, options);
    }

    /// <summary>
    /// Shows a component in a dialog with a title. Dialog parameters are passed as a new DialogParameters object instance.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to be shown.</typeparam>
    /// <returns>A Task whose result is a value tuple of DialogResult and TComponent representing the result of the operation.</returns>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title,
        string message) where TComponent : ComponentBase, new()
    {
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, _ => { });
    }

    /// <summary>
    /// Shows a component in a dialog with a title. Dialog parameters are passed as a new DialogParameters object instance.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to be shown.</typeparam>
    /// <returns>A Task whose result is a value tuple of DialogResult and TComponent representing the result of the operation.</returns>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title,
        string message, DialogOptionsEx options) where TComponent : ComponentBase, new()
    {
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, _ => { }, options);
    }

    /// <summary>
    /// Shows a component in a dialog with a title. Dialog parameters are passed as a new DialogParameters object instance.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to be shown.</typeparam>
    /// <returns>A Task whose result is a value tuple of DialogResult and TComponent representing the result of the operation.</returns>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title) where TComponent : ComponentBase, new()
    {
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, string.Empty);
    }

    /// <summary>
    /// Shows a component in a dialog with a title. Dialog parameters are passed as a new DialogParameters object instance.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to be shown.</typeparam>
    /// <returns>A Task whose result is a value tuple of DialogResult and TComponent representing the result of the operation.</returns>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, DialogOptionsEx options) where TComponent : ComponentBase, new()
    {
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, string.Empty, options);
    }

    /// <summary>
    /// Shows a component in a dialog with a title. Dialog parameters are passed as a new DialogParameters object instance.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to be shown.</typeparam>
    /// <returns>A Task whose result is a value tuple of DialogResult and TComponent representing the result of the operation.</returns>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService) where TComponent : ComponentBase, new()
    {
        return dialogService.ShowComponentInDialogAsync<TComponent>(string.Empty);
    }

    /// <summary>
    /// Shows a component in a dialog with a title, message, component configuration action, dialog parameters object, and dialog options object.
    /// </summary>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title,
        Action<TComponent> componentOptions,
        DialogParameters dialogParameters,
        DialogOptionsEx options = null) where TComponent : ComponentBase, new()
    {
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, string.Empty, componentOptions, dialogParameters, options);
    }

    /// <summary>
    /// Shows a component in a dialog with a title, message, component configuration action, dialog parameters object, and dialog options object.
    /// </summary>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title,
        Action<TComponent> componentOptions,
        DialogOptionsEx options) where TComponent : ComponentBase, new()
    {
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, string.Empty, componentOptions, new DialogParameters(), options);
    }

    /// <summary>
    /// Shows a component in a dialog with a title, message, component configuration action, dialog parameters action and dialog options.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to be shown.</typeparam>
    /// <param name="dialogService">The IDialogService instance.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message to be shown in the dialog.</param>
    /// <param name="componentOptions">The action that configures the component.</param>
    /// <param name="dialogParameters">The action that configures the dialog parameters.</param>
    /// <param name="options">The action that configures the dialog options.</param>
    /// <returns>A Task whose result is a value tuple of DialogResult and TComponent representing the result of the operation.</returns>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        Action<TComponent> componentOptions,
        Action<MudExMessageDialog> dialogParameters,
        Action<DialogOptionsEx> options = null) where TComponent : ComponentBase, new()
    {
        
        var componentAttributes = componentOptions != null ? PropertyHelper.ValidValuesDictionary(componentOptions, true).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : new Dictionary<string, object>();
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, componentAttributes, dialogParameters, options);
    }

    /// <summary>
    /// Shows a component in a dialog with a title, message, component configuration action and dialog options action.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to be shown.</typeparam>
    /// <param name="dialogService">The IDialogService instance.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message to be shown in the dialog.</param>
    /// <param name="componentOptions">The action that configures the component.</param>
    /// <param name="options">The action that configures the dialog options.</param>
    /// <returns>A Task whose result is a value tuple of DialogResult and TComponent representing the result of the operation.</returns>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        Action<TComponent> componentOptions,
        DialogOptionsEx options) where TComponent : ComponentBase, new()
    {
        var componentAttributes = componentOptions != null ? PropertyHelper.ValidValuesDictionary(componentOptions, true).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : new Dictionary<string, object>();
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, componentAttributes, null as DialogParameters, options);
    }



    /// <summary>
    /// Shows a component in a dialog with a title, message, component configuration dictionary, dialog parameters action and dialog options action.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to be shown.</typeparam>
    /// <param name="dialogService">The IDialogService instance.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message to be shown in the dialog.</param>
    /// <param name="componentOptions">The dictionary that configures the component.</param>
    /// <param name="dialogParameters">The action that configures the dialog parameters.</param>
    /// <param name="options">The action that configures the dialog options.</param>
    /// <returns>A Task whose result is a value tuple of DialogResult and TComponent representing the result of the operation.</returns>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        IDictionary<string, object> componentOptions,
        Action<MudExMessageDialog> dialogParameters,
        DialogOptionsEx options = null) where TComponent : ComponentBase, new()
    {
        var parameters = dialogParameters != null ? PropertyHelper.ValidValuesDictionary(dialogParameters, true).ToDialogParameters() : new DialogParameters();
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, componentOptions, parameters, options);
    }



    /// <summary>
    /// Shows a component in a dialog with a title, message, component configuration action, dialog parameters object and dialog options action.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to be shown.</typeparam>
    /// <param name="dialogService">The IDialogService instance.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message to be shown in the dialog.</param>
    /// <param name="componentOptions">The action that configures the component.</param>
    /// <param name="dialogParameters">The dialog parameters to be passed.</param>
    /// <param name="options">The action that configures the dialog options.</param>
    /// <returns>A Task whose result is a value tuple of DialogResult and TComponent representing the result of the operation.</returns>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        Action<TComponent> componentOptions,
        Action<MudExMessageDialog> dialogParameters,
        DialogOptionsEx options) where TComponent : ComponentBase, new()
    {
        var parameters = dialogParameters != null ? PropertyHelper.ValidValuesDictionary(dialogParameters, true).ToDialogParameters() : new DialogParameters();
        var componentAttributes = componentOptions != null ? PropertyHelper.ValidValuesDictionary(componentOptions, true).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : new Dictionary<string, object>();
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, componentAttributes, parameters, options);
    }

    /// <summary>
    /// Shows a component in a dialog with a title, message, component configuration action, dialog parameters object, and dialog options object.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to be shown.</typeparam>
    /// <param name="dialogService">The IDialogService instance.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message to be shown in the dialog.</param>
    /// <param name="componentOptions">The action that configures the component.</param>
    /// <param name="dialogParameters">The dialog parameters to be passed.</param>
    /// <param name="options">The dialog options to be passed.</param>
    /// <returns>A Task whose result is a value tuple of DialogResult and TComponent representing the result of the operation.</returns>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        Action<TComponent> componentOptions,
        DialogParameters dialogParameters,
        Action<DialogOptionsEx> options = null) where TComponent : ComponentBase, new()
    {

        var componentAttributes = componentOptions != null ? PropertyHelper.ValidValuesDictionary(componentOptions, true).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : new Dictionary<string, object>();
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, componentAttributes, dialogParameters, options);
    }

    /// <summary>
    /// Shows a dialog with a custom component asynchronously.
    /// </summary>
    /// <param name="dialogService">Instance of IDialogService</param>
    /// <param name="title">Title of the dialog</param>
    /// <param name="message">Message of the dialog</param>
    /// <param name="componentOptions">Component-specific configuration parameters</param>
    /// <param name="dialogParameters">Parameters for the dialog</param>
    /// <param name="options">Options for the dialog</param>
    /// <returns>A Task that returns a tuple consisting of a DialogResult and the component displayed in the dialog</returns>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        Action<TComponent> componentOptions,
        DialogParameters dialogParameters,
        DialogOptionsEx options) where TComponent : ComponentBase, new()
    {

        var componentAttributes = componentOptions != null ? PropertyHelper.ValidValuesDictionary(componentOptions, true).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : new Dictionary<string, object>();
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, componentAttributes, dialogParameters, options);
    }

    /// <summary>
    /// Shows a dialog with a custom component asynchronously.
    /// </summary>
    /// <param name="dialogService">Instance of IDialogService</param>
    /// <param name="title">Title of the dialog</param>
    /// <param name="message">Message of the dialog</param>
    /// <param name="componentOptions">Component-specific configuration parameters</param>
    /// <param name="dialogParameters">Parameters for the dialog</param>
    /// <param name="options">Options for the dialog</param>
    /// <returns>A Task that returns a tuple consisting of a DialogResult and the component displayed in the dialog</returns>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        Dictionary<string, object> componentOptions,
        Action<MudExMessageDialog> dialogParameters,
        Action<DialogOptionsEx> options = null) where TComponent : ComponentBase, new()
    {
        var parameters = dialogParameters != null ? PropertyHelper.ValidValuesDictionary(dialogParameters, true).ToDialogParameters() : new DialogParameters();
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, componentOptions, parameters, options);
    }

    /// <summary>
    /// Shows a dialog with a custom component asynchronously.
    /// </summary>
    /// <param name="dialogService">Instance of IDialogService</param>
    /// <param name="title">Title of the dialog</param>
    /// <param name="message">Message of the dialog</param>
    /// <param name="componentOptions">Component-specific configuration parameters</param>
    /// <param name="dialogParameters">Parameters for the dialog</param>
    /// <param name="options">Options for the dialog</param>
    /// <returns>A Task that returns a tuple consisting of a DialogResult and the component displayed in the dialog</returns>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        Dictionary<string, object> componentOptions,
        DialogParameters dialogParameters,
        Action<DialogOptionsEx> options) where TComponent : ComponentBase, new()
    {
        var optionsEx = DefaultOptions();
        options?.Invoke(optionsEx);
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, componentOptions, dialogParameters, optionsEx);
    }

    /// <summary>
    /// Shows a dialog with a custom component asynchronously.
    /// </summary>
    /// <param name="dialogService">Instance of IDialogService</param>
    /// <param name="title">Title of the dialog</param>
    /// <param name="message">Message of the dialog</param>
    /// <param name="componentOptions">Component-specific configuration parameters</param>
    /// <param name="dialogParameters">Parameters for the dialog</param>
    /// <param name="options">Options for the dialog</param>
    /// <returns>A Task that returns a tuple consisting of a DialogResult and the component displayed in the dialog</returns>
    public static Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogOkCancelAsync<TComponent>(
        this IDialogService dialogService, string title, string message, string confirmText, string cancelText,
        Dictionary<string, object> componentOptions,
        DialogParameters dialogParameters,
        DialogOptionsEx options = null) where TComponent : ComponentBase, new()
    {
        var parameters = dialogParameters ?? new DialogParameters();
        parameters.Add(nameof(MudExMessageDialog.Buttons), MudExDialogResultAction.OkCancel(confirmText, cancelText));
        return dialogService.ShowComponentInDialogAsync<TComponent>(title, message, componentOptions, parameters, options);
    }

    /// <summary>
    /// Shows a dialog with a custom component asynchronously.
    /// </summary>
    /// <param name="dialogService">Instance of IDialogService</param>
    /// <param name="title">Title of the dialog</param>
    /// <param name="message">Message of the dialog</param>
    /// <param name="componentOptions">Component-specific configuration parameters</param>
    /// <param name="dialogParameters">Parameters for the dialog</param>
    /// <param name="options">Options for the dialog</param>
    /// <returns>A Task that returns a tuple consisting of a DialogResult and the component displayed in the dialog</returns>
    public static async Task<(DialogResult DialogResult, TComponent Component)> ShowComponentInDialogAsync<TComponent>(this IDialogService dialogService, string title, string message,
        IDictionary<string, object> componentOptions,
        DialogParameters dialogParameters,
        DialogOptionsEx options) where TComponent : ComponentBase, new()
    {
        TComponent component = null;
        var componentAttributes = componentOptions != null ? componentOptions.Where(kvp => ComponentRenderHelper.IsValidParameter(typeof(TComponent), kvp.Key, kvp.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : new Dictionary<string, object>();
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
        options ??= DefaultOptions();
        var dialog = await dialogService.ShowEx<MudExMessageDialog>(title, parameters, options);
        var mudExMessageDialog = ((MudExMessageDialog)dialog.Dialog);
        mudExMessageDialog.Component = component;
        return (await dialog.Result, component);
    }

    /// <summary>
    /// Shows a confirmation dialog asynchronously.
    /// </summary>
    public static async Task<bool> ShowConfirmationDialogAsync(this IDialogService dialogService, string title,
        DialogParameters parameters,
        DialogOptionsEx options = null)
    {
        options ??= DefaultOptions();
        var dialog = await dialogService.ShowEx<MudExMessageDialog>(title, parameters, options);

        return !(await dialog.Result).Cancelled;
    }

    /// <summary>
    /// Shows a confirmation dialog with custom MessageBoxOptions asynchronously.
    /// </summary>
    public static Task<bool> ShowConfirmationDialogAsync(this IDialogService dialogService, MessageBoxOptions messageBoxOptions, DialogOptionsEx options = null)
    {
        return dialogService.ShowConfirmationDialogAsync(messageBoxOptions, null, options);
    }

    /// <summary>
    /// Shows a confirmation dialog with custom MessageBoxOptions and an icon asynchronously.
    /// </summary>
    public static Task<bool> ShowConfirmationDialogAsync(this IDialogService dialogService, MessageBoxOptions messageBoxOptions, string icon, DialogOptionsEx options = null)
    {
        return dialogService.ShowConfirmationDialogAsync(messageBoxOptions.Title, messageBoxOptions.Message, messageBoxOptions.YesText, messageBoxOptions.CancelText, icon, options);
    }

    /// <summary>
    /// Shows a confirmation dialog with customizable title, message, confirm text, cancel text, and icon asynchronously.
    /// </summary>
    public static Task<bool> ShowConfirmationDialogAsync(this IDialogService dialogService, string title,
        string message,
        string confirmText = "Confirm",
        string cancelText = "Cancel",
        string icon = null,
        DialogOptionsEx options = null)
    {
        var actions = MudExDialogResultAction.OkCancel(confirmText, cancelText);
        var parameters = new DialogParameters
        {
            {
                nameof(MudExMessageDialog.Message), message
            },
            {nameof(MudExMessageDialog.Icon), icon ?? Icons.Material.Filled.Check},
            {nameof(MudExMessageDialog.Buttons), actions}
        };
        return ShowConfirmationDialogAsync(dialogService, title, parameters, options);
    }

    /// <summary>
    /// Shows an information dialog with customizable parameters asynchronously.
    /// </summary>
    public static async Task<IMudExDialogReference<MudExMessageDialog>> ShowInformationAsync(this IDialogService dialogService, string title,
        Action<MudExMessageDialog> parameters,
        DialogOptionsEx options = null)
    {
        options ??= DefaultOptions();
        return (await dialogService.ShowEx<MudExMessageDialog>(title, parameters, options)).AsMudExDialogReference<MudExMessageDialog>();
    }

    /// <summary>
    /// Shows an information dialog with custom DialogParameters asynchronously.
    /// </summary>
    public static async Task<IMudExDialogReference<MudExMessageDialog>> ShowInformationAsync(this IDialogService dialogService, string title,
        DialogParameters parameters,
        DialogOptionsEx options = null)
    {
        options ??= DefaultOptions();
        return (await dialogService.ShowEx<MudExMessageDialog>(title, parameters, options)).AsMudExDialogReference<MudExMessageDialog>();
    }

    /// <summary>
    /// Shows an information dialog with a custom message asynchronously.
    /// </summary>
    public static Task<IMudExDialogReference<MudExMessageDialog>> ShowInformationAsync(
        this IDialogService dialogService, string title,
        string message)
    {
        return ShowInformationAsync(dialogService, title, message, null, null);
    }

    /// <summary>
    /// Shows an information dialog with a custom message and icon asynchronously.
    /// </summary>
    public static Task<IMudExDialogReference<MudExMessageDialog>> ShowInformationAsync(this IDialogService dialogService, string title,
        string message,
        string icon,
        DialogOptionsEx options)
    {
        var actions = MudExDialogResultAction.Ok();
        var parameters = new DialogParameters
        {
            {
                nameof(MudExMessageDialog.Message), message
            },
            {nameof(MudExMessageDialog.AllowEmptyActions), true},
            {nameof(MudExMessageDialog.Icon), icon ?? Icons.Material.Filled.Check},
            {nameof(MudExMessageDialog.Buttons), actions}
        };
        options ??= DefaultOptions();
        return ShowInformationAsync(dialogService, title, parameters, options);
    }

    /// <summary>
    /// Shows an information dialog with a custom message, icon, ability to close, and show progress asynchronously.
    /// </summary>
    public static async Task<IMudExDialogReference<MudExMessageDialog>> ShowInformationAsync(this IDialogService dialogService, string title,
        string message,
        string icon,
        bool canClose = true,
        bool showProgress = false,
        DialogOptionsEx options = null)
    {
        var actions = canClose ? MudExDialogResultAction.Ok() : null;
        var parameters = new DialogParameters
        {
            {
                nameof(MudExMessageDialog.Message), message
            },
            {nameof(MudExMessageDialog.AllowEmptyActions), true},
            {nameof(MudExMessageDialog.Icon), icon ?? Icons.Material.Filled.Check},
            {nameof(MudExMessageDialog.Buttons), actions},
            {nameof(MudExMessageDialog.ShowProgress), showProgress}
        };
        options ??= DefaultOptions();
        options.CloseButton = canClose;
        options.CloseOnEscapeKey = canClose;
        options.DisableBackdropClick = !canClose;
        var reference = await dialogService.ShowEx<MudExMessageDialog>(title, parameters, options);
        return reference.AsMudExDialogReference<MudExMessageDialog>();
    }


    /// <summary>
    /// Prompts the user for input with a default empty string asynchronously.
    /// </summary>
    public static Task<string> PromptAsync(this IDialogService dialogService, string title, string message, DialogOptionsEx options)
    {
        return dialogService.PromptAsync(title, message, "", options: options);
    }

    /// <summary>
    /// Prompts the user for input with a validation function asynchronously.
    /// </summary>
    public static Task<string> PromptAsync(this IDialogService dialogService, string title, string message, Func<string, bool> canConfirm, DialogOptionsEx options = null)
    {
        return dialogService.PromptAsync(title, message, "", canConfirm: canConfirm, options: options);
    }

    /// <summary>
    /// Prompts the user for input with a validation function and custom icon asynchronously.
    /// </summary>
    public static Task<string> PromptAsync(this IDialogService dialogService, string title, string message, string icon, Func<string, bool> canConfirm, DialogOptionsEx options = null)
    {
        return dialogService.PromptAsync(title, message, "", icon: icon, canConfirm: canConfirm, options: options);
    }

    /// <summary>
    /// Prompts the user for input with customizable title, message, initialValue, buttonOkText, buttonCancelText, icon, validation function, and dialog options asynchronously.
    /// </summary>
    public static async Task<string> PromptAsync(this IDialogService dialogService, string title, string message,
        string initialValue = "",
        string buttonOkText = "Ok", 
        string buttonCancelText = "Cancel",
        string icon = null,
        Func<string, bool> canConfirm = null,
        DialogOptionsEx options = null)
    {
        canConfirm ??= (s => true);
        var parameters = new DialogParameters
        {
            {nameof(MudExPromptDialog.Message), message},
            {nameof(MudExPromptDialog.Icon), icon},
            {nameof(MudExPromptDialog.OkText), buttonOkText},
            {nameof(MudExPromptDialog.CancelText), buttonCancelText},
            {nameof(MudExPromptDialog.CanConfirm), canConfirm},
            {nameof(MudExPromptDialog.Value), initialValue}
        };
        
        options ??= DefaultOptions();
        var res = await dialogService.ShowEx<MudExPromptDialog>(title, parameters, options);
        var dialogResult = (await res.Result);

        if (!dialogResult.Canceled && dialogResult.Data != null && canConfirm(dialogResult.Data.ToString()))
            return dialogResult.Data.ToString();
        return null;
    }

}