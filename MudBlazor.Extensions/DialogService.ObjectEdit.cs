using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions;

public static partial class DialogServiceExt
{

    /// <summary>
    /// Shows an object edit dialog as readonly for given object.
    /// </summary>
    public static Task<(bool Cancelled, TModel Result)> ShowObjectAsync<TModel>(this IDialogService dialogService,
        TModel value, string title, string icon, DialogOptionsEx options = null,
        Action<ObjectEditMeta<TModel>> metaConfig = null, DialogParameters dialogParameters = null)
    {
        var parameters = new DialogParameters
        {
            { nameof(MudExObjectEditDialog<TModel>.DialogIcon), icon }
        };

        return ShowObjectAsync(dialogService, value, title, options, metaConfig,
            dialogParameters == null ? parameters : dialogParameters.MergeWith(parameters));
    }

    /// <summary>
    /// Shows an object edit dialog as readonly for given object.
    /// </summary>
    /// <typeparam name="TModel">The type of the object to edit.</typeparam>
    /// <param name="dialogService">The dialog service to use.</param>
    /// <param name="value">The object to edit.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="options">The dialog options.</param>
    /// <param name="metaConfig">The configuration of meta information.</param>
    /// <param name="dialogParameters">The dialog parameters.</param>
    /// <returns>A tuple indicating if edit was cancelled and the result. </returns>
    public static async Task<(bool Cancelled, TModel Result)> ShowObjectAsync<TModel>(this IDialogService dialogService,
        TModel value, string title, DialogOptionsEx options = null, Action<ObjectEditMeta<TModel>> metaConfig = null,
        DialogParameters dialogParameters = null)
    {
        var parameters = new DialogParameters
        {
            {
                nameof(MudExObjectEditDialog<TModel>.GlobalResetSettings),
                new GlobalResetSettings() { AllowReset = false }
            },
            {
                nameof(MudExObjectEditDialog<TModel>.DefaultPropertyResetSettings),
                new PropertyResetSettings() { AllowReset = false }
            },
            { nameof(MudExObjectEditDialog<TModel>.ShowSaveButton), false },
            { nameof(MudExObjectEditDialog<TModel>.CancelButtonText), "Close" },
        };
        return await dialogService.EditObjectAsync(value, title, options, meta =>
        {
            metaConfig?.Invoke(meta);
            meta.Properties().AsReadOnly();
        }, dialogParameters.MergeWith(parameters));
    }

    /// <summary>
    /// Shows an object edit dialog as readonly for given data string as object edit.
    /// </summary>
    public static Task<(bool Cancelled, IStructuredDataObject Result)> ShowStructuredDataStringAsync(
        this IDialogService dialogService,
        string value, string title,
        DialogOptionsEx options,
        Action<ObjectEditMeta<IStructuredDataObject>> metaConfig = null,
        DialogParameters dialogParameters = null)
    {
        var model = ReflectionHelper.CreateTypeAndDeserialize(value);
        metaConfig =
            (metaConfig ?? (_ => { })).CombineWith(RenderDataDefaults.ColorFromStringOptions<IStructuredDataObject>());
        return dialogService.ShowObjectAsync(model, title, options, metaConfig, dialogParameters);
    }

    /// <summary>
    /// Shows an object edit dialog as readonly for given data string as object edit.
    /// </summary>
    public static Task<(bool Cancelled, IStructuredDataObject Result)> ShowStructuredDataStringAsync(
        this IDialogService dialogService, StructuredDataType dataType,
        string value, string title,
        DialogOptionsEx options,
        Action<ObjectEditMeta<IStructuredDataObject>> metaConfig = null,
        DialogParameters dialogParameters = null)
    {
        var model = ReflectionHelper.CreateTypeAndDeserialize(value, dataType);
        metaConfig = (metaConfig ?? (_ => { })).CombineWith(RenderDataDefaults.ColorFromStringOptions<IStructuredDataObject>());
        return dialogService.ShowObjectAsync(model, title, options, metaConfig, dialogParameters);
    }

