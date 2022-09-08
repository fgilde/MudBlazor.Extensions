using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Services;

namespace MudBlazor.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMudExtensions(this IServiceCollection services, params Assembly[] serviceImplementationAssemblies)
    {
        //services.AddSingleton<MudBlazorExtensionJsInterop>();
        return services.AddMudExObjectEdit(serviceImplementationAssemblies);
    }    
    
    public static IServiceCollection AddMudServicesWithExtensions(this IServiceCollection services, params Assembly[] serviceImplementationAssemblies) 
        => services.AddMudServices().AddMudExtensions(serviceImplementationAssemblies);

    public static IServiceCollection AddMudServicesWithExtensions(this IServiceCollection services, MudServicesConfiguration mudServicesConfiguration, params Assembly[] serviceImplementationAssemblies) 
        => services.AddMudServices(mudServicesConfiguration).AddMudExtensions(serviceImplementationAssemblies);

    public static IServiceCollection AddMudServicesWithExtensions(this IServiceCollection services, Action<MudServicesConfiguration> mudServicesConfiguration, 
        params Assembly[] serviceImplementationAssemblies) => services.AddMudServices(mudServicesConfiguration).AddMudExtensions(serviceImplementationAssemblies);
}