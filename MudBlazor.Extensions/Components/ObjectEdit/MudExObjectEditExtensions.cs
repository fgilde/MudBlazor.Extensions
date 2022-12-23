using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public static partial class MudExObjectEditExtensions
{
    public static IServiceCollection AddMudExObjectEdit(this IServiceCollection services, params Assembly[] serviceImplementationAssemblies)
    {
        return services.RegisterAllImplementationsOf(new[] { typeof(IObjectMetaConfiguration<>) } , serviceImplementationAssemblies, ServiceLifetime.Scoped)
                .RegisterAllImplementationsOf( new[] { typeof(IDefaultRenderDataProvider) }, serviceImplementationAssemblies, ServiceLifetime.Singleton); 
    }
}