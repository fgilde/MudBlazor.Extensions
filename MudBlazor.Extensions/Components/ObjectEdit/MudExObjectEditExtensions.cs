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

    public static IServiceCollection RegisterAllImplementationsOf(this IServiceCollection services, 
        Type[] interfacesToSearchImplementationsFor,
        Assembly[] assembliesToSearchImplementationsIn,
        ServiceLifetime lifeTime = ServiceLifetime.Transient)
    {
        assembliesToSearchImplementationsIn = (assembliesToSearchImplementationsIn.IsNullOrEmpty()
            ? new[] {Assembly.GetCallingAssembly(), Assembly.GetExecutingAssembly(), Assembly.GetEntryAssembly() }
            : assembliesToSearchImplementationsIn).Distinct().ToArray();
        var types = assembliesToSearchImplementationsIn.SelectMany(a => a.GetTypes()).Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType);
        foreach (var type in types)
        {
            foreach (var registerType in interfacesToSearchImplementationsFor)
            {
                if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == registerType))
                    services.Add(new ServiceDescriptor(type.GetInterfaces().First(i => i.GetGenericTypeDefinition() == registerType), type, lifeTime));
                else if (registerType.IsAssignableFrom(type))
                    services.Add(new ServiceDescriptor(registerType, type, lifeTime));
            }
        }
        return services;
    }
}