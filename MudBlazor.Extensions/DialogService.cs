using System.Net.Mime;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Extensions;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Nextended.Core;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;
using Nextended.Blazor.Extensions;

namespace MudBlazor.Extensions
{
    public static class DialogServiceExt
    {
        private static DialogParameters MergeParameters(DialogParameters dialogParameters, DialogParameters parameters)
        {
            if (dialogParameters != null)
            {
                foreach (var param in dialogParameters)
                    parameters.Add(param.Key, param.Value);
            }

            return parameters;
        }

        #region File Display

        public static Task<IDialogReference> ShowFileDisplayDialog(this IDialogService dialogService, string url, string fileName, string contentType, Func<IFileDisplayInfos, Task<ContentErrorResult>> handleContentErrorFunc,  Action<DialogOptionsEx> options = null) 
            => dialogService.ShowFileDisplayDialog(url, fileName, contentType, options, new DialogParameters { { nameof(MudExFileDisplay.HandleContentErrorFunc), handleContentErrorFunc } });

        public static Task<IDialogReference> ShowFileDisplayDialog(this IDialogService dialogService, IBrowserFile browserFile, Func<IFileDisplayInfos, Task<ContentErrorResult>> handleContentErrorFunc, Action<DialogOptionsEx> options = null) 
            => dialogService.ShowFileDisplayDialog(browserFile, options, new DialogParameters { { nameof(MudExFileDisplay.HandleContentErrorFunc), handleContentErrorFunc } });

        public static Task<IDialogReference> ShowFileDisplayDialog(this IDialogService dialogService, Stream stream, string fileName, string contentType, Func<IFileDisplayInfos, Task<ContentErrorResult>> handleContentErrorFunc, Action<DialogOptionsEx> options = null)
            => dialogService.ShowFileDisplayDialog(stream, fileName, contentType, options, new DialogParameters { { nameof(MudExFileDisplay.HandleContentErrorFunc), handleContentErrorFunc } });

        public static async Task<IDialogReference> ShowFileDisplayDialog(this IDialogService dialogService, string url, string fileName, string contentType, Action<DialogOptionsEx> options = null, DialogParameters dialogParameters = null)
        {
            var parameters = new DialogParameters
        {
            {nameof(MudExFileDisplayDialog.Icon), BrowserFileExt.IconForFile(contentType)},
            {nameof(MudExFileDisplayDialog.Url), url},
            {nameof(MudExFileDisplayDialog.ContentType), contentType}
        };
            
            return await dialogService.ShowFileDisplayDialog(fileName, MergeParameters(dialogParameters, parameters), options);
        }

        public static async Task<IDialogReference> ShowFileDisplayDialog(this IDialogService dialogService, IBrowserFile browserFile, Action<DialogOptionsEx> options = null, DialogParameters dialogParameters = null)
        {
            if (MimeType.IsZip(browserFile.ContentType))
            {
                var ms = new MemoryStream(await browserFile.GetBytesAsync());
                return await dialogService.ShowFileDisplayDialog(ms, browserFile.Name, browserFile.ContentType, options);
            }
            return await dialogService.ShowFileDisplayDialog(await browserFile.GetDataUrlAsync(), browserFile.Name, browserFile.ContentType, options, dialogParameters);
        }

        public static async Task<IDialogReference> ShowFileDisplayDialog(this IDialogService dialogService, Stream stream, string fileName, string contentType, Action<DialogOptionsEx> options = null, DialogParameters dialogParameters = null)
        {
            var parameters = new DialogParameters
        {
            {nameof(MudExFileDisplayDialog.Icon), BrowserFileExt.IconForFile(contentType)},
            {nameof(MudExFileDisplayDialog.ContentStream), stream},
            {nameof(MudExFileDisplayDialog.ContentType), contentType}
        };

            return await dialogService.ShowFileDisplayDialog(fileName, MergeParameters(dialogParameters, parameters), options);
        }

