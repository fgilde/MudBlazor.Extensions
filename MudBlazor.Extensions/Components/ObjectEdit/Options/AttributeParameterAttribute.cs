using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class AttributeParameterAttribute : System.Attribute
{
    public string Key { get; }
    public object Value { get; }

    public AttributeParameterAttribute(string key, object value)
    {
        Key = key;
        Value = value;
    }

    public static IDictionary<string, object> GetAttributesFromEnumValue(Enum val)
    {
        return EnumHelper.GetCustomAttributes<AttributeParameterAttribute>(val, false)?.ToDictionary(a => a.Key, a => a.Value) ?? new Dictionary<string, object>();
    }
}
