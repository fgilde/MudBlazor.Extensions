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
        private static string min => UseMinified ? ".min" : string.Empty;

        internal static bool UseMinified = false;

        internal static string BasePath = "./_content/";

        internal static IJSRuntime LegacyRuntimeReference;


        /// <summary>
        /// Imports requires JS module and required css styles for MudBlazor.Extensions
        /// </summary>        
        [Obsolete("This method is not needed anymore. If you have problems without this method, please report an issue.")]
        public static Task<IJSRuntime> InitializeMudBlazorExtensionsAsync(this IJSRuntime runtime, bool force = false) 
            => runtime.InitializeMudBlazorExtensionsCoreAsync(force);

        /// <summary>
        /// Imports requires JS module and required css styles for MudBlazor.Extensions and returns the initialized JS runtime
        /// </summary>        
        internal static async Task<IJSRuntime> InitializeMudBlazorExtensionsCoreAsync(this IJSRuntime runtime, bool force = false)
        {
            try
            {
                LegacyRuntimeReference ??= (runtime ??= GetJsRuntime());
                if (force || !UseMinified || !await runtime.IsNamespaceAvailableAsync("MudBlazorExtensions"))
                {
                    try
                    {
                        await runtime.ImportModuleBlazorJS(); // This is a workaround for using module in MAUI apps
                    }
                    catch (Exception e)
                    {}

                    try
                    {
                        await ImportMainMudEx(runtime); // This is a workaround for using module in MAUI apps
                    }
                    catch (Exception e)
                    {}
                }
                await LoadCssAsync(runtime, force);
            }
            catch (Exception e)
            {}
            return runtime;
        }

        internal static async Task LoadMudMarkdownAsync(this IJSRuntime runtime)
        {
            await runtime.LoadFilesAsync(
                "./_content/MudBlazor.Markdown/MudBlazor.Markdown.min.js",
                "./_content/MudBlazor.Markdown/MudBlazor.Markdown.min.css"
            );
            await runtime.WaitForNamespaceAsync("highlightCodeElement", TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200) );
            await Task.Delay(200);
        }

        internal static async Task LoadCssAsync(this IJSRuntime runtime, bool force = false) 
            => await runtime.AddCss(await MudExResource.GetEmbeddedFileContentAsync($"wwwroot/mudBlazorExtensions{min}.css"), "mudex-styles", !force);

        private static Task ImportMainMudEx(IJSRuntime runtime) 
            => UseMinified ? runtime.ImportModuleMudEx() : runtime.LoadFilesAsync(MainJs());

        internal static IJSRuntime GetInitializedJsRuntime() => LegacyRuntimeReference;

        internal static async Task<IJSRuntime> GetInitializedJsRuntime(object field, IJSRuntime fallback)
        {
            var js = fallback ?? LegacyRuntimeReference ?? field.ExposeField<IJSRuntime>("_jsRuntime") ?? field.ExposeField<IJSRuntime>("_jsInterop");
            return await InitializeMudBlazorExtensionsCoreAsync(js);
        }

        internal static Task<IJSObjectReference> ImportModuleMudEx(this IJSRuntime runtime) 
            => runtime.InvokeAsync<IJSObjectReference>("import", $"./_content/MudBlazor.Extensions/MudBlazor.Extensions.lib.module.js{Cb()}").AsTask();

        internal static string ComponentJs<TComponent>(string name = null) 
            => ComponentJs(GetJsComponentName<TComponent>(name), typeof(TComponent).Assembly.GetName().Name);

        private static string GetJsComponentName<TComponent>(string name = null) 
            => string.IsNullOrEmpty(name) ? typeof(TComponent).Name.Split('`')[0] : name;

        internal static string MainJs() 
            => $"{BasePath.EnsureEndsWith("/")}{Assembly.GetExecutingAssembly().GetName().Name}/js/mudBlazorExtensions.all{min}.js{Cb()}";

        internal static string ComponentJs(string componentName, string assemblyDirName = null)
        {
            return JsPath($"/js/components/{componentName}{min}.js{Cb()}", assemblyDirName);
        }

        internal static string JsPath(string path, string assemblyDirName = null)
        {
            //var assemblyDirName = Assembly.GetExecutingAssembly().GetName().Name;
            assemblyDirName ??= Assembly.GetCallingAssembly().GetName().Name;
            return $"{BasePath.EnsureEndsWith("/")}{assemblyDirName}{path.EnsureStartsWith("/")}";
        }

        private static string Cb() => UseMinified ? string.Empty : $"?cb={DateTime.Now.Ticks}";

        internal static Task<IJSObjectReference> ImportModuleAsync<TComponent>(this IJSRuntime js, string name=null) 
            => js.ImportModuleAsync(ComponentJs<TComponent>(name));

        internal static Task<(IJSObjectReference moduleReference, IJSObjectReference jsObjectReference)> ImportModuleAndCreateJsAsync<TComponent>(this IJSRuntime js, string name, params object?[]? args) 
            => js.ImportModuleAndCreateJsAsync(ComponentJs<TComponent>(name), $"initialize{GetJsComponentName<TComponent>(name)}", args);

        internal static Task<(IJSObjectReference moduleReference, IJSObjectReference jsObjectReference)> ImportModuleAndCreateJsAsync<TComponent>(this IJSRuntime js, params object?[]? args) 
            => js.ImportModuleAndCreateJsAsync(ComponentJs<TComponent>(), $"initialize{GetJsComponentName<TComponent>()}", args);

        private static IJSInProcessRuntime GetJsRuntime()
        {
            const string defaultJsRuntimeTypeName = "DefaultWebAssemblyJSRuntime";
            const string instanceFieldName = "Instance";

            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a?.FullName?.Contains("Microsoft.AspNetCore.Components.WebAssembly") == true);
            if (assembly == null)
                return null;

            var defaultJsRuntimeType = assembly
                .GetTypes()
                .SingleOrDefault(t => t.Name == defaultJsRuntimeTypeName);

            if (defaultJsRuntimeType == null)
            {
                return null;
            }

            var instanceField = defaultJsRuntimeType.GetField(instanceFieldName, BindingFlags.Static | BindingFlags.NonPublic);
            if (instanceField == null)
            {
                return null;
            }

            return (IJSInProcessRuntime)instanceField.GetValue(obj: null);
        }
    }
}