        private static async Task<IDialogReference> ShowFileDisplayDialog(this IDialogService dialogService, string fileName, DialogParameters parameters, Action<DialogOptionsEx> options = null)
        {
            var optionsEx = new DialogOptionsEx
            {
                CloseButton = true,
                MaxWidth = MaxWidth.ExtraExtraLarge,
                FullWidth = true,
                DisableBackdropClick = false,
                MaximizeButton = true,
                DragMode = MudDialogDragMode.Simple,
                Position = DialogPosition.BottomCenter,
                Animations = new[] { AnimationType.FadeIn, AnimationType.SlideIn },
                AnimationDuration = TimeSpan.FromSeconds(1),
                FullHeight = true,
                Resizeable = true
            };
            options?.Invoke(optionsEx);

            return await dialogService.ShowEx<MudExFileDisplayDialog>(fileName, parameters, optionsEx);
        }

        #endregion


        #region Object edit

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

        #endregion
        
        public static DialogParameters ToDialogParameters(this IEnumerable<KeyValuePair<string, object>> parameters)
        {
            var dialogParameters = new DialogParameters();
            foreach (var parameter in parameters)
                dialogParameters.Add(parameter.Key, parameter.Value);
            return dialogParameters;
        }

        public static DialogParameters ConvertToDialogParameters<TDialog>(this Action<TDialog> dialogParameters) where TDialog : new()
            => DictionaryHelper.GetValuesDictionary(dialogParameters, true).Where(p => typeof(TDialog).GetProperty(p.Key, BindingFlags.Public | BindingFlags.Instance)?.CanWrite == true).ToDialogParameters();

        public static DialogParameters ConvertToDialogParameters<TDialog>(this TDialog dialogParameters) where TDialog : new()
            => DictionaryHelper.GetValuesDictionary(dialogParameters, true).Where(p => typeof(TDialog).GetProperty(p.Key, BindingFlags.Public | BindingFlags.Instance)?.CanWrite == true).ToDialogParameters();

        public static Task<IDialogReference> ShowEx<TDialog>(this IDialogService dialogService, string title, Action<TDialog> dialogParameters, Action<DialogOptionsEx> optionsEx)
            where TDialog : ComponentBase, new()
        {
            var options = new DialogOptionsEx();
            optionsEx(options);
            return dialogService.ShowEx<TDialog>(title, dialogParameters, options);
        }

        public static Task<IDialogReference> ShowEx<TDialog>(this IDialogService dialogService, string title, TDialog dialogParameters, Action<DialogOptionsEx> optionsEx)
            where TDialog : ComponentBase, new()
        {
            var options = new DialogOptionsEx();
            optionsEx(options);
            return dialogService.ShowEx<TDialog>(title, dialogParameters, options);
        }

        public static async Task<IDialogReference> ShowEx<TDialog>(this IDialogService dialogService, string title, TDialog dialogParameters, DialogOptionsEx optionsEx = null) where TDialog : ComponentBase, new()
            => await dialogService.ShowEx<TDialog>(title, dialogParameters.ConvertToDialogParameters(), optionsEx ?? new DialogOptionsEx());

        public static async Task<IDialogReference> ShowEx<TDialog>(this IDialogService dialogService, string title, Action<TDialog> dialogParameters, DialogOptionsEx optionsEx = null) where TDialog : ComponentBase, new()
            => await dialogService.ShowEx<TDialog>(title, dialogParameters.ConvertToDialogParameters(), optionsEx ?? new DialogOptionsEx());

        public static IDialogReference Show<TDialog>(this IDialogService dialogService, string title, Action<TDialog> dialogParameters, Action<DialogOptions> options)
            where TDialog : ComponentBase, new()
        {
            var dlgOptions = new DialogOptions();
            options(dlgOptions);
            return Show<TDialog>(dialogService, title, dialogParameters, dlgOptions);
        }

