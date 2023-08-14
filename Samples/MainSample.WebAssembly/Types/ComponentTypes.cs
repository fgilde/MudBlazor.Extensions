using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Helper;
using System.Reflection;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;

namespace MainSample.WebAssembly.Types;

internal static class ComponentTypes
{
    public static HashSet<(Type Type, HasDocumentationAttribute Documentation)> DocumentedUtils()
    {
        return typeof(MudExSvg).Assembly.GetTypes()
            .Select(t => (t, t.GetCustomAttribute<HasDocumentationAttribute>()))
            .Where(d => d.Item2 != null)
            .Where(d => !d.Item1.Name.StartsWith("<") && !d.Item1.Name.StartsWith("_"))
            .ToHashSet();
    }

    public static HashSet<Type> Api()
    {
        return AllMudExComponents().Concat(DocumentedUtils().Select(d => d.Type)).Distinct().ToHashSet();
    }

    public static HashSet<Type> AllMudExComponents()
    {
        var interfaceType = typeof(IMudExComponent);
        return interfaceType.Assembly.GetTypes().Where(
                t => t.ImplementsInterface(interfaceType) && !t.IsInterface && !t.IsAbstract)
            .ToHashSet();
    }

    public static HashSet<Type> AllMudBlazorComponents()
    {
        return typeof(MudButton).Assembly.GetTypes().Where(
                t => !t.Name.StartsWith("_") && t.IsAssignableTo(typeof(ComponentBase)) && t is {IsInterface: false, IsAbstract: false, IsGenericType: false})
            .ToHashSet();
    }

}