    /// <summary>
    /// Shows an object edit dialog for given data string as object edit.
    /// </summary>
    public static Task<(bool Cancelled, IStructuredDataObject Result)> EditStructuredDataStringAsync(
        this IDialogService dialogService,
        string value, string title,
        Func<IStructuredDataObject, MudExObjectEditDialog<IStructuredDataObject>, Task<string>> customSubmit,
        DialogOptionsEx options, Action<ObjectEditMeta<IStructuredDataObject>> metaConfig = null,
        DialogParameters dialogParameters = null)
    {
        var model = ReflectionHelper.CreateTypeAndDeserialize(value);
        metaConfig =
            (metaConfig ?? (_ => { })).CombineWith(RenderDataDefaults.ColorFromStringOptions<IStructuredDataObject>());
        return dialogService.EditObjectAsync(model, title, customSubmit, options, metaConfig, dialogParameters);
    }

    /// <summary>
    /// Shows an object edit dialog for given data string as object edit.
    /// </summary>
    public static Task<(bool Cancelled, IStructuredDataObject Result)> EditStructuredDataStringAsync(
        this IDialogService dialogService,
        string value, string title,
        DialogOptionsEx options,
        Action<ObjectEditMeta<IStructuredDataObject>>
            metaConfig = null,
        DialogParameters dialogParameters = null)
    {
        var model = ReflectionHelper.CreateTypeAndDeserialize(value);
        metaConfig =
            (metaConfig ?? (_ => { })).CombineWith(RenderDataDefaults.ColorFromStringOptions<IStructuredDataObject>());
        return dialogService.EditObjectAsync(model, title, options, metaConfig, dialogParameters);
    }

    /// <summary>
    /// Shows an object edit dialog for given data string as object edit.
    /// </summary>
    public static Task<(bool Cancelled, IStructuredDataObject Result)> EditStructuredDataStringAsync(
        this IDialogService dialogService, StructuredDataType dataType,
        string value, string title,
        Func<IStructuredDataObject, MudExObjectEditDialog<IStructuredDataObject>, Task<string>> customSubmit,
        DialogOptionsEx options, Action<ObjectEditMeta<IStructuredDataObject>> metaConfig = null,
        DialogParameters dialogParameters = null)
    {
        var model = ReflectionHelper.CreateTypeAndDeserialize(value, dataType);
        metaConfig =
            (metaConfig ?? (_ => { })).CombineWith(RenderDataDefaults.ColorFromStringOptions<IStructuredDataObject>());
        return dialogService.EditObjectAsync(model, title, customSubmit, options, metaConfig, dialogParameters);
    }

    /// <summary>
    /// Shows an object edit dialog for given data string as object edit.
    /// </summary>
    public static Task<(bool Cancelled, IStructuredDataObject Result)> EditStructuredDataStringAsync(
        this IDialogService dialogService, StructuredDataType dataType,
        string value, string title,
        DialogOptionsEx options,
        Action<ObjectEditMeta<IStructuredDataObject>>
            metaConfig = null,
        DialogParameters dialogParameters = null)
    {
        var model = ReflectionHelper.CreateTypeAndDeserialize(value, dataType);
        metaConfig =
            (metaConfig ?? (_ => { })).CombineWith(RenderDataDefaults.ColorFromStringOptions<IStructuredDataObject>());
        return dialogService.EditObjectAsync(model, title, options, metaConfig, dialogParameters);
    }


    /// <summary>
    /// Shows an object edit dialog for the given object.
    /// </summary>
    /// <typeparam name="TModel">The type of the object to edit.</typeparam>
    /// <param name="dialogService">The dialog service to use.</param>
    /// <param name="value">The object to edit.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="customSubmit">Custom submit</param>
    /// <param name="options">The dialog options.</param>
    /// <param name="metaConfig">The configuration of meta information.</param>
    /// <param name="dialogParameters">The dialog parameters.</param>
    /// <returns>A tuple indicating if edit was cancelled and the result.</returns>
    public static async Task<(bool Cancelled, TModel Result)> EditObjectAsync<TModel>(this IDialogService dialogService,
        TModel value, string title, Func<TModel, MudExObjectEditDialog<TModel>, Task<string>> customSubmit,
        DialogOptionsEx options, Action<ObjectEditMeta<TModel>> metaConfig = null,
        DialogParameters dialogParameters = null)
    {
        var parameters = new DialogParameters
        {
            { nameof(MudExObjectEditDialog<TModel>.CustomSubmit), customSubmit }
        };
        return await dialogService.EditObjectAsync(value, title, options, metaConfig, dialogParameters.MergeWith(parameters));
    }

