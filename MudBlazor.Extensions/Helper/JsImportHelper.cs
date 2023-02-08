using System.Reflection;
using System.Text;
using BlazorJS;
using Microsoft.Extensions.FileProviders;
using Microsoft.JSInterop;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Helper
{
    public static class JsImportHelper
    {
        private static bool useMinified => true; //!Debugger.IsAttached;
        
        private static string min => useMinified ? ".min" : string.Empty;
        private static bool initialized;
        internal static IJSRuntime _runtime; 

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
                var cssToLoad = $"wwwroot/mudBlazorExtensions{min}.css";
                var css = await GetEmbeddedFileContentAsync(cssToLoad);
                await runtime.LoadJsAsync(MainJs());
                await runtime.AddCss(css);
                initialized = true;
            }
            return runtime;
        }

        internal static string ComponentJs<TComponent>()
        {
            var componentName = GetJsComponentName<TComponent>();
            return ComponentJs(componentName);
        }

        private static string GetJsComponentName<TComponent>()
        {
            return typeof(TComponent).Name.Split('`')[0];
        }

        internal static string MainJs()
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            //return $"/_content/{assemblyName}/js/mudBlazorExtensions.js";
            return $"/_content/{assemblyName}/js/mudBlazorExtensions.all{min}.js";
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

        internal static Task<(IJSObjectReference moduleReference, IJSObjectReference jsObjectReference)> ImportModuleAndCreateJsAsync<TComponent>(this IJSRuntime js, params object?[]? args)
        {
            return js.ImportModuleAndCreateJsAsync(ComponentJs<TComponent>(), $"initialize{GetJsComponentName<TComponent>()}", args);
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