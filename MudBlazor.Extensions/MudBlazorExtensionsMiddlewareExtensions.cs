using Microsoft.AspNetCore.Builder;
using MudBlazor.Extensions.Middleware;

namespace MudBlazor.Extensions;

public static class MudBlazorExtensionsMiddlewareExtensions
{
    public static IApplicationBuilder UseMudExtensions(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MudBlazorExtensionsMiddleware>();
    }
}