    public static async Task<(bool Cancelled, TModel Result)> EditObjectAsync<TModel>(this IDialogService dialogService,
        TModel value)
    {
        return await dialogService.EditObjectAsync(value, $"Edit {typeof(TModel).Name}", DialogOptionsEx.DefaultDialogOptions);
    }

    public static Task<(bool Cancelled, TModel Result)> EditObjectAsync<TModel>(this IDialogService dialogService,
        TModel value, string title, string icon, DialogOptionsEx options, Action<ObjectEditMeta<TModel>> metaConfig = null,
        DialogParameters dialogParameters = null)
    {
        dialogParameters ??= new DialogParameters();
        dialogParameters.Add(nameof(MudExObjectEditDialog<TModel>.DialogIcon), icon);
        return EditObjectAsync<TModel>(dialogService, value, title, options, metaConfig, dialogParameters);
    }

    /// <summary>
    /// Shows an object edit dialog for the given object.
    /// </summary>
    /// <typeparam name="TModel">The type of the object to edit.</typeparam>
    /// <param name="dialogService">The dialog service to use.</param>
    /// <param name="value">The object to edit.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="options">The dialog options.</param>
    /// <param name="metaConfig">The configuration of meta information.</param>
    /// <param name="dialogParameters">The dialog parameters.</param>
    /// <returns>A tuple indicating if edit was cancelled and the result.</returns>
    public static async Task<(bool Cancelled, TModel Result)> EditObjectAsync<TModel>(this IDialogService dialogService, TModel value, string title, DialogOptionsEx options, Action<ObjectEditMeta<TModel>> metaConfig = null, DialogParameters dialogParameters = null)
    {
        if (MudExObjectEdit<TModel>.IsPrimitive())
        {
            #region Not important but here we change some options for primitives

            var attributesForPrimitive = new Dictionary<string, object>
            {
                // All registered Primitive editors that are pickers we render PickerVariant.Static to avoid extra dialogs
                {nameof(MudPicker<object>.PickerVariant), PickerVariant.Static}
            };

            var modelForPrimitive = new ModelForPrimitive<TModel>(value);
            options.Resizeable = true;

            #endregion
            
            var r = await dialogService.EditObjectAsync(modelForPrimitive, title, options, meta =>
            {
                meta.Property(m => m.Value).WithAdditionalAttributes(attributesForPrimitive, true);
            }, dialogParameters);

            return (r.Cancelled, r.Result.Value);
        }

        var parameters = new DialogParameters
            {
                {nameof(MudExObjectEditDialog<TModel>.Value), value},
                {nameof(MudExObjectEditDialog<TModel>.ConfigureMetaInformationAlways), true},
                {nameof(MudExObjectEditDialog<TModel>.MetaInformation), value.ObjectEditMeta(metaConfig)}
            };

        var dialog = await dialogService.ShowExAsync<MudExObjectEditDialog<TModel>>(title, dialogParameters.MergeWith(parameters), options);

        var res = await dialog.Result;
        return (res?.Canceled ?? false, res?.Canceled == true ? value : (TModel)res.Data);
    }