        public static IDialogReference Show<TDialog>(this IDialogService dialogService, string title, TDialog dialogParameters, Action<DialogOptions> options)
            where TDialog : ComponentBase, new()
        {
            var dlgOptions = new DialogOptions();
            options(dlgOptions);
            return dialogService.Show<TDialog>(title, dialogParameters, dlgOptions);
        }

        public static IDialogReference Show<TDialog>(this IDialogService dialogService, string title, TDialog dialogParameters, DialogOptions options = null) where TDialog : ComponentBase, new()
            => dialogService.Show<TDialog>(title, dialogParameters.ConvertToDialogParameters(), options ?? new DialogOptions());

        public static IDialogReference Show<TDialog>(this IDialogService dialogService, string title, Action<TDialog> dialogParameters, DialogOptions options = null) where TDialog : ComponentBase, new()
            => dialogService.Show<TDialog>(title, dialogParameters.ConvertToDialogParameters(), options ?? new DialogOptions());

        public static async Task<IDialogReference> ShowEx<T>(this IDialogService dialogService, string title, DialogParameters parameters, DialogOptionsEx options = null) where T : ComponentBase 
            => await dialogService.Show<T>(title, parameters, options).InjectOptionsAsync(options);

        public static async Task<IDialogReference> ShowEx<T>(this IDialogService dialogService, string title, DialogOptionsEx options = null) where T : ComponentBase 
            => await dialogService.Show<T>(title, options).InjectOptionsAsync(options);

        public static async Task<IDialogReference> ShowEx(this IDialogService dialogService, Type type, string title, DialogParameters parameters, DialogOptionsEx options = null) 
            => await dialogService.Show(type, title, parameters, options).InjectOptionsAsync(options);

        public static async Task<bool?> ShowMessageBoxEx(this IDialogService dialogService, MessageBoxOptions mboxOptions, DialogOptionsEx options = null)
        {
            DialogParameters parameters = new DialogParameters()
            {
                ["Title"] = mboxOptions.Title,
                ["Message"] = mboxOptions.Message,
                ["MarkupMessage"] = mboxOptions.MarkupMessage,
                ["CancelText"] = mboxOptions.CancelText,
                ["NoText"] = mboxOptions.NoText,
                ["YesText"] = mboxOptions.YesText
            };
            
            string title = mboxOptions.Title;
            
            DialogResult result = await (await dialogService.Show<MudMessageBox>(title, parameters, options).InjectOptionsAsync(options)).Result;
            return result.Cancelled || !(result.Data is bool data) ? new bool?() : data;
        }

        public static async Task<IDialogReference> ShowEx(this IDialogService dialogService, Type type, string title, DialogOptionsEx options = null)
        {
            return await dialogService.Show(type, title, options).InjectOptionsAsync(options);
        }

        private static async Task<IDialogReference> InjectOptionsAsync(this IDialogReference dialogReference, DialogOptionsEx options)
        {
            var dialogComponent = await new Func<ComponentBase>(() => dialogReference.Dialog as ComponentBase).WaitForResultAsync();
            DotNetObjectReference<ComponentBase> callbackReference = DotNetObjectReference.Create(dialogComponent);
            var js = await JsImportHelper.GetInitializedJsRuntime(dialogComponent, options.JsRuntime);

            if (options.MaximizeButton == true)
            {
                var buttons = options.Buttons?.ToList() ?? new List<MudDialogButton>();
                buttons.Insert(0, new MudDialogButton(null, null)
                {
                    Id = $"mud-button-maximize-{Guid.NewGuid()}",
                    Icon = Icons.Filled.AspectRatio
                });
                options.Buttons = buttons.ToArray();
            }

            options.Buttons ??= Array.Empty<MudDialogButton>();
            options.Buttons.Apply((i, button) => button.Html = button.GetHtml(i + (options.CloseButton == true ? 1 : 0)));
            await js.InvokeVoidAsync("MudBlazorExtensions.setNextDialogOptions", options, callbackReference);
            return dialogReference;
        }

    }
}