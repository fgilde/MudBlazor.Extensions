﻿using System.Reflection;
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

        var assemblies = new List<Assembly>
        {
            typeof(MudExFileDisplayZip).Assembly,
            Assembly.GetExecutingAssembly(),
            Assembly.GetEntryAssembly(),
            Assembly.GetCallingAssembly()
        };
        if(serviceImplementationAssemblies is { Length: > 0 })
            assemblies.AddRange(serviceImplementationAssemblies);
        var assembliesArray = assemblies.Where(a => a != null).Distinct().ToArray();
        services.RegisterAllImplementationsOf(new[] { typeof(IMudExFileDisplay) }, assembliesArray, ServiceLifetime.Scoped);
        
        services.RegisterAllWithRegisterAsAttribute(assembliesArray);
        
        services.AddScoped<MudBlazorExtensionJsInterop>();
        
        services.RegisterAllImplementationsOf(new[] { typeof(IMudExExternalFilePicker) }, assembliesArray, ServiceLifetime.Scoped);
        
        // TODO: Find maybe a better solution. For example if the MudBlazor.DialogService has a reference to injected JsRuntime, we can remove this section and the class and interface for  MudExDialogService : IMudExDialogService
        #region Replace IDialogService with MudExDialogService

        services.AddScoped<DialogService>(); // add the original service without interface
        // Get the original service descriptor
        services.Remove(services.First(d => d.ServiceType == typeof(IDialogService)));
        services.AddScoped<IDialogService, MudExDialogService>(); // add the new service with the original interface


        #endregion
       
        services.AddMudMarkdownServices();

        return services.AddMudExObjectEdit(assembliesArray);
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
    public static IServiceCollection AddMudServicesWithExtensions(this IServiceCollection services, Action<MudServicesConfiguration> mudServicesConfiguration, Action<MudExConfiguration> config,
        params Assembly[] serviceImplementationAssemblies) => services.AddMudServices(mudServicesConfiguration).AddMudExtensions(config, serviceImplementationAssemblies);

}