    /// <summary>
    /// Displays a dialog that allows the user to edit a collection of items.
    /// </summary>
    /// <typeparam name="T">The type of items to edit.</typeparam>
    /// <param name="dialogService">The dialog service instance used to display the dialog.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="headerText">The header text displayed in the dialog.</param>
    /// <param name="itemsLoadFunc">
    /// A function that asynchronously loads the items to be edited.
    /// </param>
    /// <param name="allowAddAndRemove">
    /// Indicates whether the dialog allows adding and removing items.
    /// Defaults to <c>true</c>.
    /// </param>
    /// <param name="canCancel">
    /// Specifies whether canceling the dialog is allowed.
    /// If <c>false</c>, the close button will be disabled.
    /// </param>
    /// <param name="validResult">
    /// A function used to validate the edited items.
    /// The dialog is confirmed only if this function returns <c>true</c>.
    /// If not provided, a default validation is applied (always valid).
    /// </param>
    /// <param name="itemToStringFunc">
    /// A function to convert each item into its string representation.
    /// If not provided, the item's <c>ToString()</c> method is used.
    /// </param>
    /// <param name="options">
    /// Optional dialog options to customize the behavior and appearance of the dialog.
    /// </param>
    /// <param name="dialogParameters">
    /// An optional action to configure additional parameters for the dialog.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The result contains the edited array of items if the dialog was confirmed,
    /// or the original items if the dialog was canceled.
    /// </returns>
    public static async Task<T[]> EditItemsAsync<T>(
        this IDialogService dialogService,
        string title,
        string headerText,
        Func<Task<IEnumerable<T>>> itemsLoadFunc,
        bool allowAddAndRemove = true,
        bool canCancel = true,
        Func<ICollection<T>, bool> validResult = null,
        Func<T, string> itemToStringFunc = null,
        DialogOptionsEx options = null,
        Action<MudExMessageDialog> dialogParameters = null
    )
    {
        validResult ??= _ => true;
        itemToStringFunc ??= i => i.ToString();
        dialogParameters ??= _ => { };
        options ??= DialogOptionsEx.DefaultDialogOptions;
        if (!canCancel)
        {
            options.CloseButton = false;
            options.BackdropClick = false;
            options.CloseOnEscapeKey = false;
        }

        var items = (await itemsLoadFunc()).ToArray();
        var res = await dialogService.ShowComponentInDialogAsync<MudExCollectionEditor<T>>(
            title,
            headerText,
            ce =>
            {
                ce.ItemToStringFunc = itemToStringFunc;
                ce.Items = items;
                ce.Label = string.Empty;
                ce.AllowAdd = allowAddAndRemove;
                ce.AllowRemove = allowAddAndRemove;
            },
            dialogParameters.CombineWith(dialog =>
            {
                dialog.Icon = Icons.Material.Filled.List;
                dialog.Buttons = canCancel
                    ? MudExDialogResultAction.OkCancelWithOkCondition<MudExCollectionEditor<T>>(editor => validResult(editor.Items))
                    : MudExDialogResultAction.OkWithCondition<MudExCollectionEditor<T>>(editor => validResult(editor.Items));
            }),
            options
        );

        return !res.DialogResult.Canceled ? res.Component.Items.ToArray() : items;
    }

    /// <summary>
    /// Displays a dialog that allows the user to edit a collection of items.
    /// This overload accepts a predefined collection of items.
    /// </summary>
    /// <typeparam name="T">The type of items to edit.</typeparam>
    /// <param name="dialogService">The dialog service instance used to display the dialog.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="headerText">The header text displayed in the dialog.</param>
    /// <param name="items">
    /// The collection of items to be edited.
    /// </param>
    /// <param name="allowAddAndRemove">
    /// Indicates whether the dialog allows adding and removing items.
    /// Defaults to <c>true</c>.
    /// </param>
    /// <param name="canCancel">
    /// Specifies whether canceling the dialog is allowed.
    /// If <c>false</c>, the dialog cannot be closed without confirming a valid edit.
    /// </param>
    /// <param name="validResult">
    /// A function used to validate the edited items.
    /// The dialog is confirmed only if this function returns <c>true</c>.
    /// </param>
    /// <param name="itemToStringFunc">
    /// A function to convert each item into its string representation.
    /// If not provided, the item's <c>ToString()</c> method is used.
    /// </param>
    /// <param name="options">
    /// Optional dialog options to customize the behavior and appearance of the dialog.
    /// </param>
    /// <param name="dialogParameters">
    /// An optional action to configure additional parameters for the dialog.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The result contains the edited array of items if the dialog was confirmed,
    /// or the original items if the dialog was canceled.
    /// </returns>
    public static async Task<T[]> EditItemsAsync<T>(
        this IDialogService dialogService,
        string title,
        string headerText,
        IEnumerable<T> items,
        bool allowAddAndRemove = true,
        bool canCancel = true,
        Func<ICollection<T>, bool> validResult = null,
        Func<T, string> itemToStringFunc = null,
        DialogOptionsEx options = null,
        Action<MudExMessageDialog> dialogParameters = null
    )
    {
        return await dialogService.EditItemsAsync(
            title,
            headerText,
            () => Task.FromResult(items),
            allowAddAndRemove,
            canCancel,
            validResult,
            itemToStringFunc,
            options,
            dialogParameters);
    }

}