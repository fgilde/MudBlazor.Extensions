using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace MainSample.WebAssembly.Utils;

public class ComponentRenderHelper
{
    //public static string RenderComponent<TComponent>(Dictionary<string, object> parameters) where TComponent : IComponent
    //{
    //    using var ctx = new Bunit.TestContext();

    //    var cpr = parameters.Select(pair => ComponentParameter.CreateParameter(pair.Key, pair.Value));
    //    var component = ctx.RenderComponent<TComponent>(cpr.ToArray());
        
    //    return component.Markup;
    //}
    
    public static string GenerateBlazorMarkupFromInstance<TComponent>(TComponent componentInstance) 
    {
        var componentName = componentInstance.GetType().FullName.Replace(componentInstance.GetType().Namespace + ".", string.Empty);
        var properties = componentInstance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var props = properties.ToDictionary(info => info.Name, info => info.GetValue(componentInstance)).Where(pair => Nextended.Blazor.Helper.ComponentRenderHelper.IsValidParameter(typeof(TComponent), pair.Key, pair.Value));
        
        var parameterString = string.Join("\n", props.Select(p => $"    {p.Key}=\"{p.Value}\""));

        var markup = $"<{componentName}\n{parameterString}\n/>";

        return markup;
    }

    public static string GenerateBlazorMarkup(Type componentType, IDictionary<string, object> parameters)
    {
        var componentName = componentType.FullName.Replace(componentType.Namespace + ".", string.Empty);
        var parameterString = string.Join("\n", parameters.Select(p => $"    {p.Key}=\"{p.Value}\""));

        var markup = $"<{componentName}\n{parameterString}\n/>";

        return markup;
    }
}

