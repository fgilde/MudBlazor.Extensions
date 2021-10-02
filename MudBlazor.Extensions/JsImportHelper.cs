using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.JSInterop;

namespace MudBlazor.Extensions
{
    public static class JsImportHelper
    {
        private static bool initialized;
        
        public static async Task<IJSRuntime> EnsureCanWork(this IJSRuntime runtime)
        {
            if (!initialized)
            {
                //await runtime.InvokeVoidAsync("eval", "document.body.appendChild(Object.assign(document.createElement('script'),{src: './_content/MudBlazor.Extensions/mudBlazorExtensions.js',type: 'text/javascript' })); ");
                var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());
                var fileInfo = embeddedProvider.GetFileInfo("wwwroot/mudBlazorExtensions.js");
                await using var stream = fileInfo.CreateReadStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var javaScript = await reader.ReadToEndAsync();
                await runtime.InvokeVoidAsync("eval", javaScript);
                initialized = true;

            }
            return runtime;
        }
    }
}