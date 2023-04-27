using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Middleware;

public class MudBlazorExtensionsMiddleware
{
    private readonly RequestDelegate _next;

    public MudBlazorExtensionsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IJSRuntime jsRuntime)
    {
        JsImportHelper._runtime ??= jsRuntime;
        await _next(context);
    }
}