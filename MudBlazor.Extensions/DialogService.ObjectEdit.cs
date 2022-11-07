using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions;

public static partial class DialogServiceExt
{

    public static async Task<(bool Cancelled, TModel Result)> ShowObject<TModel>(this IDialogService dialogService, TModel value, string title, DialogOptionsEx options, Action<ObjectEditMeta<TModel>> metaConfig = null, DialogParameters dialogParameters = null)
    {
        var parameters = new DialogParameters
            {
                {nameof(MudExObjectEditDialog<TModel>.ShowSaveButton), false},
                {nameof(MudExObjectEditDialog<TModel>.CancelButtonText), "Close"},
            };
        return await dialogService.EditObject(value, title, options, meta =>
        {
            metaConfig?.Invoke(meta);
            meta.Properties().AsReadOnly();
        }, MergeParameters(dialogParameters, parameters));
    }

    public static async Task<(bool Cancelled, TModel Result)> EditObject<TModel>(this IDialogService dialogService,
        TModel value, string title, Func<TModel, MudExObjectEditDialog<TModel>, Task<string>> customSubmit, DialogOptionsEx options, Action<ObjectEditMeta<TModel>> metaConfig = null,
        DialogParameters dialogParameters = null)
    {
        var parameters = new DialogParameters
            {
                {nameof(MudExObjectEditDialog<TModel>.CustomSubmit), customSubmit}
            };
        return await dialogService.EditObject(value, title, options, metaConfig, MergeParameters(dialogParameters, parameters));
    }

    public static async Task<(bool Cancelled, TModel Result)> EditObject<TModel>(this IDialogService dialogService, TModel value, string title, DialogOptionsEx options, Action<ObjectEditMeta<TModel>> metaConfig = null, DialogParameters dialogParameters = null)
    {
        if (MudExObjectEdit<TModel>.IsPrimitive())
        {
            var modelForPrimitive = new ModelForPrimitive<TModel>(value);
            var r = await dialogService.EditObject(modelForPrimitive, title, options, null, dialogParameters);
            return (r.Cancelled, r.Result.Value);
        }
        var parameters = new DialogParameters
            {
                {nameof(MudExObjectEditDialog<TModel>.Value), value},
                {nameof(MudExObjectEditDialog<TModel>.ConfigureMetaInformationAlways), true},
                {nameof(MudExObjectEditDialog<TModel>.MetaInformation), value.ObjectEditMeta(metaConfig)}
            };

        var dialog = await dialogService.ShowEx<MudExObjectEditDialog<TModel>>(title, MergeParameters(dialogParameters, parameters), options);

        var res = await dialog.Result;
        return (res.Cancelled, res.Cancelled ? value : (TModel)res.Data);
    }
}