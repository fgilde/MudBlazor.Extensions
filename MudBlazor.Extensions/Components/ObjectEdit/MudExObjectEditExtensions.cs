using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions.Components.ObjectEdit;

/// <summary>
/// Contains extension methods for IServiceCollection to configure MudBlazor services with extensions.
/// </summary>
public static partial class MudExObjectEditExtensions
{
    private static readonly List<ServiceDescriptor> RegisteredRenderDataProvider = new();

    /// <summary>
    /// Adds MudBlazor Object edit extensions to the service collection.
    /// </summary>
    public static IServiceCollection AddMudExObjectEdit(this IServiceCollection services, params Assembly[] serviceImplementationAssemblies)
    {
        return services
                .RegisterAllImplementationsOf(new[] { typeof(IObjectMetaConfiguration<>) } , serviceImplementationAssemblies, ServiceLifetime.Scoped)
                .RegisterAllImplementationsOf(new[] { typeof(IDefaultRenderDataProvider) }, serviceImplementationAssemblies, ServiceLifetime.Singleton, RegisteredRenderDataProvider.Add)
                .RegisterAllImplementationsOf(new[] { typeof(IObjectEditorFor<>) }, serviceImplementationAssemblies, ServiceLifetime.Scoped,
                    descriptor =>
                    {
                        var propertyType = descriptor.ServiceType.GetGenericArguments().FirstOrDefault();
                        if (propertyType == null) return;
                        var hasRenderDataRegistered = RegisteredRenderDataProvider.Any(d => d.ImplementationType.ImplementsInterface(typeof(IDefaultRenderDataProviderFor<>).MakeGenericType(propertyType)));
                        if (!hasRenderDataRegistered)
                        {
                            IRenderData renderData = RenderDataDefaults.RegisterDefault(propertyType, RenderData.For(nameof(IObjectEditorFor<object>.Value), propertyType, descriptor.ImplementationType));
                            RenderDataDefaults.RegisterDefault(propertyType, renderData);
                        }

                    });
    }

}