using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Options;
using MudBlazor.Services;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions;

/// <summary>
/// Contains extension methods for IServiceCollection to configure MudBlazor services with extensions.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MudBlazor extensions to the service collection.
    /// </summary>
    public static IServiceCollection AddMudExtensions(this IServiceCollection services, params Assembly[] serviceImplementationAssemblies)
    {
        //services.AddSingleton<MudBlazorExtensionJsInterop>();
        services.RegisterAllImplementationsOf(new[] { typeof(IMudExFileDisplay) }, serviceImplementationAssemblies, ServiceLifetime.Scoped);

        return services.AddMudExObjectEdit(serviceImplementationAssemblies);
    }

    /// <summary>
    /// Adds MudBlazor extensions to the service collection with custom configuration.
    /// </summary>
    public static IServiceCollection AddMudExtensions(this IServiceCollection services, Action<MudExConfiguration> config, params Assembly[] serviceImplementationAssemblies)
    {
        config?.Invoke(new MudExConfiguration());
        return services.AddMudExtensions(serviceImplementationAssemblies);
    }

    /// <summary>
    /// Adds MudBlazor services and extensions to the service collection.
    /// </summary>
    public static IServiceCollection AddMudServicesWithExtensions(this IServiceCollection services, params Assembly[] serviceImplementationAssemblies)
        => services.AddMudServices().AddMudExtensions(serviceImplementationAssemblies);

    /// <summary>
    /// Adds MudBlazor services with custom configuration and extensions to the service collection.
    /// </summary>
    public static IServiceCollection AddMudServicesWithExtensions(this IServiceCollection services, MudServicesConfiguration mudServicesConfiguration, params Assembly[] serviceImplementationAssemblies)
        => services.AddMudServices(mudServicesConfiguration).AddMudExtensions(serviceImplementationAssemblies);

    /// <summary>
    /// Adds MudBlazor services with custom configuration and extensions to the service collection.
    /// </summary>
    public static IServiceCollection AddMudServicesWithExtensions(this IServiceCollection services, Action<MudServicesConfiguration> mudServicesConfiguration,
        params Assembly[] serviceImplementationAssemblies) => services.AddMudServices(mudServicesConfiguration).AddMudExtensions(serviceImplementationAssemblies);

    /// <summary>
    /// Adds MudBlazor services and extensions with custom configuration to the service collection.
    /// </summary>
    public static IServiceCollection AddMudServicesWithExtensions(this IServiceCollection services, Action<MudExConfiguration> config, params Assembly[] serviceImplementationAssemblies)
        => services.AddMudServices().AddMudExtensions(config, serviceImplementationAssemblies);

    /// <summary>
    /// Adds MudBlazor services with custom configuration and extensions with custom configuration to the service collection.
    /// </summary>
    public static IServiceCollection AddMudServicesWithExtensions(this IServiceCollection services, MudServicesConfiguration mudServicesConfiguration, Action<MudExConfiguration> config, params Assembly[] serviceImplementationAssemblies)
        => services.AddMudServices(mudServicesConfiguration).AddMudExtensions(config, serviceImplementationAssemblies);

    /// <summary>
    /// Adds MudBlazor services with custom configuration and extensions with custom configuration to the service collection.
    /// </summary>
    public static IServiceCollection AddMudServicesWithExtensions(this IServiceCollection services, Action<MudServicesConfiguration> mudServicesConfiguration, Action<MudExConfiguration> config,
        params Assembly[] serviceImplementationAssemblies) => services.AddMudServices(mudServicesConfiguration).AddMudExtensions(config, serviceImplementationAssemblies);

}


