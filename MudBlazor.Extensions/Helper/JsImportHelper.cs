using System.Reflection;
using BlazorJS;
using Microsoft.JSInterop;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Helper
{
    /// <summary>
    /// Helper class for importing JS modules
    /// </summary>
    public static class JsImportHelper
    {
        private static bool useMinified => true;
        private static string min => useMinified ? ".min" : string.Empty;        
        
        internal static IJSRuntime LegacyRuntimeReference;


        /// <summary>
        /// Imports requires JS module and required css styles for MudBlazor.Extensions
        /// </summary>        
        [Obsolete("This method is not needed anymore. If you have problems without this method, please report an issue.")]
        public static Task<IJSRuntime> InitializeMudBlazorExtensionsAsync(this IJSRuntime runtime, bool force = false) 
            => runtime.InitializeMudBlazorExtensionsCoreAsync(force);

        /// <summary>
        /// Imports requires JS module and required css styles for MudBlazor.Extensions
        /// </summary>        
        internal static async Task<IJSRuntime> InitializeMudBlazorExtensionsCoreAsync(this IJSRuntime runtime, bool force = false)
        {
            LegacyRuntimeReference ??= runtime;
            if (force || !await runtime.IsNamespaceAvailableAsync("MudBlazorExtensions"))
            {
                await runtime.ImportModuleBlazorJS(); // This is a workaround for using module in MAUI apps
                await ImportMainMudEx(runtime); // This is a workaround for using module in MAUI apps
            }
            await runtime.AddCss(await MudExResource.GetEmbeddedFileContentAsync($"wwwroot/mudBlazorExtensions{min}.css"), "mudex-styles", !force);
            return runtime;
        }

        private static Task ImportMainMudEx(IJSRuntime runtime) 
            => useMinified ? runtime.ImportModuleMudEx() : runtime.LoadFilesAsync(MainJs());

        internal static IJSRuntime GetInitializedJsRuntime() => LegacyRuntimeReference;

        internal static async Task<IJSRuntime> GetInitializedJsRuntime(object field, IJSRuntime fallback)
        {
            var js = fallback ?? LegacyRuntimeReference ?? field.ExposeField<IJSRuntime>("_jsRuntime") ?? field.ExposeField<IJSRuntime>("_jsInterop");
            return await InitializeMudBlazorExtensionsCoreAsync(js);
        }

        internal static Task<IJSObjectReference> ImportModuleMudEx(this IJSRuntime runtime) 
            => runtime.InvokeAsync<IJSObjectReference>("import", $"./_content/MudBlazor.Extensions/MudBlazor.Extensions.lib.module.js{Cb()}").AsTask();

        internal static string ComponentJs<TComponent>(string name = null) 
            => ComponentJs(GetJsComponentName<TComponent>(name));

        private static string GetJsComponentName<TComponent>(string name = null) 
            => string.IsNullOrEmpty(name) ? typeof(TComponent).Name.Split('`')[0] : name;

        internal static string MainJs() 
            => $"/_content/{Assembly.GetExecutingAssembly().GetName().Name}/js/mudBlazorExtensions.all{min}.js{Cb()}";

        internal static string ComponentJs(string componentName) 
            => $"/_content/{Assembly.GetExecutingAssembly().GetName().Name}/js/components/{componentName}{min}.js{Cb()}";

        private static string Cb() => useMinified ? string.Empty : $"?cb={DateTime.Now.Ticks}";

        internal static Task<IJSObjectReference> ImportModuleAsync<TComponent>(this IJSRuntime js, string name=null) 
            => js.ImportModuleAsync(ComponentJs<TComponent>(name));

        internal static Task<(IJSObjectReference moduleReference, IJSObjectReference jsObjectReference)> ImportModuleAndCreateJsAsync<TComponent>(this IJSRuntime js, string name, params object?[]? args) 
            => js.ImportModuleAndCreateJsAsync(ComponentJs<TComponent>(name), $"initialize{GetJsComponentName<TComponent>(name)}", args);

        internal static Task<(IJSObjectReference moduleReference, IJSObjectReference jsObjectReference)> ImportModuleAndCreateJsAsync<TComponent>(this IJSRuntime js, params object?[]? args) 
            => js.ImportModuleAndCreateJsAsync(ComponentJs<TComponent>(), $"initialize{GetJsComponentName<TComponent>()}", args);
    }
}