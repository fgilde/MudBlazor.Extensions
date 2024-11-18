//using Microsoft.AspNetCore.Http;
//using Microsoft.JSInterop;
//using MudBlazor.Extensions.Helper;

//namespace MudBlazor.Extensions.Middleware;

///// <summary>
///// Middleware for MudBlazor extensions that allows to inject IJSRuntime.
///// </summary>
//public class MudBlazorExtensionsMiddleware
//{
//    private readonly RequestDelegate _next;

//    /// <summary>
//    /// Initializes a new instance of the <see cref="MudBlazorExtensionsMiddleware"/> class.
//    /// </summary>
//    /// <param name="next">The next middleware in the pipeline.</param>
//    public MudBlazorExtensionsMiddleware(RequestDelegate next)
//    {
//        _next = next;
//    }

//    /// <summary>
//    /// Method invoked by the pipeline. Injects IJSRuntime into JsImportHelper and then passes control to the next middleware in the pipeline.
//    /// </summary>
//    /// <param name="context">The HttpContext for the current request.</param>
//    /// <param name="jsRuntime">The IJSRuntime instance for the current request.</param>
//    public async Task InvokeAsync(HttpContext context, IJSRuntime jsRuntime)
//    {
//        JsImportHelper.LegacyRuntimeReference = jsRuntime;
//        await _next(context);
//    }
//}
