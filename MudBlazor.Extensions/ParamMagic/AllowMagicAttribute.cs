using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.ParamMagic;

public class AllowMagicAttribute: System.Attribute
{
    public Type[] AllowedTypes { get; set; }

    public AllowMagicAttribute(params Type[] allowedTypes)
    {
        AllowedTypes = allowedTypes;
    }

    public bool IsAllowed(Type type)
    {
        return AllowedTypes == null
               || AllowedTypes.Length == 0
               || AllowedTypes.Contains(type);
    }
}