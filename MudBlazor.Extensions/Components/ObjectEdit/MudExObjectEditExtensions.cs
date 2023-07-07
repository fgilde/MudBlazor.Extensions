using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit;

/// <summary>
/// Contains extension methods for IServiceCollection to configure MudBlazor services with extensions.
/// </summary>
public static partial class MudExObjectEditExtensions
{
    /// <summary>
    /// Adds MudBlazor Object edit extensions to the service collection.
    /// </summary>
    public static IServiceCollection AddMudExObjectEdit(this IServiceCollection services, params Assembly[] serviceImplementationAssemblies)
    {
        return services.RegisterAllImplementationsOf(new[] { typeof(IObjectMetaConfiguration<>) } , serviceImplementationAssemblies, ServiceLifetime.Scoped)
                .RegisterAllImplementationsOf( new[] { typeof(IDefaultRenderDataProvider) }, serviceImplementationAssemblies, ServiceLifetime.Singleton); 
    }
}