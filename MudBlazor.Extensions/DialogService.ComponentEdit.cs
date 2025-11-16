using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions;

public static partial class DialogServiceExt
{


    ///// <summary>
    ///// Shows an object edit dialog as readonly for given object.
    ///// </summary>
    //public static async Task EditComponentAsync<TCmp>(this IDialogService dialogService,
    //    TCmp value,
    //    string title = "",
    //    string icon = "",
    //    DialogOptionsEx options = null,
    //    Action<ObjectEditMeta<TCmp>> metaConfig = null, DialogParameters dialogParameters = null)
    ////where TCmp : IComponent
    //{
    //    var parameters = new DialogParameters
    //    {
    //        { nameof(MudExComponentEditDialog<TCmp>.DialogIcon), icon }
    //    };

    //    await dialogService.ShowComponentInDialogAsync<MudExComponentPropertyGrid<TCmp>>(title, "msg", grid =>
    //    {
    //        grid.ShowInherited = true;
    //        grid.Value = value;
    //        //grid.MetaConfiguration = metaConfig;
    //    }, dialogParameters == null ? parameters : dialogParameters.MergeWith(parameters), options);

    //}

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
    public static async Task<(bool Cancelled, TModel Result)> EditComponentAsync<TModel>(this IDialogService dialogService,
        TModel value, string title, Func<TModel, MudExComponentEditDialog<TModel>, Task<string>> customSubmit,
        DialogOptionsEx options, Action<ObjectEditMeta<TModel>> metaConfig = null,
        DialogParameters dialogParameters = null)
    {
        var parameters = new DialogParameters
        {
            { nameof(MudExComponentEditDialog<TModel>.CustomSubmit), customSubmit }
        };
        return await dialogService.EditComponentAsync(value, title, options, metaConfig, dialogParameters.MergeWith(parameters));
    }

    public static async Task<(bool Cancelled, TModel Result)> EditComponentAsync<TModel>(this IDialogService dialogService,
        TModel value, DialogOptionsEx options)
    {
        return await dialogService.EditComponentAsync(value, $"Edit {value.GetType().Name}", options);
    }

    public static async Task<(bool Cancelled, TModel Result)> EditComponentAsync<TModel>(this IDialogService dialogService,
        TModel value)
    {
        return await dialogService.EditComponentAsync(value, $"Edit {value.GetType().Name}", DialogOptionsEx.DefaultDialogOptions);
    }

    public static Task<(bool Cancelled, TModel Result)> EditComponentAsync<TModel>(this IDialogService dialogService,
        TModel value, string title, string icon, DialogOptionsEx options, Action<ObjectEditMeta<TModel>> metaConfig = null,
        DialogParameters dialogParameters = null)
    {
        dialogParameters ??= new DialogParameters();
        dialogParameters.Add(nameof(MudExComponentEditDialog<TModel>.DialogIcon), icon);
        return EditComponentAsync<TModel>(dialogService, value, title, options, metaConfig, dialogParameters);
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
    public static async Task<(bool Cancelled, TModel Result)> EditComponentAsync<TModel>(this IDialogService dialogService, TModel value, string title, DialogOptionsEx options, Action<ObjectEditMeta<TModel>> metaConfig = null, DialogParameters dialogParameters = null)
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
            
            var r = await dialogService.EditComponentAsync(modelForPrimitive, title, options, meta =>
            {
                meta.Property(m => m.Value).WithAdditionalAttributes(attributesForPrimitive, true);
            }, dialogParameters);

            return (r.Cancelled, r.Result.Value);
        }

        var parameters = new DialogParameters
            {
                {nameof(MudExComponentEditDialog<TModel>.ShowInherited), true},
                {nameof(MudExComponentEditDialog<TModel>.Value), value},
                {nameof(MudExComponentEditDialog<TModel>.ConfigureMetaInformationAlways), true},
                {nameof(MudExComponentEditDialog<TModel>.MetaInformation), value.ObjectEditMeta(metaConfig)}
            };

        var dialog = await dialogService.ShowExAsync<MudExComponentEditDialog<TModel>>(title, dialogParameters.MergeWith(parameters), options);

        var res = await dialog.Result;
        return (res?.Canceled ?? false, res?.Canceled == true ? value : (TModel)res.Data);
    }


}