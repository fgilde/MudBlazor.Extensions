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
}