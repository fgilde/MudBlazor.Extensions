using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Options;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Extensions.Components;
using System.Net.Mime;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Components.ObjectEdit;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions;

/// <summary>
/// Contains extensions for the IDialogService for displaying a file in different formats.
/// </summary>
public static partial class DialogServiceExt
{
    [Obsolete("Use ShowExAsync instead.")]
    public static Task<IMudExDialogReference<TDialog>> ShowEx<TDialog>(this IDialogService dialogService, string title, Action<TDialog> dialogParameters, Action<DialogOptionsEx> optionsEx)
        where TDialog : ComponentBase, new()
    => ShowExAsync(dialogService, title, dialogParameters, optionsEx);

    [Obsolete("Use ShowExAsync instead.")]
    public static Task<IMudExDialogReference<TDialog>> ShowEx<TDialog>(this IDialogService dialogService, string title, TDialog dialogParameters, Action<DialogOptionsEx> optionsEx)
        where TDialog : ComponentBase, new()
    => ShowExAsync(dialogService, title, dialogParameters, optionsEx);

    [Obsolete("Use ShowExAsync instead.")]
    public static Task<IMudExDialogReference<TDialog>> ShowEx<TDialog>(this IDialogService dialogService, string title,
        TDialog dialogParameters, DialogOptionsEx optionsEx = null) where TDialog : ComponentBase, new()
        => ShowExAsync(dialogService, title, dialogParameters, optionsEx);

    [Obsolete("Use ShowExAsync instead.")]
    public static Task<IMudExDialogReference<TDialog>> ShowEx<TDialog>(this IDialogService dialogService, string title, Action<TDialog> dialogParameters, DialogOptionsEx optionsEx = null) where TDialog : ComponentBase, new()
        => ShowExAsync(dialogService, title, dialogParameters, optionsEx);

    [Obsolete("Use ShowExAsync instead.")]
    public static Task<IMudExDialogReference<T>> ShowEx<T>(this IDialogService dialogService, string title, DialogParameters parameters, DialogOptionsEx options = null) where T : ComponentBase
        => ShowExAsync<T>(dialogService, title, parameters, options);

    [Obsolete("Use ShowExAsync instead.")]
    public static Task<IMudExDialogReference<T>> ShowEx<T>(this IDialogService dialogService, string title, DialogOptionsEx options = null) where T : ComponentBase
        => ShowExAsync<T>(dialogService, title, options);

    [Obsolete("Use ShowExAsync instead.")]
    public static Task<IDialogReference> ShowEx(this IDialogService dialogService, Type type, string title, DialogParameters parameters, DialogOptionsEx options = null)
        => ShowExAsync(dialogService, type, title, parameters, options);

    [Obsolete("Use ShowMessageBoxExAsync instead.")]
    public static Task<bool?> ShowMessageBoxEx(this IDialogService dialogService, MessageBoxOptions mboxOptions, DialogOptionsEx options = null)
    => ShowMessageBoxExAsync(dialogService, mboxOptions, options);

    [Obsolete("Use ShowExAsync instead.")]
    public static Task<IDialogReference> ShowEx(this IDialogService dialogService, Type type, string title, DialogOptionsEx options = null)
        => ShowExAsync(dialogService, type, title, options);



