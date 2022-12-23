using Microsoft.AspNetCore.Components;
using Nextended.Core.Helper;
using System.Reflection;

namespace MudBlazor.Extensions.Helper;

public static class ComponentRenderHelper
{
    public static bool IsValidParameter(Type componentType, string key, object value)
    {
        return IsValidPropertyWithAttribute<ParameterAttribute>(componentType, key, value);
    }

    public static bool IsValidPropertyWithAttribute<TAttribute>(Type componentType, string key, object value) where TAttribute : Attribute
    {
        return IsValidProperty(componentType, key, value, typeof(TAttribute));
    }
    
    public static bool IsValidProperty(Type componentType, string key, object value, params Type[] requiredAttributeTypes)
    {
        if (key == nameof(MudComponentBase.UserAttributes))
            return false;
        var propertyInfo = componentType?.GetProperty(key, BindingFlags.Public | BindingFlags.Instance);

        bool res = propertyInfo != null && value != null
            ? propertyInfo.PropertyType.IsInstanceOfType(value)
            : propertyInfo != null;

        return res && (requiredAttributeTypes.Length == 0 || propertyInfo?.GetCustomAttributes().Any(x => requiredAttributeTypes.Contains(x.GetType())) == true);
    }

    public static IDictionary<string, object> GetCompatibleParameters<T>(T instance, Type targetCompatibleType) where T : new()
    {
        return DictionaryHelper.GetValuesDictionary(instance, false).Where(p => IsValidParameter(targetCompatibleType, p.Key, p.Value)).ToDictionary(p => p.Key, p => p.Value);
    }

    public static IDictionary<string, object> GetCompatibleProperties<T>(T instance, Type targetCompatibleType) where T : new()
    {
        return DictionaryHelper.GetValuesDictionary(instance, false).Where(p => IsValidProperty(targetCompatibleType, p.Key, p.Value)).ToDictionary(p => p.Key, p => p.Value);
    }
}