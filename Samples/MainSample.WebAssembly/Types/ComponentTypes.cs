using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Helper;
using System.Reflection;
using System.Xml;

namespace MainSample.WebAssembly.Types;

internal static class ComponentTypes
{
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
                t => t.IsAssignableTo(typeof(ComponentBase)) && !t.IsInterface && !t.IsAbstract && !t.IsGenericType)
            .ToHashSet();
    }

}