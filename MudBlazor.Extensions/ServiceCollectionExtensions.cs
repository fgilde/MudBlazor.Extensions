using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;
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
        if (services.All(x => x.ServiceType != typeof(MudExConfiguration)))        
            services.AddSingleton(new MudExConfiguration());  // Default configuration
        
        services.RegisterAllImplementationsOf(new[] { typeof(IMudExFileDisplay) }, serviceImplementationAssemblies, ServiceLifetime.Scoped);
        services.AddScoped<MudBlazorExtensionJsInterop>();
        services.AddScoped<MudExStyleBuilder>();
        services.AddScoped<MudExCssBuilder>();
        services.AddScoped<MudExAppearanceService>();

        // TODO: Find maybe a better solution. For example if the MudBlazor.DialogService has a reference to injected JsRuntime, we can remove this section and the class and interface for  MudExDialogService : IMudExDialogService
        #region Replace IDialogService with MudExDialogService

        services.AddScoped<DialogService>();    
        // Get the original service descriptor
        var originalDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDialogService));
        services.Remove(originalDescriptor);
        
        services.AddScoped<IDialogService>(provider =>
        {
            var originalDialogService = provider.GetRequiredService<DialogService>();
            var jsRuntime = provider.GetRequiredService<IJSRuntime>();
            return new MudExDialogService(originalDialogService, jsRuntime, provider, provider.GetRequiredService<MudExAppearanceService>());
        });

        services.AddScoped<IMudExDialogService>(sp => (MudExDialogService)sp.GetRequiredService<IDialogService>());

        #endregion
       
        
        return services.AddMudExObjectEdit(serviceImplementationAssemblies);
    }

    /// <summary>
    /// Adds MudBlazor extensions to the service collection with custom configuration.
    /// </summary>
    public static IServiceCollection AddMudExtensions(this IServiceCollection services, Action<MudExConfiguration> config, params Assembly[] serviceImplementationAssemblies)
    {
        var configuration = new MudExConfiguration();
        config?.Invoke(configuration);
        services.AddSingleton(configuration);
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


