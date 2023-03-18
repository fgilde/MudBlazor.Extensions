using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Extensions.Components.Base;
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
                t => t.ImplementsInterface(interfaceType) && !t.IsInterface && !t.IsAbstract && !t.IsGenericType)
            .ToHashSet();
    }

    public static HashSet<Type> AllMudBlazorComponents()
    {
        return typeof(MudButton).Assembly.GetTypes().Where(
                t => t.IsAssignableTo(typeof(ComponentBase)) && !t.IsInterface && !t.IsAbstract && !t.IsGenericType)
            .ToHashSet();
    }

    public static string GetDocumentation(Type type)
    {
        Assembly assembly = Assembly.GetAssembly(type);
        string assemblyPath = assembly.Location;
        string xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
        if (!File.Exists(xmlPath))
        {
            return null;
        }

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(xmlPath);
        string typeName = type.FullName;
        string xpath = $"//member[@name='T:{typeName}']/summary";
        XmlNode summaryNode = xmlDoc.SelectSingleNode(xpath);
        if (summaryNode == null)
        {
            return null;
        }

        return summaryNode.InnerText.Trim();
    }
}