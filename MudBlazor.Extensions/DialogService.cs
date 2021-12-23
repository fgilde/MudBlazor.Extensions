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

        public static async Task<IDialogReference> ShowEx(this IDialogService dialogService, Type type, string title, DialogOptionsEx options = null)
        {
            return await dialogService.Show(type, title, options).InjectOptionsAsync(options);
        }

        private static async Task<IDialogReference> InjectOptionsAsync(this IDialogReference dialogReference, DialogOptionsEx options)
        {
            var dialogComponent = await new Func<ComponentBase>(() => dialogReference.Dialog as ComponentBase).WaitForResultAsync();
            DotNetObjectReference<ComponentBase> callbackReference = DotNetObjectReference.Create(dialogComponent);
            var js = dialogComponent.ExposeField<IJSRuntime>("_jsRuntime") ?? dialogComponent.ExposeField<IJSRuntime>("_jsInterop");
            await js.EnsureCanWork();
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

            options.Buttons.Apply((i, button) => button.Html = button.GetHtml(i + (options.CloseButton == true ? 1 : 0)));
            await js.InvokeVoidAsync("MudBlazorExtensions.setNextDialogOptions", options, callbackReference);
            return dialogReference;
        }

    }
}