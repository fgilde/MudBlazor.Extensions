using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions
{
    public static class DialogServiceExt
    {
        public static async Task<IDialogReference> ShowEx<T>(this IDialogService dialogService, string title, DialogParameters parameters, DialogOptionsEx options = null)
            where T : ComponentBase
        {
            return await dialogService.Show<T>(title, parameters, options).InjectOptionsAsync(options);
        }

        public static async Task<IDialogReference> ShowEx<T>(this IDialogService dialogService, string title, DialogOptionsEx options = null)
            where T : ComponentBase
        {
            return await dialogService.Show<T>(title, options).InjectOptionsAsync(options);
        }

        public static async Task<IDialogReference> ShowEx(this IDialogService dialogService, Type type, string title, DialogParameters parameters, DialogOptionsEx options = null)
        {
            return await dialogService.Show(type, title, parameters, options).InjectOptionsAsync(options);
        }

        public static async Task<bool?> ShowMessageBoxEx(IDialogService dialogService, MessageBoxOptions mboxOptions, DialogOptionsEx options = null)
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