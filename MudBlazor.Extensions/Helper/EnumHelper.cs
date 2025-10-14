using MudBlazor.Extensions.Components.ObjectEdit.Options;

namespace MudBlazor.Extensions.Helper;

public class EnumHelper
{
    public static TAttribute[] GetCustomAttributes<TAttribute>(Enum val, bool inherit)
    {
        var customAttributes =val.GetType().GetField(val.ToString())?.GetCustomAttributes(typeof(TAttribute), inherit);
        return customAttributes?.Select(o => (TAttribute)o).ToArray() ?? Array.Empty<TAttribute>();
    }
}