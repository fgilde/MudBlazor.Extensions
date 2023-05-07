using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Nextended.Core;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions
{
    public static partial class DialogServiceExt
    {
        private static DialogOptionsEx DefaultOptions() => DialogOptionsEx.DefaultDialogOptions?.CloneOptions() ?? new();

        private static DialogParameters MergeParameters(DialogParameters dialogParameters, DialogParameters parameters)
        {
            if (dialogParameters != null)
            {
                foreach (var param in dialogParameters)
                    parameters.Add(param.Key, param.Value);
            }

            return parameters;
        }
        
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

        public static Task<IMudExDialogReference<TDialog>> ShowEx<TDialog>(this IDialogService dialogService, string title, Action<TDialog> dialogParameters, Action<DialogOptionsEx> optionsEx)
            where TDialog : ComponentBase, new()
        {
            var options = DefaultOptions();
            optionsEx(options);
            return dialogService.ShowEx<TDialog>(title, dialogParameters, options);
        }

        public static Task<IMudExDialogReference<TDialog>> ShowEx<TDialog>(this IDialogService dialogService, string title, TDialog dialogParameters, Action<DialogOptionsEx> optionsEx)
            where TDialog : ComponentBase, new()
        {
            var options = DefaultOptions();
            optionsEx(options);
            return dialogService.ShowEx<TDialog>(title, dialogParameters, options);
        }

        public static async Task<IMudExDialogReference<TDialog>> ShowEx<TDialog>(this IDialogService dialogService, string title, TDialog dialogParameters, DialogOptionsEx optionsEx = null) where TDialog : ComponentBase, new()
            => await dialogService.ShowEx<TDialog>(title, dialogParameters.ConvertToDialogParameters(), optionsEx ?? DefaultOptions());

        public static async Task<IMudExDialogReference<TDialog>> ShowEx<TDialog>(this IDialogService dialogService, string title, Action<TDialog> dialogParameters, DialogOptionsEx optionsEx = null) where TDialog : ComponentBase, new()
            => await dialogService.ShowEx<TDialog>(title, dialogParameters.ConvertToDialogParameters(), optionsEx ?? DefaultOptions());

        public static IMudExDialogReference<TDialog> Show<TDialog>(this IDialogService dialogService, string title, Action<TDialog> dialogParameters, Action<DialogOptions> options)
            where TDialog : ComponentBase, new()
        {
            var dlgOptions = DefaultOptions();
            options(dlgOptions);
            return Show<TDialog>(dialogService, title, dialogParameters, dlgOptions);
        }

        public static IMudExDialogReference<TDialog> Show<TDialog>(this IDialogService dialogService, string title, TDialog dialogParameters, Action<DialogOptions> options)
            where TDialog : ComponentBase, new()
        {
            var dlgOptions = DefaultOptions();
            options(dlgOptions);
            return dialogService.Show<TDialog>(title, dialogParameters, dlgOptions).AsMudExDialogReference<TDialog>();
        }

        public static IMudExDialogReference<TDialog> Show<TDialog>(this IDialogService dialogService, string title, TDialog dialogParameters, DialogOptions options = null) where TDialog : ComponentBase, new()
            => dialogService.Show<TDialog>(title, dialogParameters.ConvertToDialogParameters(), options ?? DefaultOptions()).AsMudExDialogReference<TDialog>();

        public static IMudExDialogReference<TDialog> Show<TDialog>(this IDialogService dialogService, string title, Action<TDialog> dialogParameters, DialogOptions options = null) where TDialog : ComponentBase, new()
            => dialogService.Show<TDialog>(title, dialogParameters.ConvertToDialogParameters(), options ?? DefaultOptions()).AsMudExDialogReference<TDialog>();

        public static async Task<IMudExDialogReference<T>> ShowEx<T>(this IDialogService dialogService, string title, DialogParameters parameters, DialogOptionsEx options = null) where T : ComponentBase
            => await dialogService.ShowAndInject<T>(title, options, parameters).AsMudExDialogReferenceAsync<T>();

        public static async Task<IMudExDialogReference<T>> ShowEx<T>(this IDialogService dialogService, string title, DialogOptionsEx options = null) where T : ComponentBase 
            => await dialogService.ShowAndInject<T>(title, options).AsMudExDialogReferenceAsync<T>();

        public static async Task<IDialogReference> ShowEx(this IDialogService dialogService, Type type, string title, DialogParameters parameters, DialogOptionsEx options = null) 
            => await dialogService.ShowAndInject(type, title, options, parameters); //dialogService.Show(type, title, parameters, options).InjectOptionsAsync(options);

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
            //options.ClassBackground = $"mud-ex-mb-container {options.ClassBackground}";
            var result = await (await dialogService.ShowAndInject<MudMessageBox>(title, options, parameters)).Result;
            return result.Canceled || !(result.Data is bool data) ? new bool?() : data;
        }


        public static async Task<T> GetDialogAsync<T>(this IDialogReference dialogReference) where T : ComponentBase
        {
            await Task.Run(() =>
            {
                while (dialogReference.Dialog == null)
                    Thread.Sleep(10);
            });
            return dialogReference.Dialog as T;
        }

        public static async Task<IDialogReference> ShowEx(this IDialogService dialogService, Type type, string title, DialogOptionsEx options = null)
        {
            return await ShowAndInject(dialogService, type, title, options);
        }

        public static string GetDialogId(this IDialogReference dialogReference)
        {
            return dialogReference != null && dialogReference.Id != Guid.Empty ? $"_{dialogReference.Id.ToString().Replace("-", "")}" : null;
        }


        internal static Task<IDialogReference> ShowAndInject<T>(this IDialogService dialogService, string title, DialogOptionsEx options, DialogParameters parameters = null) where T : ComponentBase 
            => dialogService.ShowAndInject(typeof(T), title, options, parameters);

        internal static Task<IDialogReference> ShowAndInject(this IDialogService dialogService, Type type, string title, DialogOptionsEx options, DialogParameters parameters = null)
        {
            if (!options.Modal)
                options.ClassBackground = $"mud-dialog-container-no-modal {options.ClassBackground}";
            return dialogService.Show(type, title, parameters, options).InjectOptionsAsync(options);
        }

        internal static async Task<IDialogReference> InjectOptionsAsync(this Task<IDialogReference> dialogReference,
            DialogOptionsEx options)
        {
            return await (await dialogReference).InjectOptionsAsync(options);
        }

        internal static async Task<IDialogReference> InjectOptionsAsync(this IDialogReference dialogReference, DialogOptionsEx options)
        {
            var callbackReference = await WaitForCallbackReference(dialogReference);
            var js = await JsImportHelper.GetInitializedJsRuntime(callbackReference.Value, options.JsRuntime);
            await InjectOptionsAsync(callbackReference, js, options);
            return dialogReference;
        }

        internal static async Task InjectOptionsAsync(DotNetObjectReference<ComponentBase> callbackReference, IJSRuntime js, DialogOptionsEx options)
        {
            options = PrepareOptions(options);
            await js.InvokeVoidAsync("MudBlazorExtensions.setNextDialogOptions", options, callbackReference);
        }

        private static async Task<DotNetObjectReference<ComponentBase>> WaitForCallbackReference(IDialogReference dialogReference)
        {
            var dialogComponent = await new Func<ComponentBase>(() => dialogReference.Dialog as ComponentBase).WaitForResultAsync();
            DotNetObjectReference<ComponentBase> callbackReference = DotNetObjectReference.Create(dialogComponent);
            return callbackReference;
        }

        private static DialogOptionsEx PrepareOptions(DialogOptionsEx options)
        {
            options ??= DefaultOptions();
            options = options.CloneOptions();
            if (options.MinimizeButton == true)
            {
                options.Buttons = InsertButton(options, new MudDialogButton(null, null)
                {
                    Id = $"mud-button-minimize-{Guid.NewGuid()}",
                    Icon = Icons.Filled.Minimize
                });
            }
            if (options.MaximizeButton == true)
            {
                options.Buttons = InsertButton(options, new MudDialogButton(null, null)
                {
                    Id = $"mud-button-maximize-{Guid.NewGuid()}",
                    Icon = Icons.Filled.AspectRatio
                });
            }

            options.Buttons ??= Array.Empty<MudDialogButton>();
            options.Buttons.Apply((i, button) => button.Html = button.GetHtml(i + (options.CloseButton == true ? 1 : 0)));
            return options;
        }

        private static MudDialogButton[] InsertButton(DialogOptionsEx options, MudDialogButton btn)
        {
            var buttons = options.Buttons?.ToList() ?? new List<MudDialogButton>();
            buttons.Insert(0, btn);
            return buttons.ToArray();
        }

    }
}