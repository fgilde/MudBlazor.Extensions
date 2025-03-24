using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions
{
    /// <summary>
    /// Dialog service extension methods.
    /// </summary>
    [HasDocumentation("DialogExtensions.md")]
    public static partial class DialogServiceExt
    {
        /// <summary>
        /// The default dialog options.
        /// </summary>
        /// <returns>The default dialog options.</returns>
        internal static DialogOptionsEx DefaultOptions() => DialogOptionsEx.DefaultDialogOptions?.CloneOptions() ?? new();


        /// <summary>
        /// Shows the dialog and injects dependencies asynchronously.
        /// </summary>
        /// <typeparam name="TDialog">The dialog type.</typeparam>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="title">The title.</param>
        /// <param name="dialogParameters">The dialog parameters.</param>
        /// <param name="optionsEx">The options.</param>
        /// <returns>A <see cref="IMudExDialogReference{T}"/>.</returns>
        public static Task<IMudExDialogReference<TDialog>> ShowEx<TDialog>(this IDialogService dialogService, string title, Action<TDialog> dialogParameters, Action<DialogOptionsEx> optionsEx)
            where TDialog : ComponentBase, new()
        {
            var options = DefaultOptions();
            optionsEx(options);
            return dialogService.ShowEx(title, dialogParameters, options);
        }

        /// <summary>
        /// Shows the dialog and injects dependencies asynchronously.
        /// </summary>
        /// <typeparam name="TDialog">The dialog type.</typeparam>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="title">The title.</param>
        /// <param name="dialogParameters">The dialog parameters.</param>
        /// <param name="optionsEx">The options.</param>
        /// <returns>A <see cref="IMudExDialogReference{T}"/>.</returns>
        public static Task<IMudExDialogReference<TDialog>> ShowEx<TDialog>(this IDialogService dialogService, string title, TDialog dialogParameters, Action<DialogOptionsEx> optionsEx)
            where TDialog : ComponentBase, new()
        {
            var options = DefaultOptions();
            optionsEx(options);
            return dialogService.ShowEx(title, dialogParameters, options);
        }

        /// <summary>
        /// Shows the dialog and injects dependencies asynchronously.
        /// </summary>
        /// <typeparam name="TDialog">The dialog type.</typeparam>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="title">The title.</param>
        /// <param name="dialogParameters">The dialog parameters.</param>
        /// <param name="optionsEx">The options.</param>
        /// <returns>A <see cref="IMudExDialogReference{T}"/>.</returns>
        public static Task<IMudExDialogReference<TDialog>> ShowEx<TDialog>(this IDialogService dialogService, string title, TDialog dialogParameters, DialogOptionsEx optionsEx = null) where TDialog : ComponentBase, new()
            => dialogService.ShowEx<TDialog>(title, dialogParameters.ConvertToDialogParameters(), optionsEx ?? DefaultOptions());

        /// <summary>
        /// Shows the dialog and injects dependencies asynchronously.
        /// </summary>
        /// <typeparam name="TDialog">The dialog type.</typeparam>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="title">The title.</param>
        /// <param name="dialogParameters">The dialog parameters.</param>
        /// <param name="optionsEx">The options.</param>
        /// <returns>The interface <see cref="IMudExDialogReference{T}"/>.</returns>
        public static Task<IMudExDialogReference<TDialog>> ShowEx<TDialog>(this IDialogService dialogService, string title, Action<TDialog> dialogParameters, DialogOptionsEx optionsEx = null) where TDialog : ComponentBase, new()
            => dialogService.ShowEx<TDialog>(title, dialogParameters.ConvertToDialogParameters(), optionsEx ?? DefaultOptions());

        /// <summary>
        /// Shows the dialog and injects dependencies immediately.
        /// </summary>
        /// <typeparam name="TDialog">The dialog type.</typeparam>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="title">The title.</param>
        /// <param name="dialogParameters">The dialog parameters.</param>
        /// <param name="options">The options.</param>
        /// <returns>The interface <see cref="IMudExDialogReference{T}"/>.</returns>
        public static IMudExDialogReference<TDialog> Show<TDialog>(this IDialogService dialogService, string title, Action<TDialog> dialogParameters, Action<DialogOptions> options)
            where TDialog : ComponentBase, new()
        {
            var dlgOptions = DefaultOptions();
            options(dlgOptions);
            return Show(dialogService, title, dialogParameters, dlgOptions);
        }

        /// <summary>
        /// Shows the dialog and injects dependencies immediately.
        /// </summary>
        /// <typeparam name="TDialog">The dialog type.</typeparam>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="title">The title.</param>
        /// <param name="dialogParameters">The dialog parameters.</param>
        /// <param name="options">The options.</param>
        /// <returns>The interface <see cref="IMudExDialogReference{T}"/>.</returns>
        public static IMudExDialogReference<TDialog> Show<TDialog>(this IDialogService dialogService, string title, TDialog dialogParameters, Action<DialogOptions> options)
            where TDialog : ComponentBase, new()
        {
            var dlgOptions = DefaultOptions();
            options(dlgOptions);
            return dialogService.Show(title, dialogParameters, dlgOptions).AsMudExDialogReference<TDialog>();
        }

        /// <summary>
        /// Shows the dialog and injects dependencies immediately.
        /// </summary>
        /// <typeparam name="TDialog">The dialog type.</typeparam>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="title">The title.</param>
        /// <param name="dialogParameters">The dialog parameters.</param>
        /// <param name="options">The options.</param>
        /// <returns>The interface <see cref="IMudExDialogReference{T}"/>.</returns>
        public static IMudExDialogReference<TDialog> Show<TDialog>(this IDialogService dialogService, string title, TDialog dialogParameters, DialogOptions options = null) where TDialog : ComponentBase, new()
            => dialogService.Show<TDialog>(title, dialogParameters.ConvertToDialogParameters(), options ?? DefaultOptions()).AsMudExDialogReference<TDialog>();

        /// <summary>
        /// Shows the dialog and injects dependencies immediately.
        /// </summary>
        /// <typeparam name="TDialog">The dialog type.</typeparam>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="title">The title.</param>
        /// <param name="dialogParameters">The dialog parameters.</param>
        /// <param name="options">The options.</param>
        /// <returns>The interface <see cref="IMudExDialogReference{T}"/>.</returns>
        public static IMudExDialogReference<TDialog> Show<TDialog>(this IDialogService dialogService, string title, Action<TDialog> dialogParameters, DialogOptions options = null) where TDialog : ComponentBase, new()
            => dialogService.Show<TDialog>(title, dialogParameters.ConvertToDialogParameters(), options ?? DefaultOptions()).AsMudExDialogReference<TDialog>();

        /// <summary>
        /// Shows the dialog and injects dependencies asynchronously.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="title">The title.</param>
        /// <param name="parameters">The dialog parameters.</param>
        /// <param name="options">The options.</param>
        /// <returns>The interface <see cref="IMudExDialogReference{T}"/>.</returns>
        public static Task<IMudExDialogReference<T>> ShowEx<T>(this IDialogService dialogService, string title, DialogParameters parameters, DialogOptionsEx options = null) where T : ComponentBase
            => dialogService.ShowAndInject<T>(title, options, parameters).AsMudExDialogReferenceAsync<T>();

        /// <summary>
        /// Shows the dialog and injects dependencies asynchronously.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="title">The title.</param>
        /// <param name="options">The options.</param>
        /// <returns>The interface <see cref="IMudExDialogReference{T}"/>.</returns>
        public static Task<IMudExDialogReference<T>> ShowEx<T>(this IDialogService dialogService, string title, DialogOptionsEx options = null) where T : ComponentBase
            => dialogService.ShowAndInject<T>(title, options).AsMudExDialogReferenceAsync<T>();

        /// <summary>
        /// Shows the dialog and injects dependencies immediately.
        /// </summary>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="type">The type.</param>
        /// <param name="title">The title.</param>
        /// <param name="parameters">The dialog parameters.</param>
        /// <param name="options">The options.</param>
        /// <returns>The interface <see cref="IDialogReference"/>reference.</returns>
        public static Task<IDialogReference> ShowEx(this IDialogService dialogService, Type type, string title, DialogParameters parameters, DialogOptionsEx options = null)
            => dialogService.ShowAndInject(type, title, options, parameters); //dialogService.Show(type, title, parameters, options).InjectOptionsAsync(options);

        /// <summary>
        /// Shows the message box asynchronously.
        /// </summary>
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


        /// <summary>
        /// Gets the dialog component asynchronously.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="dialogReference">The dialog reference.</param>
        /// <returns>The dialog component.</returns>
        public static async Task<T> GetDialogAsync<T>(this IDialogReference dialogReference) where T : ComponentBase
        {
            await Task.Run(() =>
            {
                while (dialogReference.Dialog == null)
                    Thread.Sleep(10);
            });
            return dialogReference.Dialog as T;
        }

        /// <summary>
        /// Shows the dialog and injects dependencies immediately.
        /// </summary>
        public static Task<IDialogReference> ShowEx(this IDialogService dialogService, Type type, string title, DialogOptionsEx options = null)
        {
            return ShowAndInject(dialogService, type, title, options);
        }


        internal static Task<IDialogReference> ShowAndInject<T>(this IDialogService dialogService, string title, DialogOptionsEx options, DialogParameters parameters = null) where T : ComponentBase
            => dialogService.ShowAndInject(typeof(T), title, options, parameters);

        internal static async Task<IDialogReference> ShowAndInject(this IDialogService dialogService, Type type, string title, DialogOptionsEx options, DialogParameters parameters = null)
        {
            options = options?.CloneOptions() ?? DefaultOptions();
            if (dialogService is IMudExDialogService service && options != null)
            {
                options.JsRuntime = service.JSRuntime;
                options.AppearanceService = service.AppearanceService;
            }

            await PrepareOptionsBeforeShow(options);

            Task OnAddedFunc(IDialogReference reference)
            {
                _ = reference.InjectOptionsAsync(dialogService, options);
                return Task.CompletedTask;
            }

            dialogService.DialogInstanceAddedAsync += OnAddedFunc;
            var res = await dialogService.ShowAsync(type, title, parameters ?? new DialogParameters(), options);
            dialogService.DialogInstanceAddedAsync -= OnAddedFunc;
            return res;
        }

        internal static async Task PrepareOptionsBeforeShow(DialogOptionsEx options)
        {
            if (options == null)
                return;
            if (!options.Modal)
                options.BackgroundClass = MudExCss.Classes.Backgrounds.NoModal;
            else if (options.DialogBackgroundAppearance != null)
                await options.GetAppearanceService().ApplyAsClassOnlyToAsync(options.DialogBackgroundAppearance, options, (o, cls) => o.BackgroundClass = $"{cls} {o.BackgroundClass}");

            (options.DialogAppearance ??= MudExAppearance.Empty()).WithStyle(b => b
                .WithHeight(options.CustomSize?.Height, !string.IsNullOrEmpty(options.CustomSize?.Height))
                .WithWidth(options.CustomSize?.Width, !string.IsNullOrEmpty(options.CustomSize?.Width))
            );

            //if (options.Animations?.Any(a => a != AnimationType.Default) == true)
            //    options.DialogAppearance.WithStyle(b => b.WithAnimations(options.Animations, options.AnimationDuration, AnimationDirection.In, options.AnimationTimingFunction, options.Position));
        }
        
        internal static async Task<IDialogReference> InjectOptionsAsync(this IDialogReference dialogReference, IDialogService service, DialogOptionsEx options)
        {
            options = PrepareOptionsAfterShow(options);
            if (service is MudExDialogService mudExService)
            {
                mudExService.AddOrUpdate(dialogReference.Id, dialogReference, options);
            }
            var callbackReference = await WaitForCallbackReference(dialogReference);
            var js = await JsImportHelper.GetInitializedJsRuntime(callbackReference.Value, options.JsRuntime);

            if (options.DialogAppearance != null)
                await options.GetAppearanceService().ApplyToAsync(options.DialogAppearance, dialogReference);

            await InjectOptionsAsync(service, callbackReference, js, options);
            return dialogReference;
        }

        private static async Task InjectOptionsAsync(IDialogService service, DotNetObjectReference<ComponentBase> callbackReference, IJSRuntime js, DialogOptionsEx options)
        {
            var serviceCallBackRef = DotNetObjectReference.Create(service);
            await js.InvokeVoidAsync("MudBlazorExtensions.setNextDialogOptions", options, callbackReference, serviceCallBackRef);
        }

        private static async Task<DotNetObjectReference<ComponentBase>> WaitForCallbackReference(IDialogReference dialogReference)
        {
            var dialogComponent = await new Func<ComponentBase>(() => dialogReference.Dialog as ComponentBase).WaitForResultAsync();
            DotNetObjectReference<ComponentBase> callbackReference = DotNetObjectReference.Create(dialogComponent);
            return callbackReference;
        }

        internal static DialogOptionsEx PrepareOptionsAfterShow(DialogOptionsEx options)
        {
            options ??= DefaultOptions();
            options = options.CloneOptions();

            (options.DialogAppearance ??= MudExAppearance.Empty()).WithCss(options.DisableSizeMarginY ?? false ? MudExCss.Classes.Dialog.FullHeightWithoutMargin : MudExCss.Classes.Dialog.FullHeightWithMargin, options.FullHeight ?? false);
            (options.DialogAppearance ??= MudExAppearance.Empty()).WithCss(options.MaxHeight != null ? $"mud-ex-dialog-max-height-{options.MaxHeight.GetDescription()}" : string.Empty);
            (options.DialogAppearance ??= MudExAppearance.Empty()).WithCss(MudExCss.Classes.Dialog.PositionFixedNoMargin, (options.DisablePositionMargin ?? false) || options.ShowAtCursor);


            if (options.MinimizeButton == true)
            {
                options.Buttons = InsertButton(options, new MudDialogButton(null, null)
                {
                    Id = $"mud-button-minimize-{Guid.NewGuid()}",
                    Icon = Icons.Material.Filled.Minimize
                });
            }
            if (options.MaximizeButton == true)
            {
                options.Buttons = InsertButton(options, new MudDialogButton(null, null)
                {
                    Id = $"mud-button-maximize-{Guid.NewGuid()}",
                    Icon = Icons.Material.Filled.AspectRatio
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