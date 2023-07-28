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
        private static bool useMinified => true; //!Debugger.IsAttached;
        private static string min => useMinified ? ".min" : string.Empty;        
        
        internal static IJSRuntime _runtime;

        
        /// <summary>
        /// Imports requires JS module and required css styles for MudBlazor.Extensions
        /// </summary>        
        public static async Task<IJSRuntime> InitializeMudBlazorExtensionsAsync(this IJSRuntime runtime, bool force = false)
        {
            _runtime = runtime ?? _runtime; // This is a workaround for using module in MAUI apps
            if (force || !await runtime.IsNamespaceAvailableAsync("MudBlazorExtensions"))
            {
                await runtime.ImportModuleBlazorJS(); // This is a workaround for using module in MAUI apps
                if (useMinified)
                    await runtime.ImportModuleMudEx(); // This is a workaround for using module in MAUI apps
                else
                    await runtime.LoadJsAsync(MainJs());                                
            }
            if (force || !await runtime.IsElementAvailableAsync("mudex-styles"))
            {
                await runtime.RemoveElementAsync("mudex-styles");
                await runtime.AddCss(await MudExResource.GetEmbeddedFileContentAsync($"wwwroot/mudBlazorExtensions{min}.css"), "mudex-styles");
            }
            return runtime;
        }

        internal static IJSRuntime GetInitializedJsRuntime() => _runtime;

        internal static async Task<IJSRuntime> GetInitializedJsRuntime(object field, IJSRuntime fallback)
        {
            var js = fallback ?? _runtime ?? field.ExposeField<IJSRuntime>("_jsRuntime") ?? field.ExposeField<IJSRuntime>("_jsInterop");
            return await InitializeMudBlazorExtensionsAsync(js);
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