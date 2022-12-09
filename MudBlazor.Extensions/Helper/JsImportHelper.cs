using System.Reflection;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.JSInterop;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Helper
{
    public static class JsImportHelper
    {
        private const bool useMinified = false;
        
        private static string min => useMinified ? ".min" : string.Empty;
        private static bool initialized;
        private static IJSRuntime _runtime; 

        internal static async Task<IJSRuntime> GetInitializedJsRuntime(object field, IJSRuntime fallback)
        {
            var js = fallback ?? _runtime ?? field.ExposeField<IJSRuntime>("_jsRuntime") ?? field.ExposeField<IJSRuntime>("_jsInterop");
            return await InitializeMudBlazorExtensionsAsync(js);
        }

        public static async Task<IJSRuntime> InitializeMudBlazorExtensionsAsync(this IJSRuntime runtime, bool force = false)
        {
            _runtime = runtime ?? _runtime;
            if (force || !initialized)
            {
                //var jsToLoad = "wwwroot/js/mudBlazorExtensions.js";
                var jsToLoad = $"wwwroot/js/mudBlazorExtensions.es5{min}.js";
                var cssToLoad = $"wwwroot/mudBlazorExtensions{min}.css";

                var js = await GetEmbeddedFileContentAsync(jsToLoad);
                await runtime.InvokeVoidAsync("eval", js);

                var css = await GetEmbeddedFileContentAsync(cssToLoad);
                await runtime.InvokeVoidAsync("MudBlazorExtensions.addCss", css);
                initialized = true;

            }
            return runtime;
        }

        internal static string ComponentJs<TComponent>()
        {
            return ComponentJs(typeof(TComponent).Name);
        }

        internal static string UtilJs()
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            return $"/_content/{assemblyName}/js/mudExUtils{min}.js";
        }

        internal static string UtilJs(string utilName)
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            return $"/_content/{assemblyName}/js/utils/{utilName}.js";
        }

        internal static string ComponentJs(string componentName)
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            return $"/_content/{assemblyName}/js/components/{componentName}{min}.js";
        }

        internal static Task<IJSObjectReference> ImportModuleAsync<TComponent>(this IJSRuntime js)
        {
            return js.ImportModuleAsync(ComponentJs<TComponent>());
        }

        public static async Task<IJSObjectReference> ImportModuleAsync(this IJSRuntime js, string file)
        {
            return await js.InvokeAsync<IJSObjectReference>("import", file);
        }

        internal static Task<(IJSObjectReference moduleReference, IJSObjectReference jsObjectReference)> ImportModuleAndCreateJsAsync<TComponent>(this IJSRuntime js, params object?[]? args)
        {
            return js.ImportModuleAndCreateJsAsync(ComponentJs<TComponent>(), $"initialize{typeof(TComponent).Name}", args);
        }

        internal static Task<(IJSObjectReference moduleReference, IJSObjectReference jsObjectReference)> ImportModuleAndCreateJsAsync<TComponent>(this IJSRuntime js, string jsCreateMethod, params object?[]? args)
        {
            return js.ImportModuleAndCreateJsAsync(ComponentJs<TComponent>(), jsCreateMethod, args);
        }

        public static async Task<(IJSObjectReference moduleReference, IJSObjectReference jsObjectReference)> ImportModuleAndCreateJsAsync(this IJSRuntime js, string file, string jsCreateMethod, params object?[]? args)
        {
            IJSObjectReference jsReference = null;
            var module = await js.ImportModuleAsync(file);
            if (module != null)
            {
                jsReference = await module.InvokeAsync<IJSObjectReference>(jsCreateMethod, args);
            }
            return (module, jsReference);
        }

        private static async Task<string> GetEmbeddedFileContentAsync(string file)
        {
            var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());
            var fileInfo = embeddedProvider.GetFileInfo(file);
            await using var stream = fileInfo.CreateReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
    }
}