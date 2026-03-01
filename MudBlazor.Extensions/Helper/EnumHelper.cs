using System.Collections.Concurrent;
using System.Reflection;
using MudBlazor.Extensions.Components.ObjectEdit.Options;

namespace MudBlazor.Extensions.Helper;

public class EnumHelper
{
    private static readonly ConcurrentDictionary<(Type enumType, string valueName), FieldInfo> FieldInfoCache = new();
    private static readonly ConcurrentDictionary<(Type enumType, string valueName, Type attributeType, bool inherit), object[]> AttributeCache = new();

    public static TAttribute[] GetCustomAttributes<TAttribute>(Enum val, bool inherit)
    {
        var enumType = val.GetType();
        var valueName = val.ToString();
        var attrType = typeof(TAttribute);

        var cacheKey = (enumType, valueName, attrType, inherit);
        var customAttributes = AttributeCache.GetOrAdd(cacheKey, key =>
        {
            var fieldKey = (key.enumType, key.valueName);
            var fieldInfo = FieldInfoCache.GetOrAdd(fieldKey, fk => fk.enumType.GetField(fk.valueName));
            return fieldInfo?.GetCustomAttributes(key.attributeType, key.inherit);
        });

        return customAttributes?.Select(o => (TAttribute)o).ToArray() ?? Array.Empty<TAttribute>();
    }
}