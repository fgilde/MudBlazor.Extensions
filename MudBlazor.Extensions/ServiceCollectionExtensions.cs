using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Options;
using MudBlazor.Services;

namespace MudBlazor.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMudExtensions(this IServiceCollection services, params Assembly[] serviceImplementationAssemblies)
    {
        //services.AddSingleton<MudBlazorExtensionJsInterop>();
        services.RegisterAllImplementationsOf(new[] { typeof(IMudExFileDisplay) }, serviceImplementationAssemblies, ServiceLifetime.Scoped);
        return services.AddMudExObjectEdit(serviceImplementationAssemblies);
    }

    public static IServiceCollection AddMudExtensions(this IServiceCollection services, Action<MudExConfiguration> config, params Assembly[] serviceImplementationAssemblies)
    {
        config?.Invoke(new MudExConfiguration());
        return services.AddMudExtensions(serviceImplementationAssemblies);
    }

    public static IServiceCollection AddMudServicesWithExtensions(this IServiceCollection services, params Assembly[] serviceImplementationAssemblies) 
        => services.AddMudServices().AddMudExtensions(serviceImplementationAssemblies);

    public static IServiceCollection AddMudServicesWithExtensions(this IServiceCollection services, MudServicesConfiguration mudServicesConfiguration, params Assembly[] serviceImplementationAssemblies) 
        => services.AddMudServices(mudServicesConfiguration).AddMudExtensions(serviceImplementationAssemblies);

    public static IServiceCollection AddMudServicesWithExtensions(this IServiceCollection services, Action<MudServicesConfiguration> mudServicesConfiguration, 
        params Assembly[] serviceImplementationAssemblies) => services.AddMudServices(mudServicesConfiguration).AddMudExtensions(serviceImplementationAssemblies);

    
    public static IServiceCollection AddMudServicesWithExtensions(this IServiceCollection services, Action<MudExConfiguration> config, params Assembly[] serviceImplementationAssemblies)
        => services.AddMudServices().AddMudExtensions(config, serviceImplementationAssemblies);

    public static IServiceCollection AddMudServicesWithExtensions(this IServiceCollection services, MudServicesConfiguration mudServicesConfiguration, Action<MudExConfiguration> config, params Assembly[] serviceImplementationAssemblies)
        => services.AddMudServices(mudServicesConfiguration).AddMudExtensions(config, serviceImplementationAssemblies);

    public static IServiceCollection AddMudServicesWithExtensions(this IServiceCollection services, Action<MudServicesConfiguration> mudServicesConfiguration, Action<MudExConfiguration> config,
        params Assembly[] serviceImplementationAssemblies) => services.AddMudServices(mudServicesConfiguration).AddMudExtensions(config, serviceImplementationAssemblies);

}

