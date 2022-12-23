using Nextended.Core.Helper;
using System.Reflection;

namespace MudBlazor.Extensions.Helper;

public static class ComponentRenderHelper
{
    public static bool IsValidParameterAttribute(Type componentType, string key, object value)
    {
        if (key == nameof(MudComponentBase.UserAttributes))
            return false;
        var propertyInfo = componentType?.GetProperty(key, BindingFlags.Public | BindingFlags.Instance);
        if (propertyInfo != null && value != null)
            return propertyInfo.PropertyType.IsInstanceOfType(value);
        return propertyInfo != null;
    }

    public static IDictionary<string, object> GetCompatibleParameters(object instance, Type targetCompatibleType)
    {
        return DictionaryHelper.GetValuesDictionary(instance, false).Where(p => IsValidParameterAttribute(targetCompatibleType, p.Key, p.Value)).ToDictionary(p => p.Key, p => p.Value);
    }
}