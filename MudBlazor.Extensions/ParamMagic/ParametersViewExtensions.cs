using System.Reflection;
using Microsoft.AspNetCore.Components;
using Nextended.Core.Extensions;


namespace MudBlazor.Extensions.ParamMagic;

public static class ParametersViewExtensions
{
    public static ParameterView Magic(this ParameterView parameters, IComponent component)
    {
        var toSet = component.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => new
        {
            PropertyInfo = p,
            Attribute = p.GetCustomAttribute<AllowMagicAttribute>()
        }).Where(x => x.Attribute != null).ToList();
        var changed = new List<string>();
        
        foreach (var param in toSet)
        {
            if (parameters.TryGetValue<object>(param.PropertyInfo.Name, out var valueAsObject) && valueAsObject!= null && param.Attribute.IsAllowed(valueAsObject.GetType()))
            {
                changed.Add(param.PropertyInfo.Name);
                param.PropertyInfo.SetValue(component, valueAsObject.MapTo(param.PropertyInfo.PropertyType));
            }
        }
        
        parameters = ParameterView.FromDictionary(parameters.ToDictionary()
            .Where(p => !changed.Contains(p.Key))
            .ToDictionary(p => p.Key, p => p.Value));
        return parameters;
    }
}