    [Obsolete("Use ShowFileDisplayDialogAsync instead.")]
    public static Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, string url, DialogOptionsEx options = null, DialogParameters dialogParameters = null)
        => ShowFileDisplayDialogAsync(dialogService, url, options, dialogParameters);

    [Obsolete("Use ShowFileDisplayDialogAsync instead.")]
    public static Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, string url, string contentType, DialogOptionsEx options, DialogParameters dialogParameters = null)
        => ShowFileDisplayDialogAsync(dialogService, url, contentType, options, dialogParameters);

    [Obsolete("Use ShowFileDisplayDialogAsync instead.")]
    public static Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, string url, string fileName, ContentType contentType, DialogOptionsEx options, DialogParameters dialogParameters = null)
        => ShowFileDisplayDialogAsync(dialogService, url, fileName, contentType, options, dialogParameters);

    [Obsolete("Use ShowFileDisplayDialogAsync instead.")]
    public static Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, string url, string fileName, string contentType, Func<IMudExFileDisplayInfos, Task<MudExFileDisplayContentErrorResult>> handleContentErrorFunc, Action<DialogOptionsEx> options = null)
        => ShowFileDisplayDialogAsync(dialogService, url, fileName, contentType, handleContentErrorFunc, options);

    [Obsolete("Use ShowFileDisplayDialogAsync instead.")]
    public static Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, IBrowserFile browserFile, Func<IMudExFileDisplayInfos, Task<MudExFileDisplayContentErrorResult>> handleContentErrorFunc, Action<DialogOptionsEx> options = null)
        => ShowFileDisplayDialogAsync(dialogService, browserFile, handleContentErrorFunc, options);

    [Obsolete("Use ShowFileDisplayDialogAsync instead.")]
    public static Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, Stream stream, string fileName, string contentType, Func<IMudExFileDisplayInfos, Task<MudExFileDisplayContentErrorResult>> handleContentErrorFunc, Action<DialogOptionsEx> options = null, DialogParameters parameters = null)
        => ShowFileDisplayDialogAsync(dialogService, stream, fileName, contentType, handleContentErrorFunc, options, parameters);

    [Obsolete("Use ShowFileDisplayDialogAsync instead.")]
    public static Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, string url, string fileName, string contentType, Action<DialogOptionsEx> options = null, DialogParameters dialogParameters = null)
        => ShowFileDisplayDialogAsync(dialogService, url, fileName, contentType, options, dialogParameters);

    [Obsolete("Use ShowFileDisplayDialogAsync instead.")]
    public static Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, IBrowserFile browserFile, Action<DialogOptionsEx> options = null, DialogParameters dialogParameters = null)
        => ShowFileDisplayDialogAsync(dialogService, browserFile, options, dialogParameters);

    [Obsolete("Use ShowFileDisplayDialogAsync instead.")]
    public static Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, Stream stream, string fileName, string contentType, Action<DialogOptionsEx> options = null, DialogParameters dialogParameters = null)
        => ShowFileDisplayDialogAsync(dialogService, stream, fileName, contentType, options, dialogParameters);


    [Obsolete("Use ShowObjectAsync instead.")]
    public static Task<(bool Cancelled, TModel Result)> ShowObject<TModel>(
        this IDialogService dialogService,
        TModel value,
        string title,
        string icon,
        DialogOptionsEx options = null,
        Action<ObjectEditMeta<TModel>> metaConfig = null,
        DialogParameters dialogParameters = null)
        => ShowObjectAsync(dialogService, value, title, icon, options, metaConfig, dialogParameters);

    [Obsolete("Use ShowObjectAsync instead.")]
    public static Task<(bool Cancelled, TModel Result)> ShowObject<TModel>(
        this IDialogService dialogService,
        TModel value,
        string title,
        DialogOptionsEx options = null,
        Action<ObjectEditMeta<TModel>> metaConfig = null,
        DialogParameters dialogParameters = null)
        => ShowObjectAsync(dialogService, value, title, options, metaConfig, dialogParameters);

    [Obsolete("Use ShowStructuredDataStringAsync instead.")]
    public static Task<(bool Cancelled, IStructuredDataObject Result)> ShowStructuredDataString(
        this IDialogService dialogService,
        string value,
        string title,
        DialogOptionsEx options,
        Action<ObjectEditMeta<IStructuredDataObject>> metaConfig = null,
        DialogParameters dialogParameters = null)
        => ShowStructuredDataStringAsync(dialogService, value, title, options, metaConfig, dialogParameters);

    [Obsolete("Use ShowStructuredDataStringAsync instead.")]
    public static Task<(bool Cancelled, IStructuredDataObject Result)> ShowStructuredDataString(
        this IDialogService dialogService,
        StructuredDataType dataType,
        string value,
        string title,
        DialogOptionsEx options,
        Action<ObjectEditMeta<IStructuredDataObject>> metaConfig = null,
        DialogParameters dialogParameters = null)
        => ShowStructuredDataStringAsync(dialogService, dataType, value, title, options, metaConfig, dialogParameters);

    [Obsolete("Use EditStructuredDataStringAsync instead.")]
    public static Task<(bool Cancelled, IStructuredDataObject Result)> EditStructuredDataString(
        this IDialogService dialogService,
        string value,
        string title,
        Func<IStructuredDataObject, MudExObjectEditDialog<IStructuredDataObject>, Task<string>> customSubmit,
        DialogOptionsEx options,
        Action<ObjectEditMeta<IStructuredDataObject>> metaConfig = null,
        DialogParameters dialogParameters = null)
        => EditStructuredDataStringAsync(dialogService, value, title, customSubmit, options, metaConfig, dialogParameters);

    [Obsolete("Use EditStructuredDataStringAsync instead.")]
    public static Task<(bool Cancelled, IStructuredDataObject Result)> EditStructuredDataString(
        this IDialogService dialogService,
        string value,
        string title,
        DialogOptionsEx options,
        Action<ObjectEditMeta<IStructuredDataObject>> metaConfig = null,
        DialogParameters dialogParameters = null)
        => EditStructuredDataStringAsync(dialogService, value, title, options, metaConfig, dialogParameters);

    [Obsolete("Use EditStructuredDataStringAsync instead.")]
    public static Task<(bool Cancelled, IStructuredDataObject Result)> EditStructuredDataString(
        this IDialogService dialogService,
        StructuredDataType dataType,
        string value,
        string title,
        Func<IStructuredDataObject, MudExObjectEditDialog<IStructuredDataObject>, Task<string>> customSubmit,
        DialogOptionsEx options,
        Action<ObjectEditMeta<IStructuredDataObject>> metaConfig = null,
        DialogParameters dialogParameters = null)
        => EditStructuredDataStringAsync(dialogService, dataType, value, title, customSubmit, options, metaConfig, dialogParameters);

    [Obsolete("Use EditStructuredDataStringAsync instead.")]
    public static Task<(bool Cancelled, IStructuredDataObject Result)> EditStructuredDataString(
        this IDialogService dialogService,
        StructuredDataType dataType,
        string value,
        string title,
        DialogOptionsEx options,
        Action<ObjectEditMeta<IStructuredDataObject>> metaConfig = null,
        DialogParameters dialogParameters = null)
        => EditStructuredDataStringAsync(dialogService, dataType, value, title, options, metaConfig, dialogParameters);

    [Obsolete("Use EditObjectAsync instead.")]
    public static Task<(bool Cancelled, TModel Result)> EditObject<TModel>(
        this IDialogService dialogService,
        TModel value,
        string title,
        Func<TModel, MudExObjectEditDialog<TModel>, Task<string>> customSubmit,
        DialogOptionsEx options,
        Action<ObjectEditMeta<TModel>> metaConfig = null,
        DialogParameters dialogParameters = null)
        => EditObjectAsync(dialogService, value, title, customSubmit, options, metaConfig, dialogParameters);

    [Obsolete("Use EditObjectAsync instead.")]
    public static Task<(bool Cancelled, TModel Result)> EditObject<TModel>(
        this IDialogService dialogService,
        TModel value)
        => EditObjectAsync(dialogService, value);

    [Obsolete("Use EditObjectAsync instead.")]
    public static Task<(bool Cancelled, TModel Result)> EditObject<TModel>(
        this IDialogService dialogService,
        TModel value,
        string title,
        string icon,
        DialogOptionsEx options,
        Action<ObjectEditMeta<TModel>> metaConfig = null,
        DialogParameters dialogParameters = null)
        => EditObjectAsync(dialogService, value, title, icon, options, metaConfig, dialogParameters);

    [Obsolete("Use EditObjectAsync instead.")]
    public static Task<(bool Cancelled, TModel Result)> EditObject<TModel>(
        this IDialogService dialogService,
        TModel value,
        string title,
        DialogOptionsEx options,
        Action<ObjectEditMeta<TModel>> metaConfig = null,
        DialogParameters dialogParameters = null)
        => EditObjectAsync(dialogService, value, title, options, metaConfig, dialogParameters);


}