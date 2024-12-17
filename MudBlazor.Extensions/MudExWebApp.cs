using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace MudBlazor.Extensions;

/// <summary>
/// MudBlazorExtensionsMiddlewareExtensions
/// </summary>
public static class MudExWebApp
{

    //public static IApplicationBuilder UseMudExtensions(this IApplicationBuilder builder)
    //{

    //    return builder.Use(async (context, next) =>
    //    {
    //        var jsRuntime = context.RequestServices.GetRequiredService<IJSRuntime>();
    //        JsImportHelper.LegacyRuntimeReference = jsRuntime;
    //        await next();
    //    });
    //}

    //public static WebApplication UseMudExtensions(this WebApplication app)
    //{
    //    app.Use(async (context, next) =>
    //    {
    //        var jsRuntime = context.RequestServices.GetRequiredService<IJSRuntime>();
    //        JsImportHelper.LegacyRuntimeReference = jsRuntime;
    //        await next(context);
    //    });
    //    return app;
    //}

    public static Func<object, Func<Task>, Task> MudExMiddleware =>
        async (context, next) =>
        {
            var p = context?.GetType()?.GetProperty("RequestServices")?.GetValue(context) as IServiceProvider;
            var jsRuntime = p?.GetRequiredService<IJSRuntime>();
            JsImportHelper.LegacyRuntimeReference = jsRuntime;
            await next();
        };
}